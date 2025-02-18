using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using CheckReport.Server.Services;

namespace CheckReport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ValidateController : ControllerBase
    {
        private readonly ILogger<ValidateController> _logger;
        private readonly OpenAiService _openAiService;
        private readonly AzureDocumentService _documentService;

        public ValidateController(ILogger<ValidateController> logger, OpenAiService openAiService, AzureDocumentService documentService)
        {
            _logger = logger;
            _openAiService = openAiService;
            _documentService = documentService;
        }

        [HttpPost]
        public async Task<IActionResult> ValidateFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { errors = new List<string> { "Не вибрано файл." } });
            }

            Console.WriteLine($"Отримано файл: {file.FileName}, Content-Type: {file.ContentType}");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { errors = new List<string> { "Невірний формат файлу. Завантажуйте PDF." } });
            }

            string extractedText = await _documentService.ExtractTextFromPdfAsync(file);
            Console.WriteLine("Витягнутий текст з Azure AI:\n" + extractedText);

            if (string.IsNullOrEmpty(extractedText))
            {
                return BadRequest(new { errors = new List<string> { "Не вдалося прочитати текст із PDF." } });
            }

            // 🔹 Розбиття тексту на частини
            string titlePage = ExtractSection(extractedText, "Титульний аркуш");
            string abstractText = ExtractSection(extractedText, "Реферат");
            string tableOfContents = ExtractSection(extractedText, "ЗМІСТ");
            string introduction = ExtractSection(extractedText, "ВСТУП");
            string conclusions = ExtractSection(extractedText, "ВИСНОВКИ");
            string references = ExtractSection(extractedText, "СПИСОК ВИКОРИСТАНИХ ДЖЕРЕЛ");

            var errors = new List<string>();

            // 🔹 Аналізуємо кожну частину через GPT-4
            errors.AddRange(await _openAiService.ValidateTitlePage(titlePage));
            errors.AddRange(await _openAiService.ValidateAbstract(abstractText));
            errors.AddRange(await _openAiService.ValidateTableOfContents(tableOfContents));
            errors.AddRange(await _openAiService.ValidateIntroduction(introduction));
            errors.AddRange(await _openAiService.ValidateConclusions(conclusions));
            errors.AddRange(await _openAiService.ValidateReferences(references));

            if (errors.Count > 0)
            {
                return BadRequest(new { errors });
            }

            return Ok(new { message = "Файл успішно пройшов перевірку!" });
        }

        // 🟢 Метод для виділення титульного аркуша
        private string ExtractTitlePage(string text)
        {
            return text.Split("\n").Take(15).Aggregate("", (acc, line) => acc + line + "\n"); // Беремо перші 15 рядків
        }

        // 🟢 Метод для виділення реферату (пошук за ключовими словами)
        private string ExtractAbstract(string text)
        {
            return ExtractSection(text, "Реферат");
        }

        // 🟢 Метод для виділення змісту
        private string ExtractTableOfContents(string text)
        {
            return ExtractSection(text, "ЗМІСТ");
        }

        // 🟢 Метод для виділення вступу
        private string ExtractIntroduction(string text)
        {
            return ExtractSection(text, "ВСТУП");
        }

        // 🟢 Метод для виділення висновків
        private string ExtractConclusions(string text)
        {
            return ExtractSection(text, "ВИСНОВКИ");
        }

        // 🟢 Метод для виділення переліку використаних джерел
        private string ExtractReferences(string text)
        {
            return ExtractSection(text, "СПИСОК ВИКОРИСТАНИХ ДЖЕРЕЛ");
        }

        // 🔹 Універсальний метод для пошуку розділу в тексті
        private string ExtractSection(string text, string sectionName)
        {
            var regex = new Regex($@"(?<=\b{sectionName}\b)[\s\S]*?(?=\n[A-ZА-ЯІЇЄ]{{2,}})", RegexOptions.IgnoreCase);
            var match = regex.Match(text);
            return match.Success ? match.Groups[0].Value.Trim() : "";
        }
    }
}
