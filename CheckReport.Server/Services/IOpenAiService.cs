namespace CheckReport.Server.Services
{
    public interface IOpenAiService
    {
        Task<string> AnalyzeText(string documentText);
    }
}
