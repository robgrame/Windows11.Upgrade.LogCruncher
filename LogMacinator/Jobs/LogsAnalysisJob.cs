using LogCruncher.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Utils.Macinator.Config;

namespace LogCruncher.Jobs
{
    internal class LogsAnalysisJob : IJob
    {
        private readonly ILogger<LogsAnalysisJob> _logger;
        private readonly ILogsAnalyzer _logAnalyzer;
        private readonly IOptions<LogProcessorSettings> _settings;
        public LogsAnalysisJob(ILogger<LogsAnalysisJob> logger, ILogsAnalyzer logAnalyzer, IOptions<LogProcessorSettings> settings)
        {
            _logger = logger;
            _logAnalyzer = logAnalyzer;
            _settings = settings;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting execution of LogAnalysisJob at {Time}", DateTimeOffset.Now);
            try
            {
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
    }
}
