using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RipVanBluRay.Library;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using static RipVanBluRay.Library.Linux;
using System.IO;
using System.Linq;

namespace RipVanBluRay.Service
{
    public class Worker : IHostedService, IDisposable
    {
        private readonly ILogger<Worker> _logger;
        private Timer _timer;
        private List<DiscDrive> DiscDrives = new List<DiscDrive>();

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            DetectDiscDrives();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(CheckForDisc, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void DetectDiscDrives()
        {
            if (LocalSystem.isWindows)
            {
                var drives = DriveInfo.GetDrives();

                foreach (var drive in drives)
                {
                    if (drive.DriveType == DriveType.CDRom)
                    {
                        DiscDrives.Add(new DiscDrive(new string(drive.Name.Take(2).ToArray())));
                    }
                }
            }

            else if (LocalSystem.isLinux)
            {
                var json = JsonSerializer.Deserialize<LsBlkJson>(LocalSystem.ExecuteCommand("lsblk -I 11 -d -J -o NAME"));

                foreach (var dev in json.blockdevices)
                    DiscDrives.Add(new DiscDrive(dev.name));
            }
        }

        public void CheckForDisc(object state)
        {
            foreach (var drive in DiscDrives)
            {
                //_logger.LogInformation($"{DateTime.Now} - No Disc in {drive.Id}");
                if (LocalSystem.isWindows)
                {

                }

                else if (LocalSystem.isLinux)
                {

                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
