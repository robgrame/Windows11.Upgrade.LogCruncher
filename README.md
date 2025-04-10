# :loudspeaker: Log Cruncher | Windows 11 Upgrade Log Analyzer :rocket:

![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white)
![GitHub](https://img.shields.io/badge/github-%23121011.svg?style=for-the-badge&logo=github&logoColor=white)
![Nuget](https://img.shields.io/badge/nuget-%230077B5.svg?style=for-the-badge&logo=nuget&logoColor=white)
![.Windows11](https://img.shields.io/badge/Windows%2011-0078D4?style=for-the-badge&logo=windows&logoColor=white)


## Overview :mag:
You have tons of Windows 11 upgrade logs and you don't know how to analyze them?
Log Cruncher is a tool designed to efficiently analyze and process log files. It is particularly useful for managing large amounts of logs generated during the upgrade to Windows 11, providing structured output in JSON format for easy consultation and analysis, or directly saved within a database.
Windows 11 Upgrade produces a lot of logs, and it is difficult to analyze them all. Log Cruncher helps you to process these logs and extract useful information from them. It can be used to analyze logs from Windows 11 upgrade.

The solution is supposed to analyze specific log files generated during the upgrade to Windows 11, such as:

- Appraiser file `.4.4.0.1_APPRAISER_HumanReadable.xml`
- Windows 11 Upgrade setup file `SetupACT.log`

These files are generated during the upgrade process and can be found within `C:\$WINDOWS.~BT\Source\Panther` and contain information about the assessment of the system and the upgrade process itself. The tool is designed to extract relevant information from these files and save it in a humanr readable format for further analysis.


## Requirements :minidisc:
- **Operating System**: any suppported Windows client/server version  Windows 10/11 or Windows Server 2016/2019/2022). Ideally this solution can be used also on any OS that supports .NET Core 9, although its worker service usage is only supported on Windows.
- **Runtime**: .NET Core 9.0. You can download it from [here](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-9.0.3-windows-x64-installer).
- **SQL engine**: If saving output to a database, a running SQL engine is required: SQL Server, MySQL, PostgreSQL. The tool is designed to work with any of these engines, but the connection string must be configured accordingly in the `appsettings.json` file.

## Features
- **Log processing in console mode**: Run the tool manually to process the specified log files either locally or from a remote share.
- **Log processing as a service**: Automate log processing every N minutes, given a root path where to look for logs.
- **Structured output**: Output files are saved in JSON format, organized in a dedicated folder for each computer.
- **Database support**: The tool can be configured to save the output directly into a database, allowing for easy querying and analysis.
- **Scheduling**: The service can be scheduled to run at specific intervals leveraging *Quartz.NET* Nuget package making use of CRON expressions.
- **Logging**: The tool uses *Serilog* for logging, providing detailed logs of the processing steps and any errors encountered. 


## Usage
### Configuration :gear:
Before using Log Cruncher, configure the `appsettings.json` file by modifying the `LogProcessorSettings` section:

- **LogsRootPath**: The path where the log files to be processed are located.
- **OutputPath**: The path where the output files in JSON format will be saved. The tool will create a folder named after the computer where the tool is executed, and the output files will be saved inside this folder.


   ```json
    "LogProcessorSettings": {
        "LogsRootPath": "C:\\MACINATOR\\Repository",
        "OutputPath": "C:\\MACINATOR\\"
    }
   ```
> Note: Paths must be written using double backslashes `\\`.


### How to run :runner:

1. Download latest release of the solution from [Releases](https://github.com/robgrame/Windows11.Upgrade.LogCruncher/releases)
1. Extract the contents of the archive `LogMacinator_vX.X.X.zip`.
1. Locate the executable `LogCruncher.exe` and the `appsettings.json` file.

#### Console Mode :computer:
Modify the `appsettings.json` file to set the paths for logs and output. Then, run the tool in console mode to process the logs immediately:

Run the tool in console mode to process the logs immediately:

```bash
LogCruncher.exe --console -LogsRootPath C:\\MACINATOR\\Clients\\ -OutputPath C:\\MACINATOR\\
```
or
```bash
LogCruncher.exe -c -L C:\\MACINATOR\\Clients\\ -O C:\\MACINATOR\\
```
#### Service Mode :kimono:
Before proceeding with running the tool as a service, ensure that you have configured the `appsettings.json` with exptected schedule and paths. The service will run in the background and process logs every N minutes.

The schedule of the service can be configured in the `appsettings.json` file under the `Quartz` section:
You can modify the values as needed to match your environment and requirements.
   The scheduling configuration is based on Quartz.NET, a powerful and flexible scheduling library for .NET applications. The configuration includes settings for the desired interval for the service to run, based on Quartz.NET cron expressions.

   The solution is comprised of 2 jobs:
   - **SetupACTLogProcessorJob**: This job processes the `SetupACT.log` file and extracts relevant information.
   - **HumanReadableFileProcessorJob**: This job processes the `.4.4.0.1_APPRAISER_HumanReadable.xml` file and extracts relevant information.

   Each job has its own configuration settings, including:
   - *JobName*: The name of the job.
   - *JobDescription*: A brief description of the job.
   - *JobGroup*: The group to which the job belongs.   
   - *TriggerName*: The name of the trigger that starts the job.
   - *TriggerGroup*: The group to which the trigger belongs.
   - *CronExpression*: The cron expression that defines the schedule for the service. You can modify this expression to set the desired interval for the service to run. For example:
     - `0 0/5 * * * ?` - Every 5 minutes
     - `0 0 12 * * ?` - Every day at noon
     - `0 0 8-18 ? * MON-FRI` - Every hour from 8 AM to 6 PM on weekdays
     For complete configuration, you can use the following example as a reference:
     ```json
        {
          "Quartz": {
            "CronExpression": "0 0/5 * * * ?"
          }
        }
     ```
     For a complete list of cron expressions and their meanings, you can refer to:
     - [Quartz.NET Cron Trigger documentation](https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/crontrigger.html)
     - [CronMaker](https://www.cronmaker.com/) website
     - [Cron Expression Generator & Explainer - Quartz](https://freeformatter.com/cron-expression-generator-quartz.html)
     
     For reference, here's the excerpt of the appsettings.json file for the Quartz section. This includes settings for the Quartz scheduler instance, thread pool, job store, and job details.
        ```json
            "Quartz": {
                "QuartzScheduler": {
                    "quartz.scheduler.instanceName": "MacinatorScheduler",
                    "quartz.scheduler.instanceId": "AUTO",
                    "quartz.threadPool.type": "Quartz.Simpl.SimpleThreadPool, Quartz",
                    "quartz.threadPool.threadCount": "10",
                    "quartz.threadPool.threadPriority": "Normal",
                    "quartz.jobStore.misfireThreshold": "60000",
                    "quartz.jobStore.type": "Quartz.Simpl.RAMJobStore, Quartz",
                    "quartz.jobStore.clustered": "false"
                },
                "QuartzJobs": [
                    {
                        "JobName": "SetupACTLogProcessorJob",
                        "JobDescription": "Analyze SetupACT.log",
                        "JobGroup": "LogProcessorGroup",
                        "TriggerName": "SetupACTLogProcessorTrigger",
                        "TriggerGroup": "SetupACTLogProcessorTriggerGroup",
                        "CronExpression": "30 */2 * ? * *"
                    },
                    {
                        "JobName": "HumanReadableFileProcessorJob",
                        "JobDescription": "Analyze Humanreadable xml file",
                        "JobGroup": "LogProcessorGroup",
                        "TriggerName": "HumanReadableFileTrigger",
                        "TriggerGroup": "HumanReadableFileTriggerGroup",
                        "CronExpression": "0 0/5 * ? * *"
                    }
                ]
            }
        ```

To run the tool as a service, just use the following command:
```bash
LogCruncher.exe --service
```
or
```bash
LogCruncher.exe -s
```
This will start the tool as a background service, processing logs every N minutes. The service will look for logs in the specified root path and save the output in the designated output path.


### Output :door:
The output files will be saved in JSON format, organized in a folder named after the computer where the tool is executed. The folder structure will look like this:
```
C:\MACINATOR\
    ├── ComputerName1\
    │   ├── AppraiserLog.json
    │   └── SetupACTLog.json
    ├── ComputerName2\
    │   ├── AppraiserLog.json
    │   └── SetupACTLog.json
```
### Database Output :1234:
If you want to save the output directly into a database, you need to change the configuration file appsettings.json accordingly.
1. Set the `SaveToDatabase` option to `true`
1. Set the `DatabaseConnectionSettings` section with the appropriate connection string for your database. The tool supports SQL Server, MySQL, and PostgreSQL. Modify the `ConnectionString` section accordingly:
```json
        "SaveToDatabase": true,
        "DatabaseConnectionSettings": {
            "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=LogCruncher;Trusted_Connection=True;TrustServerCertificate=True;",
            "DatabaseProvider": "SqlServer" // Options: SqlServer, PostgreSQL, MySQL
```

> Note: Make sure to replace the connection string with your actual database connection details.

## Logging :file_cabinet:
The solution is supposed to log to file either when running as a console or as a service.
Logging configuration is defined into a separate file `logging.json`. You can modify the values as needed to match your environment and requirements.

The logging configuration is based on Serilog, a popular logging library for .NET applications. The configuration includes settings for the logging level, output folder path, and file name prefix.
- *MinimumLevel*: The minimum logging level for the service. You can set this to `Information`, `Warning`, `Error`, etc., based on your needs.

- *WriteTo*: The output settings for the logs. In this case, it is configured to write logs to a file.

- *File*: The file settings for the log output. You can modify the following properties:
    - *Path*: The path where the log files will be saved. Make sure this path exists and is accessible by the service.
    - *FileNamePrefix*: The prefix for the log file names. The service will append a timestamp to this prefix to create unique file names.
    - *RollingInterval*: The interval for rolling over the log files. You can set this to `Day`, `Hour`, etc., based on your needs.
    - *RetainedFileCountLimit*: The maximum number of log files to retain. Older files will be deleted when this limit is reached.

    For convenience we recommend not to change the configuration except for log file path and eventually the FileNamePrefix but leave remainder configuration as is.
    In addition set the `rollingInterval` to `Day` and the `retainedFileCountLimit` to 5, so that you can keep a history of the last 5 days of logs.
    For complete configuration, you can use the following example as a reference:

    ```json
    "Serilog": {
        "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
        "MinimumLevel": {
            "Default": "Debug",
            "Override": {
                "Microsoft": "Warning",
                "System": "Warning",
                "LogProcessor": "Debug",
                "Quartz": "Warning"
            }
        },
        "WriteTo": [
            {
                "Name": "Console",
                "Args": {
                    "theme": "Serilog.Sinks.SystemConsole.Themes.SystemConsoleTheme::Literate, Serilog.Sinks.Console",
                    "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] \t [{SourceContext}] {Message:lj} {NewLine}{Exception}"

                }

            },
            {
                "Name": "File",
                "Args": {
                    "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} \t [{Level}] \t [{SourceContext}] \t {Properties} {Message}{NewLine}{Exception}",
                    "path": "C:\\MACINATOR\\Logs\\Macinator.log",
                    "encoding": "System.Text.UTF8Encoding", // utf-8, utf-16, utf-32"
                    "rollingInterval": "Day",
                    "rollOnFileSizeLimit": true,
                    "retainedFileCountLimit": 5,
                    "fileSizeLimitBytes": 10485760,
                    "flushToDiskInterval": 1
                }
            }
        ],
        "Enrich": [ "FromLogContext", "WithMachineName", "WithProcessId", "WithThreadId" ],
        "Properties": {
            "Application": "LogMacinator"
        }
    }
    ```

## Contributing :handshake:
Contributions are welcome! Please follow these steps to contribute:

1. Fork the repository.
2. Create a new branch:
    ```sh
    git checkout -b feature-branch
    ```
3. Make your changes and commit them:
    ```sh
    git commit -m "Description of changes"
    ```
4. Push to the branch:
    ```sh
    git push origin feature-branch
    ```
5. Create a pull request.

## Troubleshooting :hammer:
If you encounter any issues while using Log Cruncher, please check the following:
- Ensure that you have the correct version of .NET Core installed.
- Ensure that the paths specified in the `appsettings.json` file are correct and accessible. 
- Ensure that the log files are present in the specified root path.
- Ensure that Log output path is using double backslashes `\\` in the path.
- Ensure that the output path is writable.
- Ensure that you have the necessary permissions to read/write to the specified paths.
- If saving to a database, ensure that the connection string is correct and the database is accessible from the machine running the tool.
 
## License :card_file_box:
This project is licensed under the GPL 3.0. See the GPL license details file for more details.

## Contact :mailbox_with_no_mail:
For any questions or feedback, you can reach me at [roberto@gramellini.net](mailto:roberto@gramellini.net)

## Acknowledgements
- Thanks to the .NET community for their support and contributions.

## References
- [Quartz.NET](https://www.quartz-scheduler.net/)
- [Serilog](https://serilog.net/)
- [Windows 11 Upgrade Logs](https://docs.microsoft.com/en-us/windows/deployment/update/windows-11-upgrade-logs)
- [Windows 11 Upgrade](https://docs.microsoft.com/en-us/windows/deployment/update/windows-11-upgrade)

### Author
#### About Me :person_frowning:
I am a Cloud Solution Architect and a passionate software engineer with a strong interest in cloud computing and automation. I have experience in developing applications using .NET technologies and have a keen interest in exploring new tools and frameworks. I enjoy solving complex problems and continuously learning to improve my skills.
I believe in the power of open-source software and actively contribute to the community. I am always looking for new challenges and opportunities to grow as a developer.
I am also a strong advocate for best practices in software development, including code quality, testing, and documentation. I strive to write clean, maintainable code and follow industry standards to ensure the success of my projects.

 ![GitHub](https://img.shields.io/badge/github-%23121011.svg?style=for-the-badge&logo=github&logoColor=white)[Roberto Gramellini](https://github.com/robgrame)

![LinkwedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white) [Roberto Gramellini](https://www.linkedin.com/in/robertogramellini/)

