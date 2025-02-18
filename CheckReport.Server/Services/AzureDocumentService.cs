using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

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
                WaitUntil.Completed,
                "prebuilt-layout",
                BinaryData.FromStream(stream)
            );

            var result = operation.Value;
            var extractedText = new StringBuilder();

            foreach (var page in result.Pages)
            {
                foreach (var line in page.Lines)
                {
                    extractedText.AppendLine(line.Content.Trim());
                }
            }

            return PostProcessText(extractedText.ToString());
        }
    }

    private string PostProcessText(string text)
    {
        text = text.Replace("?", "і")
                   .Replace("?", "ї")
                   .Replace("?", "є");

        text = Regex.Replace(text, @"\s{2,}", " ");
        text = Regex.Replace(text, @"\n{2,}", "\n").Trim();

        return text;
    }
}

