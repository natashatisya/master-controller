namespace Master.API.Models;

public class HostEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Capabilities { get; set; } = string.Empty;
    public string Status { get; set; } = "Offline";
    public string ApiKey { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
}