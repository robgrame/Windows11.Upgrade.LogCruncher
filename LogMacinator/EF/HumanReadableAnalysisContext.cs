using LogCruncher.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Windows.Utils.Macinator.Config;

namespace LogCruncher.EF
{
    internal class HumanReadableAnalysisContext : DbContext
    {
        private readonly DatabaseConnectionSettings _connectionSettings;
        private ILogger<HumanReadableAnalysisContext> _logger;

        public HumanReadableAnalysisContext(DbContextOptions<HumanReadableAnalysisContext> options, DatabaseConnectionSettings connectionSettings, ILogger<HumanReadableAnalysisContext> logger)
            : base(options)
        {
            _connectionSettings = connectionSettings;
            _logger = logger;
        }
        public DbSet<HumanReadableOutputEntity> HumanReadableOutputs { get; set; }
        public DbSet<RunInfosEntity> RunInfos { get; set; }
        public DbSet<AssetEntity> Assets { get; set; }
        public DbSet<PropertyEntity> Properties { get; set; }
        public DbSet<PropertyListEntity> CompatIssuesEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Configuring model for PropertyListEntity...");
            modelBuilder.Entity<HumanReadableOutputEntity>().ToTable("HumanReadableOutputs");

            modelBuilder.Entity<RunInfosEntity>().ToTable("RunInfos");
            modelBuilder.Entity<AssetEntity>().ToTable("Assets");
            modelBuilder.Entity<PropertyEntity>().ToTable("Properties");

            modelBuilder.Entity<PropertyListEntity>().ToTable("CompatIssuesEntities");
            base.OnModelCreating(modelBuilder);
        }
    }
}