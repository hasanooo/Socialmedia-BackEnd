using ApplifyLab.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ApplifyLab.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public HealthController(AppDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var dbOk = await _db.Database.CanConnectAsync(ct);
        var redisOk = _redis.IsConnected;

        return Ok(new { database = dbOk, redis = redisOk, status = dbOk && redisOk ? "healthy" : "degraded" });
    }
}
