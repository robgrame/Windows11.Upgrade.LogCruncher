using LogCruncher.Data;
using LogCruncher.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System.Xml.Serialization;
using Windows.Utils.Macinator.Config;
using Windows.Utils.Macinator.EF;

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
                await foreach (var humanReadableFile in GetUpgradeLogFiles(_settings.LogsRootPath , _settings.CompatLogFilePattern))
                {
                    if (string.IsNullOrEmpty(humanReadableFile))
                    {
                        _logger.LogWarning("File path is null or empty.");
                        continue;
                    }

                    _logger.LogDebug("Found humanReadableFile: {FilePath}", humanReadableFile);

                    _logger.LogDebug("Loading human readable XML humanReadableFile: {FilePath}", humanReadableFile);
                    var parsedHumanReadableOutput = await LoadHumanrReadableXMLFileAsync(humanReadableFile);

                    // Retrive the computer name from the human readable output
                    if (parsedHumanReadableOutput != null)
                    {
                        var computerName = parsedHumanReadableOutput.RunInfos?.RunInfo?.FirstOrDefault()?.Components?.FirstOrDefault(c => c.Type == "Metadata")?.Properties?.FirstOrDefault(p => p.Name == "ComputerName")?.Value;
                        _logger.LogDebug("ComputerName: {ComputerName}", computerName);

                        var compatibilityIssues = IdentifyCompatibilityIssues(parsedHumanReadableOutput);
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

                            // retrieve the Humarn readable file path


                            await foreach (var setupActLogFile in GetUpgradeLogFiles(_settings.LogsRootPath, _settings.SetupActLogFilePattern))
                            {
                                if (!string.IsNullOrEmpty(setupActLogFile))
                                {
                                    _logger.LogDebug("Processing SetupACT log humanReadableFile: {FilePath}", setupActLogFile);
                                    // Process the SetupACT log humanReadableFile as needed

                                }
                                else
                                {
                                    _logger.LogWarning("SetupACT log humanReadableFile path is null or empty.");
                                }
                            }

                        }
                    }
                    else if (parsedHumanReadableOutput == null)
                    {
                        _logger.LogWarning("Failed to load human readable XML humanReadableFile: {FilePath}", humanReadableFile);
                        continue;
                    }


                    // Process the humanReadableFile as needed


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
                _logger?.LogDebug("Found humanReadableFile: {FilePath}", file);
                yield return file;
            }
        }


        public async Task AnalyzeSetupActLogsAsync(string logFile)
        {
  
                _logger.LogDebug("Starting log analysis for {LogFile}", logFile);
                var logLines = ReadLogLines(logFile);
                var results = new List<ACTOperationResultEntity>();
                var uncompleteAction = new ACTUncompleteActionEntity { Id = Guid.NewGuid() }; // Ensure OperationId is set
                var failures = new List<string>();
                var systemInfoDict = new Dictionary<string, string>();
                var uniqueIds = new HashSet<int>();
                bool systemInfoCaptured = false;
                bool isMarkerFound = false;
                bool isTableHeaderFound = false;
                bool isSystemInfoSection = false;

                await using var enumerator = logLines.GetAsyncEnumerator();
                try
                {
                    while (await enumerator.MoveNextAsync())
                    {
                        var line = enumerator.Current;

                        // Detect the start of the system information section
                        if (line.Contains("Host system information:"))
                        {
                            _logger.LogTrace("System information section detected.");
                            isSystemInfoSection = true;
                            continue;
                        }

                        // Capture system information lines
                        if (isSystemInfoSection && !systemInfoCaptured)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                _logger.LogDebug("End of system information section.");
                                systemInfoCaptured = true;
                                isSystemInfoSection = false;
                                continue;
                            }
                            if (line.Contains(":"))
                            {
                                var parts = line.Split(':', 2);
                                if (parts.Length == 2)
                                {
                                    var key = parts[0].Trim();
                                    var value = parts[1].Trim();
                                    _logger.LogDebug("Captured system info: {Key} = {Value}", key, value);
                                    systemInfoDict[key] = value;
                                }
                            }
                            continue;
                        }

                        // Detect the marker line
                        if (line.Contains("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<"))
                        {
                            _logger.LogTrace("Operation marker detected.");
                            isMarkerFound = true;
                            isTableHeaderFound = false; // Reset table header flag
                            continue;
                        }

                        // Check for the "Operation completed successfully:" line
                        if (isMarkerFound && line.Contains("Operation completed successfully: "))
                        {
                            var operationName = line.Substring(line.IndexOf(": ") + 2).Trim();
                            _logger.LogTrace("Detected successful operation: {OperationName}", operationName);
                            isMarkerFound = false;
                            // Look for the table header
                            while (await enumerator.MoveNextAsync())
                            {
                                line = enumerator.Current;
                                if (line.Contains("---|---"))
                                {
                                    _logger.LogTrace("Table header detected.");
                                    isTableHeaderFound = true;
                                    break;
                                }
                            }

                            // Parse the table rows
                            if (isTableHeaderFound)
                            {
                                while (await enumerator.MoveNextAsync())
                                {
                                    line = enumerator.Current;
                                    if (line.Contains("----------------------------------------------------------------------------------------------"))
                                    {
                                        _logger.LogTrace("End of table detected.");
                                        break;
                                    }
                                    var operationDetails = ParseOperationDetails(line, operationName);
                                    if (operationDetails != null)
                                    {
                                        _logger.LogTrace("Parsed operation details: {OperationDetails}", operationDetails);
                                        _logger.LogDebug("OperationDetails Name {name} with ID {id}", operationDetails.Name, operationDetails.OperationId);

                                        results.Add(operationDetails);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to parse table row: {Line}", line);
                                    }
                                }
                            }
                            continue;
                        }

                        // Check for "SP Executing operation:" line
                        if (line.Contains("Executing operation:"))
                        {
                            string executingOperationName = line.Substring(line.IndexOf(": ") + 2).Trim();
                            _logger.LogDebug("Detected executing operation: {executingOperationName}", executingOperationName);
                            var parts = line.Split(' ');
                            uncompleteAction.ActionName = executingOperationName;
                            string startTime = parts[0].Trim() + " " + parts[1].Replace(",", "").Trim();
                            uncompleteAction.StartTime = DateTime.TryParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var start) ? start : null;
                            continue;
                        }

                        // Check for the "Operation failed:" line
                        if (isMarkerFound && line.Contains("Operation failed: "))
                        {
                            var failureReason = line.Substring(line.IndexOf(": ") + 2).Trim();
                            _logger.LogError("Detected failed operation: {FailureReason}", failureReason);
                            failures.Add(failureReason);
                            isMarkerFound = false;
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing the log.");
                }

                // Map dictionary to SystemInfoEntity object with validation
                var systemInfo = new SystemInfoEntity
                {
                    // retrieve the hostname from parent folder name
                    Hostname = Path.GetFileName(Path.GetDirectoryName(logFile)) ?? $"Unknown{String.GetHashCode(Path.GetDirectoryName(logFile))}",
                    OsVersion = Environment.OSVersion.ToString(),
                    VM = ValidateString(systemInfoDict.GetValueOrDefault("VM") ?? string.Empty),
                    FirmwareType = ValidateString(systemInfoDict.GetValueOrDefault("Firmware type") ?? string.Empty),
                    Manufacturer = ValidateString(systemInfoDict.GetValueOrDefault("Manufacturer") ?? string.Empty),
                    Model = ValidateString(systemInfoDict.GetValueOrDefault("Model") ?? string.Empty),
                    BIOSName = ValidateString(systemInfoDict.GetValueOrDefault("BIOS name") ?? string.Empty),
                    BIOSVersion = ValidateString(systemInfoDict.GetValueOrDefault("BIOS version") ?? string.Empty),
                    BIOSReleaseDate = ValidateString(systemInfoDict.GetValueOrDefault("BIOS release date") ?? string.Empty),
                    TotalMemory = ValidateLong(systemInfoDict.GetValueOrDefault("Total memory") ?? string.Empty),
                    NumberOfPhysicalCPUs = ValidateInt(systemInfoDict.GetValueOrDefault("Number of physical CPUs") ?? string.Empty),
                    NumberOfLogicalCPUs = ValidateInt(systemInfoDict.GetValueOrDefault("Number of logical CPUs") ?? string.Empty),
                    ProcessorManufacturer = ValidateString(systemInfoDict.GetValueOrDefault("Processor manufacturer") ?? string.Empty),
                    ProcessorName = ValidateString(systemInfoDict.GetValueOrDefault("Processor name") ?? string.Empty),
                    ProcessorCaption = ValidateString(systemInfoDict.GetValueOrDefault("Processor caption") ?? string.Empty),
                    ProcessorArchitecture = ValidateString(systemInfoDict.GetValueOrDefault("Processor architecture") ?? string.Empty),
                    ProcessorClock = ValidateInt(systemInfoDict.GetValueOrDefault("Processor clock") ?? string.Empty)
                };

                _logger.LogTrace("Log Analysys completed.");

                _logger.LogTrace("Log analysis results: {ResultsCount}", results.Count);
            
        }

        private ACTOperationResultEntity? ParseOperationDetails(string line, string operationName)
        {
            if (line.Contains("|") && !line.StartsWith("---")) // Skip headers or separators
            {
                var parts = line.Split('|');
                if (parts.Length >= 6)
                {
                    // Extract the ID from the first part of the line
                    var idPart = parts[0].Split(' ').Last().Trim();
                    return new ACTOperationResultEntity
                    {
                        OperationId = int.TryParse(idPart, out var id) ? id : -1,
                        Name = operationName,
                        Executed = parts[2].Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase),
                        StartTime = DateTime.TryParseExact(parts[3].Trim(), "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var start) ? start : null,
                        EndTime = DateTime.TryParseExact(parts[4].Trim(), "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var end) ? end : null,
                        Elapsed = TimeSpan.TryParse(parts[5].Trim(), out var elapsed) ? elapsed : TimeSpan.Zero
                    };
                }
            }

            return null;
        }

        private async IAsyncEnumerable<string> ReadLogLines(string filePath)
        {
            using var reader = new StreamReader(filePath);
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                yield return line;
            }
        }

        // Helper methods for validation
        private string ValidateString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
        }

        private int ValidateInt(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        private long ValidateLong(string value)
        {
            return long.TryParse(value, out var result) ? result : 0L;
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
                _logger.LogError(ex, "Error loading XML from humanReadableFile: {FilePath}", filePath);
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
