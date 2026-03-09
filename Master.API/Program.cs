using Master.API.Data;
using Master.API.Middleware;
using Master.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Net Orchestrator - Master API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new()
    {
        Name = "X-API-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "API Key for agent authentication"
    });
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient for calling agents
builder.Services.AddHttpClient();

// Background service to monitor host health
builder.Services.AddHostedService<HostMonitorService>();

// CORS for dashboard
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Auto migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Net Orchestrator Master API v1");
    c.RoutePrefix = "swagger";
    c.HeadContent = "<link rel='icon' href='data:,'>";
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();
app.Run();
