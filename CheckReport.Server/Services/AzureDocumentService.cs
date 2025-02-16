using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

public class AzureDocumentService
{
    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureAI:Endpoint"];
        var key = configuration["AzureAI:Key"];
        _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(key));
    }

    public async Task<string> ExtractTextFromPdfAsync(IFormFile file)
    {
        using (var stream = file.OpenReadStream())
        {
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Started,
                "prebuilt-read",
                BinaryData.FromStream(stream)
            );

            AnalyzeResult result = await operation.WaitForCompletionAsync();

            var extractedText = new StringBuilder();
            foreach (var page in result.Pages)
            {
                foreach (var line in page.Lines)
                {
                    string normalizedLine = line.Content.Trim().Normalize(NormalizationForm.FormC); // 🔹 Нормалізуємо текст
                    extractedText.AppendLine(normalizedLine);
                }
            }

            return extractedText.ToString();
        }
    }
}

