using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LogCruncher.Data;
using LogCruncher.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Windows.Utils.Macinator.Config;
using Windows.Utils.Macinator.EF;

namespace LogCruncher.Processor
{
    internal class HumanReadableOutputParser : IHumanReadableOutputParser
    {
        private const string AppraiserHumanReadableXmlPattern = "*.4.0.1_APPRAISER_HumanReadable.xml";

        private readonly ILogger<HumanReadableOutputParser> _logger;
        private readonly LogProcessorSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        public HumanReadableOutputParser(ILogger<HumanReadableOutputParser> logger, IOptions<LogProcessorSettings> settings, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
        }

        private async IAsyncEnumerable<string> GetHumanReadableFiles(string rootPath, string searchPattern)
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

        public async Task ParseHumanReadableFilesAsync()
        {
            _logger.LogDebug("Retrieving human-readable files in {LogsRootPath}", _settings.LogsRootPath);

            // Get all files ending with "APPRAISER_HumanReadable.xml" in the log path
            await foreach (var file in GetHumanReadableFiles(_settings.LogsRootPath, AppraiserHumanReadableXmlPattern))
            {
                _logger.LogDebug("Processing human-readable file: {FilePath}", file);

                // Load the XML file
                var humanReadableOutput = await LoadXmlAsync(file);

                // Analyze the properties
                await IdentifyCompatibilityIssuesAsync(humanReadableOutput);
            }
        }


        public async Task<HumanReadableOutputEntity> LoadXmlAsync(string filePath)
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


        public async Task IdentifyCompatibilityIssuesAsync(HumanReadableOutputEntity humanReadableOutput)
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

                    // Ensure computerName is not null or empty before calling SaveCompatibilityIssuesAsync
                    if (!string.IsNullOrEmpty(computerName))
                    {
                        await SaveCompatibilityIssuesAsync(computerName, matchingAssets);
                    }
                    else
                    {
                        _logger.LogWarning("Computer name is null or empty. Skipping saving upgrade issues.");
                    }
                }
                else
                {
                    _logger.LogDebug("No assets with blocking properties found.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking properties in WicaRun");
                throw;
            }
        }

        private async Task SaveCompatibilityIssuesAsync(string computerName, List<PropertyListEntity> propertyList)
        {
            _logger.LogDebug("Saving upgrade issues to JSON file");
            // Save results to JSON with hostname in the filename
            var outputDirectory = Path.GetDirectoryName(_settings.OutputPath) ?? throw new InvalidOperationException("Output path directory is null.");
            var compatIssuesDirectory = Path.Combine(outputDirectory, "compat_issues"); 
            var outputFilePath = Path.Combine(compatIssuesDirectory, $"{computerName}_CompatIssues.json");

            // Check if the directory exists, if not create it
            if (!Directory.Exists(compatIssuesDirectory))
            {
                Directory.CreateDirectory(compatIssuesDirectory);
            }

            // Delete previous file if it exists
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            var json = JsonSerializer.Serialize(new { ComputerName = computerName, UpgradeIssues = propertyList }, new JsonSerializerOptions { WriteIndented = true });

            if (_settings.SaveToDatabase)
            {
                _logger.LogDebug("Saving compatibility issues to database...");
                await SaveCompatibilityIssuesToDB(computerName, propertyList.SelectMany(pl => pl.Properties ?? Enumerable.Empty<PropertyEntity>()).Select(p => new PropertyEntity { Name = p.Name, Value = p.Value }).ToList());
                _logger.LogTrace("Compatibility issues saved to database.");
            }
            else
            {
                _logger.LogDebug("Skipping saving to database as per configuration.");
            }

            await File.WriteAllTextAsync(outputFilePath, json);

            _logger.LogInformation("HumanReadable file analysis completed. Results saved to {OutputFilePath}", outputFilePath);
        }

        private async Task SaveCompatibilityIssuesToDB(string computerName, List<PropertyEntity> propertyList)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<HumanReadableAnalysisContext>();
            if (context == null)
            {
                _logger.LogError("DB Context is null");
                return;
            }

            _logger.LogDebug("Initializing database...");
            context.Database.EnsureCreated();
            _logger.LogTrace("Database initialization completed.");

            var hash = ComputeHash(computerName, propertyList);
            var existingRecord = await context.CompatIssuesEntities.FirstOrDefaultAsync(e => e.Hash == hash);
            if (existingRecord != null)
            {
                _logger.LogInformation("A record with the same hash already exists. Skipping insertion.");
                return; // Skip adding duplicate record
            }

            var entity = new PropertyListEntity
            {
                ComputerName = computerName,
                Type = "Inventory",
                Properties = propertyList.Select(p => new PropertyEntity
                {
                    Name = p.Name,
                    Value = p.Value,
                }).ToList(),
                Hash = hash
            };

            _logger.LogDebug("Adding Compatibility issues analysis results to database...");
            context.CompatIssuesEntities.Add(entity);
            _logger.LogTrace("Compatibility issues analysis results saved to database.");

            // Save changes to the database
            _logger.LogDebug("Saving changes to the database...");
            await context.SaveChangesAsync();
            _logger.LogTrace("Changes saved to the database.");
        }

        private string ComputeHash(string computerName, List<PropertyEntity> propertyList)
        {
            using var sha256 = SHA256.Create();
            // create a unique string representation of the data
            var sb = new StringBuilder();
            sb.Append(computerName);
            foreach (var property in propertyList)
            {
                sb.Append(property);
                foreach (var prop in propertyList ?? Enumerable.Empty<PropertyEntity>())
                {
                    sb.Append(prop.Name);
                    sb.Append(prop.Value);
                }
            }
            var input = sb.ToString();

            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }

    }
}
