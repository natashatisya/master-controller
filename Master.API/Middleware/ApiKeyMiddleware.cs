using Master.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Master.API.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        // Skip auth for registration and swagger
        var path = context.Request.Path.Value ?? "";
        if (path.Contains("/swagger") || 
            path.Contains("/api/hosts/register") || 
            path == "/" || 
            path.Contains(".html") || 
            path.Contains(".css") || 
            path.Contains(".js"))
        {
            await _next(context);
            return;
        }

        // Check for API key in header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key is missing.");
            return;
        }

        // Validate API key
        var host = await db.Hosts.FirstOrDefaultAsync(h => h.ApiKey == apiKey.ToString());
        if (host == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API Key.");
            return;
        }

        await _next(context);
    }
}