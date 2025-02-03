using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;

namespace CheckReport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValidateController : ControllerBase
    {
        [HttpPost("validate")]
        public IActionResult ValidateFile([FromForm] IFormFile file)
        {
            if (file == null)
            {
                return BadRequest("Не вибрано файл.");
            }

            // Перевірка формату
            if (!file.FileName.EndsWith(".docx"))
            {
                return BadRequest("Невірний формат файлу. Повинно бути .docx");
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    using (var wordDoc = WordprocessingDocument.Open(stream, false))
                    {
                        var body = wordDoc.MainDocumentPart.Document.Body;
                        return Ok(new { message = "Файл успішно пройшов перевірку!" });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Сталася помилка при обробці файлу: {ex.Message}");
            }
        }
    }
}
