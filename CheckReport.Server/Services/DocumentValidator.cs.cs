using System;
using CheckReport.Server.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace CheckReport.Server.Services
{
    public class DocumentValidator
    {
        public ValidationResult ValidateDocx(IFormFile file)
        {
            var result = new ValidationResult();

            // Перевірка назви файлу
            if (!Regex.IsMatch(file.FileName, @"^[a-zA-Z0-9_\-]+\.docx$"))
            {
                result.Errors.Add("Назва файлу повинна містити лише латинські літери.");
            }

            using (var stream = file.OpenReadStream())
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, false))
                {
                    var body = doc.MainDocumentPart.Document.Body;

                    // Перевірка шрифту (Times New Roman, 14pt)
                    if (!CheckFont(body))
                    {
                        result.Errors.Add("Текст має бути написаний шрифтом Times New Roman, розмір 14.");
                    }

                    // Перевірка полів сторінки
                    if (!CheckPageMargins(doc))
                    {
                        result.Errors.Add("Поля сторінки мають бути: ліве - 3 см, праве - 1.5 см, верхнє і нижнє - 2.5 см.");
                    }

                    // Перевірка абзацного відступу
                    if (!CheckParagraphIndent(body))
                    {
                        result.Errors.Add("Абзацний відступ повинен бути 1.25 см.");
                    }
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private bool CheckFont(Body body)
        {
            foreach (var para in body.Elements<Paragraph>())
            {
                foreach (var run in para.Elements<Run>())
                {
                    RunProperties runProps = run.RunProperties;
                    if (runProps?.RunFonts?.Ascii != "Times New Roman" || runProps?.FontSize?.Val != "28") // 14pt = 28 half-points
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool CheckPageMargins(WordprocessingDocument doc)
        {
            var sectionProps = doc.MainDocumentPart.Document.Body.GetFirstChild<SectionProperties>();
            var pageMargins = sectionProps?.GetFirstChild<PageMargin>();

            return pageMargins != null &&
                   pageMargins.Left == 1700 &&  // 3 см в twips
                   pageMargins.Right == 850 &&  // 1.5 см
                   pageMargins.Top == 1000 &&   // 2.5 см
                   pageMargins.Bottom == 1000;
        }

        private bool CheckParagraphIndent(Body body)
        {
            foreach (var para in body.Elements<Paragraph>())
            {
                var props = para.ParagraphProperties?.Indentation;
                if (props?.FirstLine != "709") // 1.25 см = 709 twips
                {
                    return false;
                }
            }
            return true;
        }
    }
}
