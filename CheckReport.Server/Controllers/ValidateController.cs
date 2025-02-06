using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;

namespace CheckReport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ValidateController : ControllerBase
    {
        [HttpPost]
        public IActionResult ValidateFile([FromForm] IFormFile file)
        {
            if (file == null)
            {
                return BadRequest(new { message = "Не вибрано файл." });
            }

            Console.WriteLine($"Отримано файл: {file.FileName}, Content-Type: {file.ContentType}");

            if (!file.FileName.EndsWith(".docx"))
            {
                return BadRequest(new { message = "Невірний формат файлу. Повинно бути .docx" });
            }

            try
            {
                using (var stream = file.OpenReadStream())
                using (var wordDoc = WordprocessingDocument.Open(stream, false))
                {
                    return Ok(new { message = "Файл успішно пройшов перевірку!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка: {ex.Message}");
                return BadRequest(new { message = $"Сталася помилка при обробці файлу: {ex.Message}" });
            }
        }
    }
}
