using CheckReport.Server.Configurations;
using Microsoft.Extensions.Options;
using OpenAI;
using System.Text.Json;
using System.Text;

namespace CheckReport.Server.Services
{
    public class OpenAiService: IOpenAiService
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
            var prompt = $@"
Ти – експерт із перевірки курсових робіт.  
Тобі надіслано текст курсової роботи.
Перевір його відповідність наступним критеріям:

1️. **Шрифт**: Times New Roman, 14 pt  
2️. **Міжрядковий інтервал**: 1.5  
3️. **Поля сторінки**: ліве – 3 см, праве – 1.5 см, верхнє/нижнє – 2.5 см  
4️. **Абзацний відступ**: 1.25 см  
5️. **Нумерація сторінок**: має починатися з другої сторінки  
6️. **Титульний аркуш повинен містити**:
   - ""Київський національний університет імені Тараса Шевченка""
   - Назву факультету
   - Назву кафедри
   - Фразу ""Курсова робота""
   - ""3-го курсу""
   - ""Київ – {DateTime.Now.Year}""

**Дуже важливо! Якщо хоч одна з вимог не виконується, поверни список помилок у форматі JSON:**  
```json
{{
  ""errors"": [
    ""Текст не має правильного шрифту."",
    ""Відсутня назва університету."",
    ""Нумерація сторінок починається не з другої сторінки.""
  ]
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
    }
}
