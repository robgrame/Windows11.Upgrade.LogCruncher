using LogCruncher.EF;
using LogCruncher.Jobs;
using LogCruncher.Processor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Quartz.Util;
using Serilog;
using System;
using System.Reflection;
using Windows.Utils.Macinator.Config;
using Windows.Utils.Macinator.EF;
using Windows.Utils.Macinator.Processor;


namespace Windows.Utils.Macinator
{
    class Program
    {
        static async Task Main(string[] args)
        {


            try
            {
                bool isService = false;
                bool isConsole = false;
                string logsRootPath = string.Empty;
                string outputPath = string.Empty;


                var builder = Host.CreateApplicationBuilder(args);

                var exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
                builder.Configuration.AddJsonFile(Path.Combine(exePath, "appsettings.json"), optional: true);
                builder.Configuration.AddJsonFile(Path.Combine(exePath, "logging.json"), optional: true);

                // Clear default logging providers
                builder.Logging.ClearProviders();

                // Set the default console color to blue
                Console.ForegroundColor = ConsoleColor.Green;
                Console.BackgroundColor = ConsoleColor.Black;

                Console.WriteLine();
                Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);
                Console.WriteLine("Windows 11 Setup Upgrade analyzer (aka Log Cruncher)", Console.ForegroundColor = ConsoleColor.Green);
                Console.WriteLine("Version: {0}", System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly()?.Location ?? string.Empty).FileVersion?.ToString() ?? "Unknown version");
                Console.WriteLine("This program is licensed under GPL 3.0");
                Console.WriteLine();
                Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);


