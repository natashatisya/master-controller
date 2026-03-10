using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Client.Agent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private Guid _hostId;
    private string _apiKey = string.Empty;
    private bool _isRegistered = false;

    public Worker(ILogger<Worker> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config;
        _http = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Client Agent started.");
        await RegisterWithMaster(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_isRegistered)
                await SendHeartbeat(stoppingToken);
            else
                await RegisterWithMaster(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task RegisterWithMaster(CancellationToken ct)
    {
        try
        {
            var masterUrl = _config["MasterApi:BaseUrl"] ?? "http://localhost:5000";
            var hostname = System.Net.Dns.GetHostName();
            var ipAddress = GetLocalIpAddress();
            var os = RuntimeInformation.OSDescription;

            var payload = JsonSerializer.Serialize(new
            {
                Hostname = hostname,
                IpAddress = ipAddress,
                OperatingSystem = os,
                Capabilities = "dotnet,powershell"
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{masterUrl}/api/hosts/register", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<RegisterResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    _hostId = result.Id;
                    _apiKey = result.ApiKey;
                    _isRegistered = true;

                    _http.DefaultRequestHeaders.Remove("X-API-Key");
                    _http.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

                    _logger.LogInformation("Registered with master. HostId: {HostId}", _hostId);
                }
            }
            else
            {
                _logger.LogWarning("Failed to register. Status: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering. Will retry in 30 seconds.");
        }
    }

    private async Task SendHeartbeat(CancellationToken ct)
    {
        try
        {
            var masterUrl = _config["MasterApi:BaseUrl"] ?? "http://localhost:5000";

            var payload = JsonSerializer.Serialize(new
            {
                CpuUsage = GetCpuUsage(),
                MemoryUsage = GetMemoryUsage(),
                DiskUsage = GetDiskUsage()
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(
                $"{masterUrl}/api/hosts/{_hostId}/heartbeat", content, ct);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Heartbeat sent at {Time}", DateTime.Now);
            else
                _logger.LogWarning("Heartbeat failed. Status: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat.");
        }
    }

    private string GetLocalIpAddress()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily ==
                    System.Net.Sockets.AddressFamily.InterNetwork
                    && !System.Net.IPAddress.IsLoopback(a.Address))
                .FirstOrDefault()?.Address.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }

    private double GetCpuUsage()
    {
        try
        {
            var cpuCounter = new System.Diagnostics.PerformanceCounter(
                "Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            Thread.Sleep(100);
            return Math.Round(cpuCounter.NextValue(), 2);
        }
        catch { return 0; }
    }

    private double GetMemoryUsage()
    {
        try
        {
            var ramCounter = new System.Diagnostics.PerformanceCounter(
                "Memory", "Available MBytes");
            var available = ramCounter.NextValue();
            var total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0;
            return Math.Round(100 - (available / total * 100), 2);
        }
        catch { return 0; }
    }

    private double GetDiskUsage()
    {
        try
        {
            var drive = new DriveInfo("C");
            var used = drive.TotalSize - drive.AvailableFreeSpace;
            return Math.Round((double)used / drive.TotalSize * 100, 2);
        }
        catch { return 0; }
    }
}

public class RegisterResponse
{
    public Guid Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}