namespace Windows.Utils.Macinator.Config
{
    public class LogProcessorSettings
    {
        public required string LogsRootPath { get; set; }
        public required string OutputPath { get; set; }
        public string? CompatIssuesFolder { get; set; } = "compat_issues";
        public string? UpgradeIssuesFolder { get; set; } = "upgrade_issues";
        public required string CompatLogFilePattern { get; set; } = "*.4.0.1_APPRAISER_HumanReadable.xml";
        public required string SetupActLogFilePattern { get; set; } = "SetupAct.log";
        public required bool DeletePreviousFiles { get; set; } = false;
        public required bool SaveToDatabase { get; set; } = false;
        public required bool SaveToFile { get; set; } = true;

        // Connection strings section
        public DatabaseConnectionSettings ConnectionStrings { get; set; } = new DatabaseConnectionSettings();
    }

    public class DatabaseConnectionSettings
    {
        // Database provider (e.g., SqlServer, PostgreSQL, MySQL)
        public string? DatabaseProvider { get; set; } = "SqlServer";
        public string? DefaultConnection { get; set; } //= "Server=localhost;Database=LogCruncher;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
