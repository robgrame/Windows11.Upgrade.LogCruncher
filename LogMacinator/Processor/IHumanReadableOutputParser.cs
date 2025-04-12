using LogCruncher.Data;

namespace LogCruncher.Processor
{
    internal interface IHumanReadableOutputParser
    {
        Task IdentifyCompatibilityIssuesAsync(HumanReadableOutput humanReadableOutput);
        Task<HumanReadableOutput> LoadXmlAsync(string filePath);
        Task ParseHumanReadableFilesAsync();
    }
}