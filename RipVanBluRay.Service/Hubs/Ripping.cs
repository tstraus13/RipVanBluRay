using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using RipVanBluRay;
using System.IO;
using System.Linq;
using RipVanLibrary;
using System.Collections.Generic;

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

        public async Task GetRips()
        {
            var drives = Worker.DiscDrives;
            List<Rip> rips = new List<Rip>();

            foreach (var drive in drives)
            {
                rips.Add(new Rip
                {
                    DriveId = drive.Id,
                    DiscLabel = drive.Label,
                    LogFile = await GetLogFile(drive.Id)
                });
            }

            //await Clients.Caller.SendAsync("ReceiveRips", rips);
            await Clients.Caller.SendAsync("ReceiveRips", new List<Rip>() { new Rip() { DriveId = "sr0", DiscLabel = "TEST" } });
        }

        private async Task<string[]> GetLogFile(string driveId)
        {
            var drive = Worker.DiscDrives.Where(d => d.Id == driveId).FirstOrDefault();

            if (drive == null)
                return null;

            var directory = new DirectoryInfo(drive.LogDirectoryPath);

            var file = directory.GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .First();

            var lines = await File.ReadAllLinesAsync(file.FullName);

            return lines;
        }

    }
}
