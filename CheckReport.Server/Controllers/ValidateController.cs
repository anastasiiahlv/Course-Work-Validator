using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
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
            var errors = new HashSet<string>();

            if (file == null)
            {
                return BadRequest(new { errors = new List<string> { "Не вибрано файл." } });
            }

            Console.WriteLine($"Отримано файл: {file.FileName}, Content-Type: {file.ContentType}");

            if (!file.FileName.EndsWith(".docx"))
            {
                return BadRequest(new { errors = new List<string> { "Невірний формат файлу. Повинно бути .docx" } });
            }

            try
            {
                using (var stream = file.OpenReadStream())
                using (var wordDoc = WordprocessingDocument.Open(stream, false))
                {
                    var doc = wordDoc.MainDocumentPart.Document;

                    ValidateTitlePage(wordDoc, errors);

                    // Перевірка текстового формату
                    ValidateTextFormat(doc, errors);

                    // Перевірка параметрів сторінки
                    ValidatePageSettings(wordDoc, errors);

                    // Перевірка нумерації сторінок
                    ValidatePageNumbering(wordDoc, errors);
                }

                if (errors.Any())
                {
                    return BadRequest(new { errors = errors.ToList() });
                }

                return Ok(new { message = "Файл успішно пройшов перевірку!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка: {ex.Message}");
                return BadRequest(new { errors = new List<string> { $"Сталася помилка при обробці файлу: {ex.Message}" } });
            }
        }

        private void ValidateTextFormat(Document doc, HashSet<string> errors)
        {
            var paragraphs = doc.Body.Elements<Paragraph>();

            foreach (var paragraph in paragraphs)
            {
                var run = paragraph.Elements<Run>().FirstOrDefault();
                if (run?.RunProperties != null)
                {
                    var runProperties = run.RunProperties;

                    // Перевірка шрифту
                    var fontName = runProperties.RunFonts?.Ascii?.Value;
                    if (fontName != null && fontName != "Times New Roman")
                    {
                        errors.Add("Основний текст має бути шрифтом Times New Roman.");
                    }

                    // Перевірка розміру шрифту
                    var fontSize = runProperties.FontSize?.Val?.Value;
                    if (fontSize != null && fontSize != "28") // 28 Half-Points = 14 pt
                    {
                        errors.Add("Основний текст має бути розміром 14.");
                    }
                }

                // Перевірка міжрядкового інтервалу
                var spacing = paragraph.ParagraphProperties?.SpacingBetweenLines;
                if (spacing != null && spacing.Line?.Value != "360") // 1.5 * 240 = 360
                {
                    errors.Add("Міжрядковий інтервал повинен бути 1.5.");
                }
            }
        }

        private void ValidatePageSettings(WordprocessingDocument wordDoc, HashSet<string> errors)
        {
            var sectionProperties = wordDoc.MainDocumentPart.Document.Body.Elements<SectionProperties>().FirstOrDefault();
            if (sectionProperties != null)
            {
                var pageMargin = sectionProperties.Elements<PageMargin>().FirstOrDefault();
                if (pageMargin != null)
                {
                    if (pageMargin.Left < 1700) errors.Add("Ліве поле повинно бути 3 см.");
                    if (pageMargin.Right < 850) errors.Add("Праве поле повинно бути 1.5 см.");
                    if (pageMargin.Top < 1000) errors.Add("Верхнє поле повинно бути 2.5 см.");
                    if (pageMargin.Bottom < 1000) errors.Add("Нижнє поле повинно бути 2.5 см.");
                }
            }

            var paragraphs = wordDoc.MainDocumentPart.Document.Body.Elements<Paragraph>();

            foreach (var paragraph in paragraphs)
            {
                var properties = paragraph.ParagraphProperties;
                if (properties?.Indentation?.FirstLine != "708") // 1.25 см = 708 twips
                {
                    errors.Add("Абзацний відступ повинен бути 1.25 см.");
                }
            }
        }
        private void ValidatePageNumbering(WordprocessingDocument wordDoc, HashSet<string> errors)
        {
            var footerParts = wordDoc.MainDocumentPart.FooterParts;
            bool hasPageNumbering = false;

            foreach (var footer in footerParts)
            {
                foreach (var paragraph in footer.RootElement.Elements<Paragraph>())
                {
                    foreach (var fieldCode in paragraph.Descendants<FieldCode>())
                    {
                        if (fieldCode.Text.Contains("PAGE"))
                        {
                            hasPageNumbering = true;
                            break;
                        }
                    }

                    if (hasPageNumbering) break;
                }
            }

            if (!hasPageNumbering)
            {
                errors.Add("Нумерація сторінок повинна бути присутня з другої сторінки.");
            }
        }

        private void ValidateTitlePage(WordprocessingDocument wordDoc, HashSet<string> errors)
        {
            var body = wordDoc.MainDocumentPart.Document.Body;
            var text = string.Join("\n", body.Elements<Paragraph>().Select(p => p.InnerText.Trim()));

            // Отримуємо поточний рік
            string currentYear = DateTime.Now.Year.ToString();

            // Перевірки
            if (!text.Contains("КИЇВСЬКИЙ НАЦІОНАЛЬНИЙ УНІВЕРСИТЕТ"))
                errors.Add("Титульний аркуш має містити назву університету.");

            if (!text.Contains("Факультет"))
                errors.Add("На титульному аркуші має бути вказаний факультет.");

            if (!text.Contains("Кафедра"))
                errors.Add("На титульному аркуші має бути вказана кафедра.");

            if (!text.Contains("Курсова робота"))
                errors.Add("На титульному аркуші має бути написано 'Курсова робота'.");

            if (!text.Contains("за спеціальністю"))
                errors.Add("На титульному аркуші має бути вказана спеціальність.");

            if (!text.Contains("3-го курсу"))
                errors.Add("Не вказано курс студента (має бути '3-го курсу').");

            if (!text.Contains("Науковий керівник"))
                errors.Add("На титульному аркуші має бути вказано 'Науковий керівник'.");

            if (!text.Contains("Засвідчую, що в цій роботі немає запозичень з праць інших авторів без відповідних посилань."))
                errors.Add("Не знайдено текст засвідчення оригінальності роботи.");

            if (!text.Contains($"Київ – {currentYear}"))
                errors.Add($"Рік у нижній частині титульного аркуша має бути {currentYear}.");
        }
    }
}

