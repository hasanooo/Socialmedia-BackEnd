using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace ApplifyLab.Api.Hubs;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<FeedHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<FeedHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyLikeChangedAsync(LikeableType type, Guid id, long likeCount, CancellationToken ct)
        => _hub.Clients.All.SendAsync("LikeChanged", new { type = type.ToString(), id, likeCount }, ct);

    public Task NotifyCommentAddedAsync(Guid postId, CommentDto comment, CancellationToken ct)
        => _hub.Clients.All.SendAsync("CommentAdded", new { postId, comment }, ct);
}
