using Client.Agent;

var builder = WebApplication.CreateBuilder(args);

// Add web API support so agent can receive commands
builder.Services.AddControllers();

// HttpClient for calling master
builder.Services.AddHttpClient();

// Worker service for registration + heartbeat
builder.Services.AddHostedService<Worker>();

// Listen on port 5100 for incoming commands from master
builder.WebHost.UseUrls("http://0.0.0.0:5100");

var app = builder.Build();

app.MapControllers();

app.Run();