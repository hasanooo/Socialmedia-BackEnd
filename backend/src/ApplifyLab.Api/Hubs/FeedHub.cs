using Microsoft.AspNetCore.SignalR;

namespace ApplifyLab.Api.Hubs;

/// <summary>
/// Clients connect here to receive live like/comment count updates. Backed by the Redis
/// backplane (see Program.cs AddStackExchangeRedis) so events fan out correctly even when
/// multiple backend instances sit behind a load balancer.
/// </summary>
public class FeedHub : Hub
{
}