                if (null == args || args.Length == 0)
                {
                    isService = false;
                    isConsole = false;

                    Console.WriteLine("Usage: LogCruncher.exe [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --service, -s       Run the worker service as a Windows Service");
                    Console.WriteLine("  --console, -c       Run the worker service as a console application");
                    Console.WriteLine("  --LogsPath, -L      Specify the logs root path used together with --Console");
                    Console.WriteLine("  --OutPath, -O       Specify the output path used together with --Console");
                    Console.WriteLine("  --help, -h          Show this help message");
                    Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);
                    return;
                }

                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--service":
                        case "-s":
                            isService = true;
                            break;
                        case "--console":
                        case "-c":
                            isConsole = true;
                            break;
                        case "--LogsPath":
                        case "-L":
                            if (i + 1 < args.Length)
                            {
                                logsRootPath = args[++i];
                            }
                            break;
                        case "--OutPath":
                        case "-O":
                            if (i + 1 < args.Length)
                            {
                                outputPath = args[++i];
                            }
                            break;
                        case "--help":
                        case "-h":
                            Console.WriteLine("Usage: LogCruncher.exe [options]");
                            Console.WriteLine("Options:");
                            Console.WriteLine("  --service, -s       Run the worker service as a Windows Service");
                            Console.WriteLine("  --console, -c       Run the worker service as a console application");
                            Console.WriteLine("  --LogsPath, -L      Specify the logs root path used together with --Console");
                            Console.WriteLine("  --OutPath, -O       Specify the output path used together with --Console");
                            Console.WriteLine("  --help, -h          Show this help message");
                            Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);
                            return;
                        default:
                            Console.WriteLine("Invalid argument. Use --help or -h for usage information.");
                            Console.WriteLine("");
                            Console.WriteLine("Usage: LogCruncher.exe [options]");
                            Console.WriteLine("Options:");
                            Console.WriteLine("  --service, -s       Run the worker service as a Windows Service");
                            Console.WriteLine("  --console, -c       Run the worker service as a console application");
                            Console.WriteLine("  --LogsPath, -L      Specify the logs root path used together with --Console");
                            Console.WriteLine("  --OutPath, -O       Specify the output path used together with --Console");
                            Console.WriteLine("  --help, -h          Show this help message");
                            Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);
                            return;
                    }
                }

                // Add Serilog to the builder
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(builder.Configuration)
                    .CreateLogger();

                builder.Logging.AddSerilog(Log.Logger);

                string logo = File.ReadAllText(Path.Combine(exePath, "logo.txt"));
                string[] logoLines = File.ReadAllLines(Path.Combine(exePath, "logo.txt"));


                // Print out the logo
                Console.ForegroundColor = ConsoleColor.Green;

                // Print out the logo
                foreach (string line in logoLines)
                {
                    Console.WriteLine(line);
                }

                Console.WriteLine();

                Console.ResetColor();

                Log.Information("---------------------------------------------------------------------------------------");
                Log.Information("------------------------------------ Starting LogCruncher -----------------------------");
                Log.Information("---------------------------------------------------------------------------------------");


                // check if args contains --schedule
                if (isService)
                {
                    // Read configuration from appsettings.json and load it into LogProcessorSettings
                    builder.Services.Configure<LogProcessorSettings>(builder.Configuration.GetSection(nameof(LogProcessorSettings)));

                    // Print out the configuration
                    var config = builder.Configuration.GetSection(nameof(LogProcessorSettings)).Get<LogProcessorSettings>();

                    if (config == null)
                    {
                        Log.Error("Configuration for LogProcessorSettings is null.");
                    }
                    else
                    {
                        Log.Information("Application {0} with version {1}", Assembly.GetExecutingAssembly()?.GetName().Name, System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly()?.Location ?? string.Empty).FileVersion?.ToString() ?? "Unknown version");
                        Log.Information("LogsRootPath is set to: {LogsRootPath}", config.LogsRootPath);
                        Log.Information("OutputPath is set to: {OutputPath}", config.OutputPath);
                        Log.Information("Arguments: {Arguments}", string.Join(", ", args) ?? "No Arguments");
                        Log.Information("SaveToDatabase is set to: {SaveToDatabase}", config.SaveToDatabase);
                    }

                    // Add services to the container
                    builder.Services.AddSingleton<ISetupACTLogProcessor, SetupACTLogProcessor>();
                    builder.Services.AddSingleton<IHumanReadableOutputParser, HumanReadableOutputParser>();

                    if (config.SaveToDatabase)
                    {
                        Log.Debug("Database saving is enabled. Configuring database context.");

                        builder.Services.Configure<DatabaseConnectionSettings>(builder.Configuration.GetSection("LogProcessorSettings:DatabaseConnectionSettings"));
                        builder.Services.AddSingleton<DatabaseConnectionSettings>(sp => sp.GetRequiredService<IOptions<DatabaseConnectionSettings>>().Value);

                        // Get database provider and connection string from configuration
                        var dbConfiguration = builder.Configuration.GetSection("LogProcessorSettings:DatabaseConnectionSettings").Get<DatabaseConnectionSettings>();
                        Log.Information("Read DatabaseProvider from configuration file: {DatabaseProvider}", dbConfiguration?.DatabaseProvider);
                        Log.Information("Read DefaultConnection from configuration file: {DefaultConnection}", dbConfiguration?.DefaultConnection );

                        if (string.IsNullOrEmpty(dbConfiguration.DatabaseProvider))
                        {
                            Log.Error("DatabaseProvider is not specified in the configuration.");
                            return;
                        }

                        // Register HumanReadableAnalysisContext with the appropriate database provider
                        builder.Services.AddDbContext<HumanReadableAnalysisContext>((serviceProvider, options) =>
                        {
                            var connectionSettings = serviceProvider.GetRequiredService<DatabaseConnectionSettings>();
                            var logger = serviceProvider.GetRequiredService<ILogger<HumanReadableAnalysisContext>>();

                            // Configure the database provider based on the settings
                            if (connectionSettings.DatabaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogDebug("Using SQL Server with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                                options.UseSqlServer(connectionSettings.DefaultConnection, sqlOptions =>
                                {
                                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                });
                            }
                            else if (connectionSettings.DatabaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogDebug("Using PostgreSQL with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                                options.UseNpgsql(connectionSettings.DefaultConnection);
                            }
                            else if (connectionSettings.DatabaseProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogDebug("Using MySQL with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                                options.UseMySql(connectionSettings.DefaultConnection, ServerVersion.AutoDetect(connectionSettings.DefaultConnection));
                            }
                            else
                            {
                                logger.LogError("Unsupported database provider: {DatabaseProvider}", connectionSettings.DatabaseProvider);
                                throw new InvalidOperationException("Unsupported database provider.");
                            }

                            options.EnableSensitiveDataLogging();
                        });


                        // Add database context
                        Log.Debug("Configuring database context with provider: {DbProvider}", dbConfiguration.DatabaseProvider);
                        builder.Services.AddDbContext<ACTLogAnalysisContext>((serviceProvider, options) =>
                        {
                            var connectionSettings = serviceProvider.GetRequiredService<DatabaseConnectionSettings>();

                            // Configure the database provider based on the settings
                            if (connectionSettings.DatabaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Debug("Using SQL Server with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                                options.UseSqlServer(connectionSettings.DefaultConnection, sqlOptions =>
                                {
                                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                });
                            }
                            else if (connectionSettings.DatabaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Debug("Using PostgreSQL with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                                options.UseNpgsql(connectionSettings.DefaultConnection);
                            }
                            else if (connectionSettings.DatabaseProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Debug("Using MySQL with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                                options.UseMySql(connectionSettings.DefaultConnection, ServerVersion.AutoDetect(connectionSettings.DefaultConnection));
                            }
                            else
                            {
                                Log.Error("Unsupported database provider: {DatabaseProvider}", connectionSettings.DatabaseProvider);
                                throw new InvalidOperationException("Unsupported database provider.");
                            }
                        });


                        // retrieve the instance of SetupACTLogProcessor
                        var processor = builder.Services.BuildServiceProvider().GetRequiredService<ISetupACTLogProcessor>();
                        (processor as SetupACTLogProcessor)?.InitializeDatabase();

                    }
                    else
                    {
                        Log.Debug("Database saving is disabled. Skipping database configuration.");
                    }

                    // Add Quartz to the services collection
                    builder.Services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));

                    var quartzOptions = builder.Configuration.GetSection("Quartz:QuartzScheduler").Get<QuartzOptions>();

                    if (quartzOptions == null)
                    {
                        Log.Error("Failed to load QuartzOptions from configuration.");
                        return;
                    }
                    else
                    {
                        Log.Debug("Retrieval of QuartzOptions from configuration was successful.");
                    }


                    // Add Quartz services
                    builder.Services.AddQuartz(q =>
                    {
                        // Configure jobs and triggers from configuration
                        var jobs = builder.Configuration.GetSection("Quartz:QuartzJobs").GetChildren();

                        foreach (var jobConfig in jobs)
                        {
                            var jobName = jobConfig["JobName"];
                            var jobGroup = jobConfig["JobGroup"];
                            var jobDescription = jobConfig["JobDescription"];

                            if (string.IsNullOrEmpty(jobName))
                            {
                                Log.Error("JobName is null or empty in configuration.");
                                continue;
                            }

                            if (jobName == "LogsAnalysisJob")
                            {
                                var jobKey = new JobKey(jobName, jobGroup);
                                q.AddJob<LogsAnalysisJob>(j => j
                                    .WithIdentity(jobKey)
                                    .WithDescription(jobDescription));

                                q.AddTrigger(t => t
                                    .WithIdentity(jobConfig["TriggerName"], jobConfig["TriggerGroup"])
                                    .ForJob(jobKey)
                                    .WithCronSchedule(jobConfig["CronExpression"]));

                                Log.Information("Configured job: {jobName} with trigger: {triggerName}", jobName, jobConfig["TriggerName"]);
                                // print the job schedule in human readable format
                                var cronExpression = jobConfig["CronExpression"];
                                var cron = new CronExpression(cronExpression);
                                var nextFireTime = cron.GetNextValidTimeAfter(DateTimeOffset.Now);
                                Log.Information("Next fire time for {jobName} occurs at: {nextFireTime} ", jobName, nextFireTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                            }

                            if (jobName == "HumanReadableFileProcessorJob")
                            {
                                var jobKey = new JobKey(jobName, jobGroup);
                                q.AddJob<HumanReadableFileProcessorJob>(j => j
                                    .WithIdentity(jobKey)
                                    .WithDescription(jobDescription));

                                q.AddTrigger(t => t
                                    .WithIdentity(jobConfig["TriggerName"], jobConfig["TriggerGroup"])
                                    .ForJob(jobKey)
                                    .WithCronSchedule(jobConfig["CronExpression"]));
                                
                                Log.Information("Configured job: {jobName} with trigger: {triggerName}", jobName, jobConfig["TriggerName"]);
                                // print the job schedule in human readable format
                                var cronExpression = jobConfig["CronExpression"];
                                var cron = new CronExpression(cronExpression);
                                var nextFireTime = cron.GetNextValidTimeAfter(DateTimeOffset.Now);
                                Log.Information("Next fire time for {jobName} occurs at: {nextFireTime} ", jobName, nextFireTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                            }

                            if (jobName == "SetupACTLogProcessorJob")
                            {
                                var jobKey = new JobKey(jobName, jobGroup);
                                q.AddJob<SetupActLogProcessorJob>(j => j
                                    .WithIdentity(jobKey)
                                    .WithDescription(jobDescription));
                                q.AddTrigger(t => t
                                    .WithIdentity(jobConfig["TriggerName"], jobConfig["TriggerGroup"])
                                    .ForJob(jobKey)
                                    .WithCronSchedule(jobConfig["CronExpression"]));

                                Log.Information("Configured job: {jobName} with trigger: {triggerName}", jobName, jobConfig["TriggerName"]);
                                // print the job schedule in human readable format
                                var cronExpression = jobConfig["CronExpression"];
                                var cron = new CronExpression(cronExpression);
                                var nextFireTime = cron.GetNextValidTimeAfter(DateTimeOffset.Now);
                                Log.Information("Next fire time for {jobName} occurs at: {nextFireTime} ", jobName, nextFireTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                        }
                    });
  

                    Log.Information("---------------------------------------------------------------------------------------");

                    // Add Quartz.NET hosted service
                    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

                    builder.Services.AddSingleton(provider => provider.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);

                    builder.Services.ConfigureOptions<WorkerJobsSetup>();

                    // Add Windows Service
                    builder.Services.AddWindowsService(options =>
                    {
                        options.ServiceName = "LogCruncher";
                    });

                    var app = builder.Build();

                    // Run the host
                    await app.RunAsync();
                }


                if (isConsole)
                {

                    //builder.Configuration.AddCommandLine(args);

                    // Validate that the arguments are provided
                    if (string.IsNullOrEmpty(logsRootPath))
                    {
                        Log.Error("LogsRootPath is required.");
                        Console.WriteLine("Error: LogsRootPath is required.");
                        return;
                    }

                    if (string.IsNullOrEmpty(outputPath))
                    {
                        Log.Error("OutputPath is required.");
                        Console.WriteLine("Error: OutputPath is required.");
                        return;
                    }

                    //remove the ' char from logsRootPath and outputPath
                    logsRootPath = logsRootPath.Replace("'", "");
                    outputPath = outputPath.Replace("'", "");

                    // Optionally, check if the paths exist
                    if (!Path.IsPathFullyQualified(logsRootPath) || !Directory.Exists(logsRootPath))
                    {
                        Log.Error("LogsRootPath does not exist or is not a fully qualified path: {LogsRootPath}", logsRootPath);
                        Console.WriteLine("Error: LogsRootPath does not exist or is not a fully qualified path: {0}", logsRootPath);
                        return;
                    }

                    if (!Path.IsPathFullyQualified(outputPath) || !Directory.Exists(outputPath))
                    {
                        Log.Error("OutputPath does not exist or is not a fully qualified path: {OutputPath}", outputPath);
                        Console.WriteLine("Error: OutputPath does not exist or is not a fully qualified path: {0}", outputPath);
                        return;
                    }

                    // check if outputPath is writable
                    if (!Directory.CreateDirectory(outputPath + "test").Exists)
                    {
                        Log.Error("OutputPath is not writable: {OutputPath}", outputPath);
                        Console.WriteLine("Error: OutputPath is not writable: {0}", outputPath);
                        return;
                    }


                    builder.Services.Configure<LogProcessorSettings>(options =>
                    {
                        options.LogsRootPath = logsRootPath;
                        options.OutputPath = outputPath;
                    });

                    Log.Debug("LogsRootPath is set to: {LogsRootPath}", logsRootPath);
                    Log.Debug("OutputPath is set to: {OutputPath}", outputPath);

                    // Add services to the container
                    builder.Services.AddSingleton<ISetupACTLogProcessor, SetupACTLogProcessor>();
                    builder.Services.AddSingleton<IHumanReadableOutputParser, HumanReadableOutputParser>();


                    var serviceProvider = builder.Services.BuildServiceProvider();
                    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                    builder.Build();

                    // Run the application
                    var humanReadableOutputParser = serviceProvider.GetRequiredService<IHumanReadableOutputParser>();
                    await humanReadableOutputParser.ParseHumanReadableFilesAsync();

                    var processor = serviceProvider.GetRequiredService<ISetupACTLogProcessor>();
                    await processor.ProcessLogFilesAsync();
                }

                Console.WriteLine("Log processing completed.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
