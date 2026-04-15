using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddSingleton<StockfishService>();

// CORS origins are driven by config so we don't need to redeploy for domain changes.
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithMethods("GET", "POST")
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.MapHub<Backend.Hubs.GameHub>("/gamehub");

app.MapGet("/api/stats", (HttpContext context) =>
{
    var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    if (!StatsRateLimiter.TryAcquire(clientKey))
    {
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    return Results.Ok(Backend.Hubs.GameHub.GetLobbyStats());
});

// AI endpoint
app.MapPost("/api/ai/move", async ([FromBody] AiMoveRequest request, StockfishService stockfish) =>
{
    if (!RequestValidation.IsValidFen(request.Fen))
    {
        return Results.BadRequest("Invalid FEN.");
    }

    var bestMove = await stockfish.GetBestMoveAsync(request.Fen, request.SkillLevel, request.MovetimeMs);
    if (bestMove == null)
    {
        return Results.BadRequest("AI failed to find a move.");
    }
    return Results.Ok(new { bestMove });
});

app.Run();

public record AiMoveRequest(string Fen, int SkillLevel, int MovetimeMs);

static class RequestValidation
{
    public static bool IsValidFen(string fen)
    {
        if (string.IsNullOrWhiteSpace(fen) || fen.Length > 300)
        {
            return false;
        }

        var parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 6;
    }
}

static class StatsRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(1);
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastRequest = new();

    public static bool TryAcquire(string clientKey)
    {
        var now = DateTimeOffset.UtcNow;
        var previous = LastRequest.GetOrAdd(clientKey, now);
        if (now - previous < Window)
        {
            return false;
        }

        LastRequest[clientKey] = now;
        return true;
    }
}
