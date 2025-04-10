﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LogMacinator.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Windows.Utils.Macinator.Config;

namespace LogMacinator.Processor
{
    internal class HumanReadableOutputParser : IHumanReadableOutputParser
    {
        private const string AppraiserHumanReadableXmlPattern = "*.4.0.1_APPRAISER_HumanReadable.xml";

        private readonly ILogger<HumanReadableOutputParser> _logger;
        private readonly LogProcessorSettings _settings;

        public HumanReadableOutputParser(ILogger<HumanReadableOutputParser> logger, IOptions<LogProcessorSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
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
                await IdentifyUpgradeIssuesAsync(humanReadableOutput);
            }
        }


        public async Task<HumanReadableOutput> LoadXmlAsync(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(HumanReadableOutput));
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var result = await Task.Run(() => serializer.Deserialize(fileStream) as HumanReadableOutput);
                    if (result == null)
                    {
                        throw new InvalidOperationException("Deserialization returned null");
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading XML from file: {FilePath}", filePath);
                throw;
            }
        }

        public async Task IdentifyUpgradeIssuesAsync(HumanReadableOutput humanReadableOutput)
        {
            try
            {
                _logger.LogTrace("Checking properties in WicaRun");

                _logger.LogTrace("Retrieving Computername from WicaRun");
                var computerName = humanReadableOutput.RunInfos?.RunInfo?.FirstOrDefault()?.Components?.FirstOrDefault(c => c.Type == "Metadata")?.Properties?.FirstOrDefault(p => p.Name == "ComputerName")?.Value;
                _logger.LogDebug("ComputerName: {ComputerName}", computerName);

                var matchingAssets = new List<PropertyList>();

                foreach (var asset in humanReadableOutput.Assets ?? Enumerable.Empty<Asset>())
                {
                    var inventoryPropertyList = asset.PropertyLists?
                        .FirstOrDefault(pl => pl.Type == "Inventory");

                    if (inventoryPropertyList != null)
                    {
                        var hasBlockingProperties = asset.PropertyLists?
                            .Where(pl => pl.Type == "DecisionMaker")
                            .SelectMany(pl => pl.Properties ?? Enumerable.Empty<Property>())
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
                        foreach (var property in inventory.Properties ?? Enumerable.Empty<Property>())
                        {
                            _logger.LogDebug("{Name}: {Value}", property.Name, property.Value);
                        }
                    }

                    // Ensure computerName is not null or empty before calling SaveUpgradeIssuesAsync
                    if (!string.IsNullOrEmpty(computerName))
                    {
                        await SaveUpgradeIssuesAsync(computerName, matchingAssets);
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

        private async Task SaveUpgradeIssuesAsync(string computerName, List<PropertyList> propertyList)
        {
            // Save results to JSON with hostname in the filename
            var outputDirectory = Path.GetDirectoryName(_settings.OutputPath) ?? throw new InvalidOperationException("Output path directory is null.");
            var outputFilePath = Path.Combine(outputDirectory, $"_UpgradeIssues.json");
            var json = JsonSerializer.Serialize(new { ComputerName = computerName, UpgradeIssues = propertyList }, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputFilePath, json);

            _logger.LogInformation("HumanReadable file analysis completed. Results saved to {OutputFilePath}", outputFilePath);
        }

    }
}
