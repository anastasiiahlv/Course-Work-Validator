using CheckReport.Server.Configurations;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using Azure;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace CheckReport.Server.Services
{
    public class OpenAiService
    {
        private readonly OpenAiConfig _openAiConfig;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, object> _requirements;

        public OpenAiService(IOptionsMonitor<OpenAiConfig> optionsMonitor)
        {
            _openAiConfig = optionsMonitor.CurrentValue;
            _httpClient = new HttpClient
            {
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", _openAiConfig.Key)
                }
            };
            _requirements = LoadRequirements();
        }

        public async Task<List<string>> AnalyzeFullText(string fullText)
        {
            if (string.IsNullOrWhiteSpace(fullText))
                return new List<string> { "Помилка: отриманий порожній текст." };

            var formattedRequirements = JsonSerializer.Serialize(_requirements, new JsonSerializerOptions { WriteIndented = true });

            var prompt = $@"
Ти – експерт із перевірки курсових робіт.
Ось вимоги до документа:

{formattedRequirements}

**Перевір документ за такими критеріями:**
1. Чи містить документ всі необхідні розділи?
2. Чи відповідає форматування вимогам?
3. Чи є порушення оформлення або структури документа?

Якщо є помилки, поверни список у форматі JSON:
```json
{{
  ""errors"": [""Помилка 1"", ""Помилка 2""]
}}
Якщо всі вимоги виконані, поверни:
{{
  ""message"": ""Файл успішно пройшов перевірку!"" 
}}
Ось текст документа: {fullText}";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "Ти суворий експерт із перевірки курсових робіт. Перевіряй уважно." },
                    new { role = "user", content = prompt }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                string gptResponse = await response.Content.ReadAsStringAsync();
                return ExtractErrorsFromResponse(gptResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка виклику OpenAI API: {ex.Message}");
                return new List<string> { "Помилка виклику OpenAI API." };
            }
        }

        private List<string> ExtractErrorsFromResponse(string gptResponse)
        {
            try
            {
                string cleanedResponse = Regex.Replace(gptResponse, @"```json|```", "").Trim();
                var gptObject = JsonSerializer.Deserialize<Dictionary<string, object>>(cleanedResponse);
                if (gptObject != null && gptObject.ContainsKey("errors"))
                {
                    return JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(gptObject["errors"]));
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка розбору JSON від OpenAI: {ex.Message}");
                return new List<string> { "Помилка розбору відповіді від GPT-4." };
            }
        }

        private Dictionary<string, object> LoadRequirements()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(basePath)?.FullName;
            var filePath = Path.Combine(projectRoot, "Resources", "requirements.json");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл із вимогами не знайдено за шляхом: {filePath}");
            }

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
