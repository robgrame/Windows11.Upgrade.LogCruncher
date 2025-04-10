using LogMacinator.Data;

namespace LogMacinator.Processor
{
    internal interface IHumanReadableOutputParser
    {
        Task IdentifyUpgradeIssuesAsync(HumanReadableOutput humanReadableOutput);
        Task<HumanReadableOutput> LoadXmlAsync(string filePath);
        Task ParseHumanReadableFilesAsync();
    }
}