using System.Text;
using ApplifyLab.Api.Auth;
using ApplifyLab.Api.Filters;
using ApplifyLab.Api.Hubs;
using ApplifyLab.Api.Middleware;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Application.Validators;
using ApplifyLab.Infrastructure;
using ApplifyLab.Infrastructure.Options;
using ApplifyLab.Infrastructure.Persistence;
using AspNetCoreRateLimit;
using AspNetCoreRateLimit.Redis;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
var frontendOrigin = builder.Configuration["Cors:FrontendOrigin"]!;
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")!;
var localStorageOptions = builder.Configuration.GetSection(LocalStorageOptions.SectionName).Get<LocalStorageOptions>()!;
var uploadsRootPath = Path.IsPathRooted(localStorageOptions.RootPath)
    ? localStorageOptions.RootPath
    : Path.Combine(Directory.GetCurrentDirectory(), localStorageOptions.RootPath);
Directory.CreateDirectory(uploadsRootPath);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.EventsType = typeof(CookieJwtBearerEvents);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddScoped<CookieJwtBearerEvents>();
builder.Services.AddAuthorization();

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins(frontendOrigin)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString, options =>
{
    options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("applifylab-signalr");
});

// Distributed rate limiting on auth + post-creation endpoints (see appsettings IpRateLimiting).
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
// AspNetCoreRateLimit.Redis's stores are backed by IDistributedCache, which isn't registered by
// default — it must point at the same Redis instance or DI validation fails at startup.
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
builder.Services.AddRedisRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRootPath),
    RequestPath = "/uploads",
});
app.UseCors("Frontend");
app.UseIpRateLimiting();
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<FeedHub>("/hubs/feed");

app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = new[]
    {
        new HangfireDashboardAuthFilter(
            builder.Configuration["Hangfire:DashboardKey"] ?? string.Empty,
            app.Environment.IsDevelopment()),
    },
});

RecurringJob.AddOrUpdate<ILikeReconciliationJob>(
    "like-count-reconciliation",
    job => job.ReconcileAsync(CancellationToken.None),
    "*/15 * * * *");

app.Run();

public partial class Program { }
