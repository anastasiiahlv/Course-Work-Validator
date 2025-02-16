using CheckReport.Server.Configurations;
using Microsoft.Extensions.Options;
using OpenAI;
using System.Text.Json;
using System.Text;
using System.IO;

namespace CheckReport.Server.Services
{
    public class OpenAiService : IOpenAiService
    {
        private readonly OpenAiConfig _openAiConfig;
        private readonly HttpClient _httpClient;

        public OpenAiService(IOptionsMonitor<OpenAiConfig> optionsMonitor)
        {
            _openAiConfig = optionsMonitor.CurrentValue;
            _httpClient = new HttpClient();
        }

        public async Task<string> AnalyzeText(string documentText)
        {
            var requirements = LoadRequirements();
            var formattedRequirements = JsonSerializer.Serialize(requirements, new JsonSerializerOptions { WriteIndented = true });

            var prompt = $@"
Ти – експерт із перевірки курсових робіт.  
Ось вимоги до оформлення документа:

{formattedRequirements}

Перевір цей документ і повідом, які вимоги не виконані.  
Якщо є помилки, поверни список у форматі JSON:
```json
{{
  ""errors"": [""Помилка 1"", ""Помилка 2""]
}}

Якщо всі вимоги виконані, поверни:
{{
  ""message"": ""Файл успішно пройшов перевірку!""
}}

Ось текст документа:
```{documentText}```";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
    {
        new { role = "system", content = "Ти експерт із перевірки курсових робіт. Будь дуже суворим у своїй оцінці." },
        new { role = "user", content = prompt }
    }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiConfig.Key}");
            Console.WriteLine($"OpenAI Key: {_openAiConfig.Key}");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            return await response.Content.ReadAsStringAsync();
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
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
    }
}
