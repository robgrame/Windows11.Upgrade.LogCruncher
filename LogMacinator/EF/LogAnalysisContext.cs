using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Configuration;
using Windows.Utils.Macinator.Config;


namespace Windows.Utils.Macinator.EF
{
    public class LogAnalysisContext : DbContext, IAsyncDisposable
    {
        private readonly DatabaseConnectionSettings _connectionSettings;
        private ILogger<LogAnalysisContext> _logger;

        public LogAnalysisContext(DbContextOptions<LogAnalysisContext> options,DatabaseConnectionSettings connectionSettings, ILogger<LogAnalysisContext> logger)
            :base(options)
        {
            _connectionSettings = connectionSettings;
            _logger = logger;
        }

        public DbSet<EFSystemInfo> SystemInfos { get; set; }
        public DbSet<EFOperationResult> OperationResults { get; set; }
        public DbSet<EFUncompleteAction> UncompleteActions { get; set; }
        public DbSet<EFLogAnalysisResult> LogAnalysisResults { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                _logger.LogTrace("Configuring DbContext with connection string: {ConnectionString}", _connectionSettings.DefaultConnection);
                ConfigureDatabaseProvider(optionsBuilder, _connectionSettings);
            }
            else
            {
                _logger.LogDebug("DbContext is already configured. Skipping configuration.");
            }
        }

        private void ConfigureDatabaseProvider(DbContextOptionsBuilder optionsBuilder, DatabaseConnectionSettings connectionSettings)
        {
            _logger.LogTrace("Configuring database provider: {DatabaseProvider}", connectionSettings.DatabaseProvider);
            _logger.LogTrace("Connection string: {ConnectionString}", connectionSettings.DefaultConnection);

            if ( connectionSettings.DatabaseProvider.Equals("SqlServer",StringComparison.OrdinalIgnoreCase))
            {
                // Use SQL Server with the specified connection string
                _logger.LogTrace("Using SQL Server with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                optionsBuilder.UseSqlServer(connectionSettings.DefaultConnection, sqlOptions =>
                {
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            }
            else if (connectionSettings.DatabaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                // Use PostgreSQL with the specified connection string
                _logger.LogTrace("Using PostgreSQL with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                optionsBuilder.UseNpgsql(connectionSettings.DefaultConnection);
            }
            else if (connectionSettings.DatabaseProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                // Use MySQL with the specified connection string
                _logger.LogTrace("Using MySQL with connection string: {ConnectionString}", connectionSettings.DefaultConnection);
                optionsBuilder.UseSqlServer(connectionSettings.DefaultConnection, sqlOptions =>
                {
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

            }
            else
            {
                _logger.LogError("Unsupported database provider: {DatabaseProvider}", connectionSettings.DatabaseProvider);
                throw new InvalidOperationException("Unsupported database provider.");
            }

            optionsBuilder.EnableSensitiveDataLogging();
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EFLogAnalysisResult>()
                .HasOne(l => l.SystemInfo)
                .WithOne()
                .HasForeignKey<EFLogAnalysisResult>(l => l.SystemInfoId); // Fixed foreign key reference

            modelBuilder.Entity<EFLogAnalysisResult>()
                 .HasMany(l => l.Results)
                 .WithOne(r => r.LogAnalysisResult) // Specify the navigation property
                 .HasForeignKey(r => r.LogAnalysisResultId); // Use the new foreign key

            modelBuilder.Entity<EFLogAnalysisResult>()
                .HasOne(l => l.UncompleteAction)
                .WithOne()
                .HasForeignKey<EFUncompleteAction>(l => l.Id); // Ensured correct foreign key reference

            // Example: Adjust configurations for PostgreSQL
            if (Database.IsNpgsql())
            {
                modelBuilder.Entity<EFLogAnalysisResult>()
                    .Property(e => e.Id)
                    .HasColumnType("uuid");
            }

            // Example: Adjust configurations for MySQL
            if (Database.IsMySql())
            {
                modelBuilder.Entity<EFLogAnalysisResult>()
                    .Property(e => e.Id)
                    .HasColumnType("char(36)");
            }
        }
    }
}
