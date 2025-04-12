using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Utils.Macinator.Processor;

namespace LogCruncher.Jobs
{
    internal class SetupActLogProcessorJob : IJob
    {

        private readonly ILogger<SetupActLogProcessorJob> _logger;
        private readonly ISetupACTLogProcessor _setupACTLogProcessor;

        public SetupActLogProcessorJob(ILogger<SetupActLogProcessorJob> logger, ISetupACTLogProcessor setupACTLogProcessor)
        {
            _logger = logger;
            _setupACTLogProcessor = setupACTLogProcessor;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting execution of SetupActLogProcessorJob at {Time}", DateTimeOffset.Now);
            try
            {
                await _setupACTLogProcessor.ProcessLogFilesAsync();
                _logger.LogInformation("Successfully completed execution of SetupActLogProcessorJob at {Time}", DateTimeOffset.Now);

                // retrieve next fire time of the job from the context
                var nextFireTime = context.NextFireTimeUtc?.DateTime;
                if (nextFireTime.HasValue)
                {
                    _logger.LogInformation("Job {jobname} next fire time {firetime}:", nameof(SetupActLogProcessorJob), nextFireTime.Value);
                }
                else
                {
                    _logger.LogWarning("Job {jobname} Next fire time is not available.", nameof(SetupActLogProcessorJob));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing SetupActLogProcessorJob at {Time}", DateTimeOffset.Now);

            }
        }
    }
}
