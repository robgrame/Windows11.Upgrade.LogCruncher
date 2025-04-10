using LogMacinator.Processor;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogMacinator.Jobs
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during execution of HumanReadableFileProcessorJob at {Time}", DateTimeOffset.Now);

            }
        }
    }

}
