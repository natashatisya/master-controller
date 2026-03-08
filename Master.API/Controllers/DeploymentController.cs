using Master.API.Data;
using Master.API.DTOs;
using Master.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Master.API.Controllers;

[ApiController]
[Route("api/deployments")]
public class DeploymentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<DeploymentController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DeploymentController(AppDbContext db, ILogger<DeploymentController> logger, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllDeployments()
    {
        var deployments = await _db.Deployments
            .Include(d => d.Host)
            .OrderByDescending(d => d.StartedAt)
            .Select(d => new
            {
                d.Id,
                d.AppName,
                d.ScriptName,
                d.Status,
                d.Logs,
                d.StartedAt,
                d.CompletedAt,
                HostName = d.Host != null ? d.Host.Hostname : "Unknown"
            })
            .ToListAsync();

        return Ok(deployments);
    }

    [HttpPost("{hostId}/deploy")]
    public async Task<IActionResult> Deploy(Guid hostId, [FromBody] DeployRequestDto dto)
    {
        var host = await _db.Hosts.FindAsync(hostId);
        if (host == null)
            return NotFound(new { message = "Host not found." });

        if (host.Status != "Online")
            return BadRequest(new { message = "Host is offline. Cannot deploy." });

        var deployment = new Deployment
        {
            HostId = hostId,
            AppName = dto.AppName,
            ScriptName = dto.ScriptName,
            Status = "Pending",
            StartedAt = DateTime.UtcNow
        };

        _db.Deployments.Add(deployment);
        await _db.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = JsonSerializer.Serialize(new
                {
                    DeploymentId = deployment.Id,
                    ScriptName = dto.ScriptName,
                    AppName = dto.AppName
                });

                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var agentUrl = $"http://{host.IpAddress}:5100/api/agent/execute";

                var response = await client.PostAsync(agentUrl, content);
                var result = await response.Content.ReadAsStringAsync();

                using var scope = HttpContext.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dep = await db.Deployments.FindAsync(deployment.Id);
                if (dep != null)
                {
                    dep.Status = response.IsSuccessStatusCode ? "Success" : "Failed";
                    dep.Logs = result;
                    dep.CompletedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deploy command to agent.");
            }
        });

        return Ok(new DeployResponseDto
        {
            DeploymentId = deployment.Id,
            Status = "Pending",
            Message = $"Deployment of {dto.AppName} started on {host.Hostname}."
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDeployment(Guid id)
    {
        var deployment = await _db.Deployments
            .Include(d => d.Host)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (deployment == null)
            return NotFound(new { message = "Deployment not found." });

        return Ok(new
        {
            deployment.Id,
            deployment.AppName,
            deployment.ScriptName,
            deployment.Status,
            deployment.Logs,
            deployment.StartedAt,
            deployment.CompletedAt,
            HostName = deployment.Host?.Hostname
        });
    }
}