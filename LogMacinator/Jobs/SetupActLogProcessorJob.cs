using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Utils.Macinator.Processor;

namespace LogMacinator.Jobs
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing SetupActLogProcessorJob at {Time}", DateTimeOffset.Now);

            }
        }
    }
}
