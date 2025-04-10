namespace Windows.Utils.Macinator.Config
{
    public class LogProcessorSettings
    {
        public required string LogsRootPath { get; set; }
        public required string OutputPath { get; set; }
        public required bool SaveToDatabase { get; set; }

        // Connection strings section
        public DatabaseConnectionSettings ConnectionStrings { get; set; } = new DatabaseConnectionSettings();

    }

    public class DatabaseConnectionSettings
    {

        // Database provider (e.g., SqlServer, PostgreSQL, MySQL)
        public string DatabaseProvider { get; set; } = "SqlServer";
        public string DefaultConnection { get; set; } //= "Server=localhost;Database=LogCruncher;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
