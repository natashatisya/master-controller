using Master.API.Data;
using Master.API.DTOs;
using Master.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Master.API.Controllers;

[ApiController]
[Route("api/hosts")]
public class HostController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<HostController> _logger;

    public HostController(AppDbContext db, ILogger<HostController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllHosts()
    {
        var hosts = await _db.Hosts
            .Select(h => new HostStatusDto
            {
                Id = h.Id,
                Hostname = h.Hostname,
                IpAddress = h.IpAddress,
                OperatingSystem = h.OperatingSystem,
                Status = h.Status,
                LastHeartbeat = h.LastHeartbeat,
                CpuUsage = h.CpuUsage,
                MemoryUsage = h.MemoryUsage,
                DiskUsage = h.DiskUsage
            })
            .ToListAsync();

        return Ok(hosts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHost(Guid id)
    {
        var host = await _db.Hosts.FindAsync(id);
        if (host == null)
            return NotFound(new { message = "Host not found." });

        return Ok(new HostStatusDto
        {
            Id = host.Id,
            Hostname = host.Hostname,
            IpAddress = host.IpAddress,
            OperatingSystem = host.OperatingSystem,
            Status = host.Status,
            LastHeartbeat = host.LastHeartbeat,
            CpuUsage = host.CpuUsage,
            MemoryUsage = host.MemoryUsage,
            DiskUsage = host.DiskUsage
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterHostDto dto)
    {
        try
        {
            var existing = await _db.Hosts
                .FirstOrDefaultAsync(h => h.Hostname == dto.Hostname && h.IpAddress == dto.IpAddress);

            if (existing != null)
            {
                existing.Status = "Online";
                existing.LastHeartbeat = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new RegisterHostResponseDto
                {
                    Id = existing.Id,
                    ApiKey = existing.ApiKey,
                    Message = "Host re-registered successfully."
                });
            }

            var host = new HostEntity
            {
                Hostname = dto.Hostname,
                IpAddress = dto.IpAddress,
                OperatingSystem = dto.OperatingSystem,
                Capabilities = dto.Capabilities,
                Status = "Online",
                ApiKey = Guid.NewGuid().ToString("N"),
                RegisteredAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow
            };

            _db.Hosts.Add(host);
            await _db.SaveChangesAsync();

            _logger.LogInformation("New host registered: {Hostname} ({IpAddress})", host.Hostname, host.IpAddress);

            return Ok(new RegisterHostResponseDto
            {
                Id = host.Id,
                ApiKey = host.ApiKey,
                Message = "Host registered successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering host.");
            return StatusCode(500, new { message = "Error registering host." });
        }
    }

    [HttpPost("{id}/heartbeat")]
    public async Task<IActionResult> Heartbeat(Guid id, [FromBody] HeartbeatDto dto)
    {
        var host = await _db.Hosts.FindAsync(id);
        if (host == null)
            return NotFound(new { message = "Host not found." });

        host.LastHeartbeat = DateTime.UtcNow;
        host.Status = "Online";
        host.CpuUsage = dto.CpuUsage;
        host.MemoryUsage = dto.MemoryUsage;
        host.DiskUsage = dto.DiskUsage;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Heartbeat received." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deregister(Guid id)
    {
        var host = await _db.Hosts.FindAsync(id);
        if (host == null)
            return NotFound(new { message = "Host not found." });

        _db.Hosts.Remove(host);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Host deregistered: {Hostname}", host.Hostname);

        return Ok(new { message = "Host deregistered successfully." });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var total = await _db.Hosts.CountAsync();
        var online = await _db.Hosts.CountAsync(h => h.Status == "Online");
        var offline = await _db.Hosts.CountAsync(h => h.Status == "Offline");

        return Ok(new
        {
            TotalHosts = total,
            OnlineHosts = online,
            OfflineHosts = offline
        });
    }
}