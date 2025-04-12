using LogCruncher.Processor;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogCruncher.Jobs
{
    internal class HumanReadableFileProcessorJob : IJob
    {
        private readonly ILogger<HumanReadableFileProcessorJob> _logger;
        private readonly IHumanReadableOutputParser _humanReadableOutputParser;

        public HumanReadableFileProcessorJob(ILogger<HumanReadableFileProcessorJob> logger, IHumanReadableOutputParser humanReadableOutputParser)
        {
            _logger = logger;
            _humanReadableOutputParser = humanReadableOutputParser;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting execution of HumanReadableFileProcessorJob at {Time}", DateTimeOffset.Now);
            try
            {
                await _humanReadableOutputParser.ParseHumanReadableFilesAsync();
                _logger.LogInformation("Successfully completed execution of HumanReadableFileProcessorJob at {Time}", DateTimeOffset.Now);

                // retrieve next fire time of the job from the context
                var nextFireTime = context.NextFireTimeUtc?.DateTime;
                if (nextFireTime.HasValue)
                {
                    _logger.LogInformation("Job {jobname} next fire time {firetime}:", nameof(HumanReadableFileProcessorJob), nextFireTime.Value);
                }
                else
                {
                    _logger.LogWarning("Job {jobname} Next fire time is not available.", nameof(HumanReadableFileProcessorJob));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during execution of HumanReadableFileProcessorJob at {Time}", DateTimeOffset.Now);

            }
        }
    }

}
