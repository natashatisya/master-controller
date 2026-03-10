namespace Master.API.Models;

public class Deployment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HostId { get; set; }
    public HostEntity? Host { get; set; }
    public string AppName { get; set; } = string.Empty;
    public string ScriptName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string Logs { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
}