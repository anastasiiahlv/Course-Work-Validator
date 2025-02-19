using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
                "prebuilt-read",
                BinaryData.FromStream(stream)
            );

            AnalyzeResult result = operation.Value;

            var extractedText = new StringBuilder();

            foreach (var page in result.Pages)
            {
                foreach (var line in page.Lines)
                {
                    extractedText.AppendLine(line.Content.Trim());
                }
            }

            string text = extractedText.ToString();
            text = FixTextEncoding(text);

            return text;
        }
    }

    private string FixTextEncoding(string text)
    {
        return text
            .Replace("?", "і")
            .Replace("!", "ї")
            .Replace("'", "’")
            .Replace("  ", " ")
            .Trim();
    }
}
