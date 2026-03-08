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
            {
                await SendHeartbeat(stoppingToken);
            }
            else
            {
                await RegisterWithMaster(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task RegisterWithMaster(CancellationToken ct)
    {
        try
        {
            var masterUrl = _config["MasterApi:BaseUrl"] ?? "http://localhost:5000";
            var hostname = System.Net.D