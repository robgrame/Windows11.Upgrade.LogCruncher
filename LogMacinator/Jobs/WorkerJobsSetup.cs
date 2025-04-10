using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Utils.Macinator.Config;

namespace LogMacinator.Jobs
{
    internal class WorkerJobsSetup : IConfigureOptions<QuartzOptions>
    {
        private readonly ILogger<WorkerJobsSetup> _logger;
        private readonly IOptions<LogProcessorSettings> _workerConfig;

        public WorkerJobsSetup(ILogger<WorkerJobsSetup> logger, IOptions<LogProcessorSettings> workerConfig)
        {
            _logger = logger;
            _workerConfig = workerConfig;
        }

        public void Configure(QuartzOptions options)
        {
            _logger.LogDebug("Configuring Quartz options");
        }


    }
}
