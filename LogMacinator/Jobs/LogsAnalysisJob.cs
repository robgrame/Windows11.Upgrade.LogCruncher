using LogCruncher.Data;
using LogCruncher.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Utils.Macinator.Config;

namespace LogCruncher.Jobs
{
    internal class LogsAnalysisJob : IJob
    {
        private readonly ILogger<LogsAnalysisJob> _logger;
        private readonly ILogsAnalyzer _logAnalyzer;
        private readonly LogProcessorSettings _settings;
        public LogsAnalysisJob(ILogger<LogsAnalysisJob> logger, ILogsAnalyzer logAnalyzer, IOptions<LogProcessorSettings> settings)
        {
            _logger = logger;
            _logAnalyzer = logAnalyzer;
            _settings = settings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting execution of LogAnalysisJob at {Time}", DateTimeOffset.Now);
            try
            {
                _logger.LogDebug("Searching for Human readable xml files");
                await foreach (var file in GetUpgradeLogFiles(_settings.LogsRootPath , _settings.CompatLogFilePattern))
                {
                    if (string.IsNullOrEmpty(file))
                    {
                        _logger.LogWarning("File path is null or empty.");
                        continue;
                    }

                    _logger.LogDebug("Found file: {FilePath}", file);

                    _logger.LogDebug("Loading human readable XML file: {FilePath}", file);
                    var humanReadableOutput = await LoadHumanrReadableXMLFileAsync(file);

                    // Retrive the computer name from the human readable output
                    if (humanReadableOutput != null)
                    {
                        var computerName = humanReadableOutput.RunInfos?.RunInfo?.FirstOrDefault()?.Components?.FirstOrDefault(c => c.Type == "Metadata")?.Properties?.FirstOrDefault(p => p.Name == "ComputerName")?.Value;
                        _logger.LogDebug("ComputerName: {ComputerName}", computerName);

                        var compatibilityIssues = IdentifyCompatibilityIssues(humanReadableOutput);
                        if (compatibilityIssues != null)
                        {
                            _logger.LogDebug("Identified compatibility issues:");
                            foreach (var issue in compatibilityIssues)
                            {
                                foreach (var property in issue.Properties ?? Enumerable.Empty<PropertyEntity>())
                                {
                                    _logger.LogDebug("{Name}: {Value}", property.Name, property.Value);
                                }
                            }
                            _logger.LogDebug("Total compatibility issues found: {Count}", compatibilityIssues.Count);
                            _logger.LogInformation("No further processing will be done for this computer: {ComputerName}", computerName);
                        }
                        else
                        {
                            _logger.LogDebug("No compatibility issues found.");

                            // Since no compatibility issues were found, we can proceed with SetupACT log processing
                            _logger.LogDebug("Processing SetupACT log files");
                            
                            await foreach (var setupActLogFile in GetUpgradeLogFiles(_settings.LogsRootPath, _settings.SetupActLogFilePattern))
                            {
                                if (!string.IsNullOrEmpty(setupActLogFile))
                                {
                                    _logger.LogDebug("Processing SetupACT log file: {FilePath}", setupActLogFile);
                                    // Process the SetupACT log file as needed

                                }
                                else
                                {
                                    _logger.LogWarning("SetupACT log file path is null or empty.");
                                }
                            }

                        }
                    }
                    else if (humanReadableOutput == null)
                    {
                        _logger.LogWarning("Failed to load human readable XML file: {FilePath}", file);
                        continue;
                    }


                    // Process the file as needed


                }

                await _logAnalyzer.AnalyzeLogsAsync();

                _logger.LogInformation("Successfully completed execution of LogAnalysisJob at {Time}", DateTimeOffset.Now);
                // retrieve next fire time of the job from the context
                var nextFireTime = context.NextFireTimeUtc?.DateTime;
                if (nextFireTime.HasValue)
                {
                    _logger.LogInformation("Job {jobname} next fire time {firetime}:", nameof(LogsAnalysisJob), nextFireTime.Value);
                }
                else
                {
                    _logger.LogWarning("Job {jobname} Next fire time is not available.", nameof(LogsAnalysisJob));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during execution of LogAnalysisJob at {Time}", DateTimeOffset.Now);
            }
        }

        private async IAsyncEnumerable<string> GetUpgradeLogFiles(string rootPath, string searchPattern)
        {
            _logger.LogDebug("Searching for files in {RootPath} with pattern {SetupActLogFilePattern}", rootPath, searchPattern);

            if (!System.IO.Directory.Exists(rootPath))
            {
                _logger.LogWarning("Directory does not exist: {RootPath}", rootPath);
                yield break;
            }
            else
            {
                _logger.LogDebug("Directory exists: {RootPath}", rootPath);
            }

            IEnumerable<string> GetFiles()
            {
                try
                {
                    return Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving files from directory: {RootPath}", rootPath);
                    return Enumerable.Empty<string>();
                }
            }

            var files = await Task.Run(GetFiles);
            foreach (var file in files)
            {
                _logger?.LogDebug("Found file: {FilePath}", file);
                yield return file;
            }
        }

        public async Task<HumanReadableOutputEntity> LoadHumanrReadableXMLFileAsync(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(HumanReadableOutput));
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var humanReadableOutput = await Task.Run(() => serializer.Deserialize(fileStream) as HumanReadableOutput);
                    if (humanReadableOutput == null)
                    {
                        throw new InvalidOperationException("Deserialization returned null");
                    }

                    // Map HumanReadableOutput to HumanReadableOutputEntity
                    var entity = new HumanReadableOutputEntity
                    {
                        RunInfos = humanReadableOutput.RunInfos != null ? new RunInfosEntity
                        {
                            RunInfo = humanReadableOutput.RunInfos.RunInfo?.Select(ri => new RunInfoEntity
                            {
                                Components = ri.Components?.Select(c => new ComponentEntity
                                {
                                    Type = c.Type,
                                    TypeIdentifier = c.TypeIdentifier,
                                    Properties = c.Properties?.Select(p => new PropertyEntity
                                    {
                                        Name = p.Name,
                                        Value = p.Value,
                                        Ordinal = p.Ordinal
                                    }).ToList()
                                }).ToList()
                            }).ToList()
                        } : null,
                        Assets = humanReadableOutput.Assets?.Select(a => new AssetEntity
                        {
                            PropertyLists = a.PropertyLists?.Select(pl => new PropertyListEntity
                            {
                                Type = pl.Type,
                                Properties = pl.Properties?.Select(p => new PropertyEntity
                                {
                                    Name = p.Name,
                                    Value = p.Value,
                                    Ordinal = p.Ordinal
                                }).ToList()
                            }).ToList()
                        }).ToList()
                    };

                    return entity;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading XML from file: {FilePath}", filePath);
                throw;
            }
        }

