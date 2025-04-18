﻿using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogCruncher.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLitePCL;
using Windows.Utils.Macinator.Config;
using Windows.Utils.Macinator.EF;


    namespace Windows.Utils.Macinator.Processor
{
    internal class SetupACTLogProcessor : ISetupACTLogProcessor
    {
        private const string SetupActLogFilePattern = "setupact.log";
        private readonly ILogger<SetupACTLogProcessor> _logger;
        private readonly LogProcessorSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        public SetupACTLogProcessor(ILogger<SetupACTLogProcessor> logger, IOptions<LogProcessorSettings> settings, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
        }


        private IEnumerable<string> GetLogFiles(string rootPath, string searchPattern)
        {
            return Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);
        }

        public async Task ProcessLogFilesAsync()
        {
            // Get all setupact.log files in the log path
            var logFiles = GetLogFiles(_settings.LogsRootPath, SetupActLogFilePattern);

            foreach (var logFile in logFiles)
            {
                _logger.LogDebug("Starting log analysis for {LogsRootPath}", logFile);
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
                                        _logger.LogDebug("OperationDetails Name {name} with ID {id}",operationDetails.Name,operationDetails.OperationId);
                                        
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

                await SaveAnalisysResultAsync(results, failures, systemInfo, uncompleteAction);
            }
        }

        public void InitializeDatabase()
        {
            // Get ACTLogAnalysisContext from serviceProvider
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<ACTLogAnalysisContext>();

            if (context == null)
            {
                _logger.LogError("ACTLogAnalysisContext is null");
                return;
            }

            _logger.LogDebug("Initializing database...");
            context.Database.EnsureCreated();
            _logger.LogTrace("Database initialization completed.");
        }


        private async Task SaveAnalisysResultAsync(List<ACTOperationResultEntity> results, List<string> failures, SystemInfoEntity systemInfo, ACTUncompleteActionEntity uncompleteAction)
        {
            // Save results to JSON with hostname in the filename
            var outputDirectory = Path.GetDirectoryName(_settings.OutputPath) ?? throw new InvalidOperationException("Output path directory is null.");
            var upgradeIssuesDirectory = Path.Combine(outputDirectory, "upgrade_issues");
            var outputFilePath = Path.Combine(upgradeIssuesDirectory, $"{systemInfo.Hostname}_UpgradeIssues.json");

            try
            {
                var json = JsonSerializer.Serialize(new { SystemInfo = systemInfo, Results = results, Failures = failures, UncompleteAction = uncompleteAction }, new JsonSerializerOptions { WriteIndented = true });

                if (_settings.SaveToDatabase)
                {
                    // Get ACTLogAnalysisContext from serviceProvider
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetService<ACTLogAnalysisContext>();
                    if (context == null)
                    {
                        _logger.LogError("DB Context is null");
                        return;
                    }

                    var hash = ComputeHash(systemInfo, uncompleteAction);
                    var existingRecord = await context.LogAnalysisResults.FirstOrDefaultAsync(r => r.Hash == hash);

                    if (existingRecord != null)
                    {
                        _logger.LogInformation("A record with the same hash already exists. Skipping insertion.");
                        return; // Skip adding duplicate record
                    }

                    var efLogAnalysisResult = new ACTLogAnalysisResultEntity
                    {
                        SystemInfo = systemInfo,
                        SystemInfoId = systemInfo.Id,
                        Results = results,
                        Failures = failures.ToList(),
                        UncompleteAction = uncompleteAction,
                        UncompleteActionId = uncompleteAction.Id,
                        Hash = hash // Store the hash
                    };


                    _logger.LogDebug("Adding log analysis results to database...");
                    context.LogAnalysisResults.Add(efLogAnalysisResult);
                    _logger.LogTrace("Log analysis results saved to database.");

                    // Save changes to the database
                    _logger.LogDebug("Saving changes to the database...");
                    await context.SaveChangesAsync();
                    _logger.LogTrace("Changes saved to the database.");
                }

                _logger.LogDebug("Saving ACT upgrade issues to JSON file");
                // Check if the directory exists, if not create it
                if (!Directory.Exists(upgradeIssuesDirectory))
                {
                    Directory.CreateDirectory(upgradeIssuesDirectory);
                }

                if (File.Exists(outputFilePath))
                {
                    File.Delete(outputFilePath);
                }

                // save to file
                _logger.LogDebug("Saving log analysis results to file: {OutputFilePath}", outputFilePath);
                await File.WriteAllTextAsync(outputFilePath, json);
                _logger.LogTrace("Log analysis results saved to file: {OutputFilePath}", outputFilePath);

                _logger.LogDebug("SetupACTLog analysis completed");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.Message, "Error saving log analysis results to database");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex.Message, "Unable to serialize data to JSON format");
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex.Message, "Unable to serialize data to JSON format");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex.Message, "Error saving log analysis results to {OutputFilePath}", outputFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error saving log analysis results to {OutputFilePath}", outputFilePath);
            }
        }

        private string ComputeHash(SystemInfoEntity systemInfo, ACTUncompleteActionEntity uncompleteAction)
        {
            using var sha256 = SHA256.Create();
            var input = $"{systemInfo.Hostname}{systemInfo.OsVersion}{uncompleteAction.ActionName}{uncompleteAction.StartTime}";
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
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
    }
}
