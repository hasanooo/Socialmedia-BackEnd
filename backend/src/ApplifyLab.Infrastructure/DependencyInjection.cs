using ApplifyLab.Application.Interfaces;
using ApplifyLab.Infrastructure.Auth;
using ApplifyLab.Infrastructure.BackgroundJobs;
using ApplifyLab.Infrastructure.Options;
using ApplifyLab.Infrastructure.Persistence;
using ApplifyLab.Infrastructure.Redis;
using ApplifyLab.Infrastructure.Services;
using ApplifyLab.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ApplifyLab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<LocalStorageOptions>(configuration.GetSection(LocalStorageOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Redis");
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(configuration.GetConnectionString("Postgres"))));
        services.AddHangfireServer();

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IJwtBlacklistService, RedisJwtBlacklistService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<RedisLikeCounterCache>();
        services.AddSingleton<ILikeCounterCache>(sp => sp.GetRequiredService<RedisLikeCounterCache>());
        services.AddSingleton<ICommentCounterCache>(sp => sp.GetRequiredService<RedisLikeCounterCache>());
        services.AddSingleton<IFeedCacheService, RedisFeedCacheService>();

        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILikeService, LikeService>();

        services.AddScoped<IThumbnailJob, ThumbnailJob>();
        services.AddScoped<ILikeSyncJob, LikeSyncJob>();
        services.AddScoped<ILikeReconciliationJob, LikeReconciliationJob>();

        return services;
    }
}
