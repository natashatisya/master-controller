namespace Master.API.DTOs;

public class RegisterHostDto
{
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Capabilities { get; set; } = string.Empty;
}

public class RegisterHostResponseDto
{
    public Guid Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class HeartbeatDto
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
}

public class DeployRequestDto
{
    public string AppName { get; set; } = string.Empty;
    public string ScriptName { get; set; } = string.Empty;
}

public class DeployResponseDto
{
    public Guid DeploymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class HostStatusDto
{
    public Guid Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
}