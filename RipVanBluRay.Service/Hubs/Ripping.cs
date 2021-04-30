using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using RipVanBluRay;
using System.IO;
using System.Linq;

namespace RipVanBluRay.Hubs
{
    public class Ripping : Hub
    {

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task GetDiscDrives()
        {
            var drives = Worker.DiscDrives;

            await Clients.Caller.SendAsync("ReceiveDiscDrives", drives.Select(d => d.Id).ToList());
        }

        public async Task GetLogFile(string driveId)
        {
            var drive = Worker.DiscDrives.Where(d => d.Id == driveId).FirstOrDefault();

            if (drive == null)
                return;

            var directory = new DirectoryInfo(drive.LogDirectoryPath);

            var file = directory.GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .First();

            var lines = await File.ReadAllLinesAsync(file.FullName);

            await Clients.Caller.SendAsync("ReceiveLogFile", drive, lines);
        }

    }
}
