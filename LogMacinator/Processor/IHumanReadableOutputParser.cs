using LogCruncher.Data;

namespace LogCruncher.Processor
{
    internal interface IHumanReadableOutputParser
    {
        Task IdentifyCompatibilityIssuesAsync(HumanReadableOutputEntity humanReadableOutput);
        Task<HumanReadableOutputEntity> LoadXmlAsync(string filePath);
        Task ParseHumanReadableFilesAsync();
    }
}