        public List<PropertyListEntity> IdentifyCompatibilityIssues(HumanReadableOutputEntity humanReadableOutput)
        {
            try
            {
                _logger.LogTrace("Checking properties in WicaRun"); 

                _logger.LogTrace("Retrieving Computername from WicaRun");
                var computerName = humanReadableOutput.RunInfos?.RunInfo?.FirstOrDefault()?.Components?.FirstOrDefault(c => c.Type == "Metadata")?.Properties?.FirstOrDefault(p => p.Name == "ComputerName")?.Value;
                _logger.LogDebug("ComputerName: {ComputerName}", computerName);

                var matchingAssets = new List<PropertyListEntity>();

                foreach (var asset in humanReadableOutput.Assets ?? Enumerable.Empty<AssetEntity>())
                {
                    var inventoryPropertyList = asset.PropertyLists?
                        .FirstOrDefault(pl => pl.Type == "Inventory");

                    if (inventoryPropertyList != null)
                    {
                        var hasBlockingProperties = asset.PropertyLists?
                            .Where(pl => pl.Type == "DecisionMaker")
                            .SelectMany(pl => pl.Properties ?? Enumerable.Empty<PropertyEntity>())
                            .Any(p => (p.Name == "DT_ANY_SVH_BlockingSV" || p.Name == "DT_ANY_SYS_BlockingSystem") && p.Value == "TRUE");

                        if (hasBlockingProperties == true)
                        {
                            matchingAssets.Add(inventoryPropertyList);
                        }

                    }
                }

                if (matchingAssets.Count > 0)
                {
                    _logger.LogDebug("Assets with blocking properties:");
                    foreach (var inventory in matchingAssets)
                    {
                        foreach (var property in inventory.Properties ?? Enumerable.Empty<PropertyEntity>())
                        {
                            _logger.LogDebug("{Name}: {Value}", property.Name, property.Value);
                        }
                    }

                    return matchingAssets;
                }
                else
                {
                    _logger.LogDebug("No assets with blocking properties found.");
                    return null;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking properties in WicaRun");
                throw;
            }
        }
    }
}
