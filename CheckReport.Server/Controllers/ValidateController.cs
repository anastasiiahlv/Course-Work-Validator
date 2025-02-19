using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                return BadRequest(new { errors = new List<string> { "Дозволений формат файлу - .pdf" } });
            }

            string extractedText = await _documentService.ExtractTextFromPdfAsync(file);
            extractedText = FixTextEncoding(extractedText);

            Console.WriteLine("Витягнутий текст:\n" + extractedText);

            if (string.IsNullOrEmpty(extractedText))
            {
                return BadRequest(new { errors = new List<string> { "Не вдалося прочитати текст із PDF." } });
            }

            var errors = await _openAiService.AnalyzeFullText(extractedText);

            if (errors.Count > 0)
            {
                return BadRequest(new { errors });
            }

            return Ok(new { message = "Файл успішно пройшов перевірку!" });
        }

        private string FixTextEncoding(string text)
        {
            return text
                .Replace("?", "і")
                .Replace("I", "І")
                .Replace("E", "Є")
                .Replace("i", "і")
                .Replace("єє", "є")
                .Replace("Ii", "Ї")
                .Replace("ye", "є")
                .Replace("’", "'")
                .Replace("–", "-")
                .Trim();
        }
    }
}

