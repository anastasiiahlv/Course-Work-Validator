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

        /// <summary>
        /// Виконує перевірку кожної частини курсової роботи окремо.
        /// </summary>
        public async Task<List<string>> ValidateTitlePage(string titlePage)
        {
            return await AnalyzeSection(titlePage, "Перевір титульний аркуш відповідно до наступних вимог", "titlePage");
        }

        public async Task<List<string>> ValidateAbstract(string abstractText)
        {
            return await AnalyzeSection(abstractText, "Перевір наявність і правильність оформлення реферату", "abstract");
        }

        public async Task<List<string>> ValidateTableOfContents(string tableOfContents)
        {
            return await AnalyzeSection(tableOfContents, "Перевір, чи присутній зміст і чи правильно він оформлений", "tableOfContents");
        }

        public async Task<List<string>> ValidateIntroduction(string introduction)
        {
            return await AnalyzeSection(introduction, "Перевір наявність і правильність оформлення вступу", "introduction");
        }

        public async Task<List<string>> ValidateConclusions(string conclusions)
        {
            return await AnalyzeSection(conclusions, "Перевір наявність і правильність оформлення висновків", "conclusions");
        }

        public async Task<List<string>> ValidateReferences(string references)
        {
            return await AnalyzeSection(references, "Перевір наявність і правильність оформлення переліку використаних джерел", "references");
        }

        /// <summary>
        /// Виконує перевірку конкретного розділу документа.
        /// </summary>
        private async Task<List<string>> AnalyzeSection(string sectionText, string instruction, string sectionKey)
        {
            if (string.IsNullOrWhiteSpace(sectionText))
                return new List<string> { $"Не вдалося знайти розділ: {instruction}" };

            var sectionRequirements = _requirements.ContainsKey(sectionKey) ? _requirements[sectionKey] : null;
            var formattedRequirements = sectionRequirements != null
                ? JsonSerializer.Serialize(sectionRequirements, new JsonSerializerOptions { WriteIndented = true })
                : "{}";

            var prompt = $@"
Ти – експерт із перевірки курсових робіт.
Перевір наступний розділ документа:
{instruction}

Ось вимоги для цієї частини документа:
{formattedRequirements}

**Якщо є помилки, поверни список у форматі JSON:**
```json
{{
  ""errors"": [""Помилка 1"", ""Помилка 2""]
}}
Якщо всі вимоги виконані, поверни:
{{
  ""message"": ""Файл успішно пройшов перевірку!""
}}
Ось текст розділу: {sectionText}";

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
        /// <summary>
        /// Витягує список помилок із відповіді GPT-4.
        /// </summary>
        private List<string> ExtractErrorsFromResponse(string gptResponse)
        {
            try
            {
                // Видаляємо `json` та ` ``` ` маркери
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

        /// <summary>
        /// Завантажує вимоги для перевірки курсових робіт.
        /// </summary>
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

