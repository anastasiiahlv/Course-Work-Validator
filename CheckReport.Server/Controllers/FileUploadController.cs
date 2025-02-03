using Microsoft.AspNetCore.Mvc;
using CheckReport.Server;
using CheckReport.Server.Services;

namespace CheckReport.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly DocumentValidator _validator = new DocumentValidator();

        [HttpPost("upload")]
        public IActionResult UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не завантажено.");
            }

            if (Path.GetExtension(file.FileName) != ".docx")
            {
                return BadRequest("Файл повинен бути у форматі DOCX.");
            }

            var validationResult = _validator.ValidateDocx(file);
            return Ok(validationResult);
        }
    }
}
