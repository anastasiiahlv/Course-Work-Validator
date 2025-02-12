using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using CheckReport.Server.Services;
using System.Text.Json;
using CheckReport.Server.Configurations;
using System.Text.RegularExpressions;

namespace CheckReport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ValidateController : ControllerBase
    {
        private readonly ILogger<ValidateController> _logger;
        private readonly IOpenAiService _openAiService;

        public ValidateController(ILogger<ValidateController> logger, IOpenAiService openAiService)
        {
            _logger = logger;
            _openAiService = openAiService;
        }

        [HttpPost]
        public async Task<IActionResult> ValidateFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { errors = new List<string> { "Не вибрано файл." } });
            }

            Console.WriteLine($"Отримано файл: {file.FileName}, Content-Type: {file.ContentType}");

            if (!file.FileName.EndsWith(".docx"))
            {
                return BadRequest(new { errors = new List<string> { "Невірний формат файлу. Повинно бути .docx" } });
            }

            // Витягуємо текст із документа
            string extractedText = ExtractTextFromDocx(file);
            if (string.IsNullOrEmpty(extractedText))
            {
                return BadRequest(new { errors = new List<string> { "Не вдалося прочитати текст із документа." } });
            }

            // Надсилаємо текст у GPT-4
            string gptResponse = await _openAiService.AnalyzeText(extractedText);
            Console.WriteLine("GPT-4 Response: " + gptResponse);

            try
            {
                // 🔹 Видаляємо ```json ... ```
                string cleanedResponse = Regex.Replace(gptResponse, @"```json|```", "").Trim();

                // 🔹 Дебаг: виводимо очищену відповідь
                Console.WriteLine("Очищений JSON від GPT-4:\n" + cleanedResponse);

                // 🔹 Розбираємо відповідь GPT-4 (оскільки errors вкладені у `choices[0].message.content`)
                var gptObject = JsonSerializer.Deserialize<Dictionary<string, object>>(cleanedResponse);
                var choices = JsonSerializer.Deserialize<JsonElement[]>(gptObject["choices"].ToString());
                var message = choices[0].GetProperty("message").GetProperty("content").GetString();

                // 🔹 Перетворюємо `message` у Dictionary
                var analysisResult = JsonSerializer.Deserialize<Dictionary<string, object>>(message);

                if (analysisResult != null && analysisResult.ContainsKey("errors"))
                {
                    var gptErrors = JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(analysisResult["errors"]));
                    return BadRequest(new { errors = gptErrors });
                }

                return Ok(new { message = "Файл успішно пройшов перевірку!" });
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"❌ JSON помилка: {jsonEx.Message}");
                return BadRequest(new { errors = new List<string> { "Помилка розбору JSON від GPT-4." } });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Інша помилка: {ex.Message}");
                return BadRequest(new { errors = new List<string> { "Помилка обробки відповіді від GPT-4." } });
            }
        }

        private string ExtractTextFromDocx(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            using (var wordDoc = WordprocessingDocument.Open(stream, false))
            {
                var body = wordDoc.MainDocumentPart.Document.Body;
                return string.Join("\n", body.Elements<Paragraph>().Select(p => p.InnerText.Trim()));
            }
        }
    }
}
