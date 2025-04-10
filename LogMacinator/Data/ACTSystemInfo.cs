public class ACTSystemInfo
{
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
