using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Windows.Utils.Macinator.EF
{

    public class EFSystemInfo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); // Automatically generate a new Guid
        public string? Hostname { get; set; }
        public string? OsVersion { get; set; }
        public string? VM { get; set; }
        public string? FirmwareType { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? BIOSName { get; set; }
        public string? BIOSVersion { get; set; }
        public string? BIOSReleaseDate { get; set; }
        public long TotalMemory { get; set; }
        public int NumberOfPhysicalCPUs { get; set; }
        public int NumberOfLogicalCPUs { get; set; }
        public string? ProcessorManufacturer { get; set; }
        public string? ProcessorName { get; set; }
        public string? ProcessorCaption { get; set; }
        public string? ProcessorArchitecture { get; set; }
        public int ProcessorClock { get; set; }
    }

    public class EFOperationResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); // Automatically generate a new Guid
        public int OperationId { get; set; }
        public string Name { get; set; }
        public bool Executed { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Elapsed { get; set; }

        // Foreign key to EFLogAnalysisResult
        public Guid LogAnalysisResultId { get; set; }
        public EFLogAnalysisResult LogAnalysisResult { get; set; }
    }


    public class EFUncompleteAction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); // Automatically generate a new Guid
        public string ActionName { get; set; }
        public DateTime? StartTime { get; set; }
    }


    public class EFLogAnalysisResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); // Automatically generate a new Guid
        public Guid SystemInfoId { get; set; } // Foreign key property
        public EFSystemInfo SystemInfo { get; set; }
        public List<EFOperationResult> Results { get; set; }
        public List<string> Failures { get; set; }
        public Guid UncompleteActionId { get; set; } // Foreign key property
        public EFUncompleteAction? UncompleteAction { get; set; }
        public string Hash { get; set; }

    }
}
