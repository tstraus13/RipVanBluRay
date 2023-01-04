using Microsoft.AspNetCore.SignalR;

namespace RipVanBluRay.Hubs;

public class RipHub : Hub
{

    private readonly ILogger<RipHub> _logger;

    public RipHub(ILogger<RipHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("RipHub - Connection Established - {ConnectionId}", Context.ConnectionId);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("RipHub - Connection Disconnected - {ConnectionId}", Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }


}
