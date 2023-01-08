using Microsoft.AspNetCore.SignalR;
using RipVanBluRay.Models;

namespace RipVanBluRay.Hubs;

public class RipHub : Hub
{

    private readonly ILogger<RipHub> _logger;
    private readonly SharedState _state;

    public RipHub(ILogger<RipHub> logger, SharedState state)
    {
        _logger = logger;
        _state = state;
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

    public async Task SendDiscDriveUpdate(DiscDrive drive)
    {
        await Clients.All.SendAsync("discDriveUpdate", drive);
    }

    public async Task RequestDiscDriveUpdate(string id)
    {
        var drive = _state.DiscDrives.First(d => string.Equals(d.Id, id, StringComparison.CurrentCultureIgnoreCase));

        await Clients.Caller.SendAsync("discDriveUpdate", drive);
    }
}
