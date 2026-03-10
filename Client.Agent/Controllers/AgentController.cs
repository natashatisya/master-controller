using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace Client.Agent.Controllers;

[ApiController]
[Route("api/agent")]
public class AgentController : ControllerBase
{
    private readonly ILogger<AgentController> _logger;
    private readonly IConfiguration _config;

    public AgentController(ILogger<AgentController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ExecuteDto dto)
    {
        _logger.LogInformation("Received deploy command for {AppName}, script: {Script}",
            dto.AppName, dto.ScriptName);

        try
        {
            var scriptsPath = _config["Agent:ScriptsPath"] ?? "scripts";
            var scriptFile = Path.Combine(scriptsPath, dto.ScriptName);

            if (!System.IO.File.Exists(scriptFile))
            {
                _logger.LogWarning("Script not found: {Script}", scriptFile);
                return NotFound(new { message = $"Script {dto.ScriptName} not found." });
            }

            var output = new StringBuilder();
            var error = new StringBuilder();

            var isWindows = OperatingSystem.IsWindows();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = isWindows ? "powershell.exe" : "/bin/bash",
                    Arguments = isWindows ? $"-File \"{scriptFile}\"" : scriptFile,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    output.AppendLine(e.Data);
                    _logger.LogInformation("[Script Output] {Line}", e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    error.AppendLine(e.Data);
                    _logger.LogError("[Script Error] {Line}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var success = process.ExitCode == 0;
            var logs = output.ToString() + (error.Length > 0 ? $"\nErrors:\n{error}" : "");

            _logger.LogInformation("Script completed. Success: {Success}", success);

            return success
                ? Ok(new { success = true, logs })
                : StatusCode(500, new { success = false, logs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing script.");
            return StatusCode(500, new { success = false, logs = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "Online",
            timestamp = DateTime.Now,
            machine = System.Net.Dns.GetHostName()
        });
    }
}

public class ExecuteDto
{
    public Guid DeploymentId { get; set; }
    public string AppName { get; set; } = string.Empty;
    public string ScriptName { get; set; } = string.Empty;
}