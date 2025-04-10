# :loudspeaker: Log Cruncher | Windows 11 Upgrade Log Analyzer :rocket:

![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white)
![GitHub](https://img.shields.io/badge/github-%23121011.svg?style=for-the-badge&logo=github&logoColor=white)
![Nuget](https://img.shields.io/badge/nuget-%230077B5.svg?style=for-the-badge&logo=nuget&logoColor=white)
![.Windows11](https://img.shields.io/badge/Windows%2011-0078D4?style=for-the-badge&logo=windows&logoColor=white)

## Overview
You have tons of Windows 11 upgrade logs and you don't know how to analyze them?
Log Cruncher is a tool designed to efficiently analyze and process log files. It is particularly useful for managing large amounts of logs generated during the upgrade to Windows 11, providing structured output in JSON format for easy consultation and analysis, or directly saved within a database.
Windows 11 Upgrade produces a lot of logs, and it is difficult to analyze them all. Log Cruncher helps you to process these logs and extract useful information from them. It can be used to analyze logs from Windows 11 upgrade.

The solution is supposed to analyze specific log files generated during the upgrade to Windows 11, such as:

- Appraiser file `.4.4.0.1_APPRAISER_HumanReadable.xml`
- Windows 11 Upgrade setup file `SetupACT.log`

These files are generated during the upgrade process and can be found within `C:\$WINDOWS.~BT\Source\Panther` and contain information about the assessment of the system and the upgrade process itself. The tool is designed to extract relevant information from these files and save it in a humanr readable format for further analysis.


## Requirements
- **Operating System**: any suppported Windows client/server version  Windows 10/11 or Windows Server 2016/2019/2022). Ideally this solution can be used also on any OS that supports .NET Core 9, although its worker service usage is only supported on Windows.
- **Runtime**: .NET Core 9.0. You can download it from [here](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-9.0.3-windows-x64-installer).
- **SQL engine**: If saving output to a database, a running SQL engine is required: SQL Server, MySQL, PostgreSQL. The tool is designed to work with any of these engines, but the connection string must be configured accordingly in the `appsettings.json` file.

## Features
- **Log processing in console mode**: Run the tool manually to process the specified log files either locally or from a remote share.
- **Log processing as a service**: Automate log processing every N minutes, given a root path where to look for logs.
- **Structured output**: Output files are saved in JSON format, organized in a dedicated folder for each computer.
- **Database support**: The tool can be configured to save the output directly into a database, allowing for easy querying and analysis.


## Usage
### Configuration
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


## How to run
### Execution
1. Download latest release of the solution from [Releases](https://github.com/robgrame/Windows11.Upgrade.LogCruncher/releases)
1. Extract the contents of the archive `LogMacinator_vX.X.X.zip`.
1. Locate the executable `LogCruncher.exe` and the `appsettings.json` file.

#### Console Mode
Modify the `appsettings.json` file to set the paths for logs and output. Then, run the tool in console mode to process the logs immediately:

Run the tool in console mode to process the logs immediately:

```bash
LogCruncher.exe --console -LogsRootPath C:\\MACINATOR\\Clients\\ -OutputPath C:\\MACINATOR\\
```
or
```bash
LogCruncher.exe -c -L C:\\MACINATOR\\Clients\\ -O C:\\MACINATOR\\
```
#### Service Mode
To run the tool as a service, use the following command:
```bash
LogCruncher.exe --service
```
or
```bash
LogCruncher.exe -s
```
This will start the tool as a background service, processing logs every N minutes. The service will look for logs in the specified root path and save the output in the designated output path.

### Output
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

## Troubleshooting
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

