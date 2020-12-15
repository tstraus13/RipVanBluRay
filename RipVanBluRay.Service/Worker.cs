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

            LocalSystem.EjectDisc("E:");
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
                // Detect Drives on Windows - wmic logicaldisk get deviceid, description
                //var output = LocalSystem.ExecuteCommand("wmic logicaldisk get deviceid, description");
                var drives = DriveInfo.GetDrives();

                /*foreach (var line in output.Split(Environment.NewLine))
                {
                    if (line.Contains("CD-ROM"))
                    {
                        var split = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                        DiscDrives.Add(new DiscDrive(split[2]));
                    }
                }*/

                foreach (var drive in drives)
                {
                    if (drive.DriveType == DriveType.CDRom)
                    {
                        DiscDrives.Add(new DiscDrive(drive.Name));
                    }
                }
            }

            else if (LocalSystem.isLinux)
            {

                // lsblk -I 11 -d -J -o NAME - /bin/bash -c "lsblk -I 11 -d -J -o NAME"
                var json = JsonSerializer.Deserialize<LsBlkJson>(LocalSystem.ExecuteCommand("lsblk -I 11 -d -J -o NAME"));

                foreach (var dev in json.blockdevices)
                    Console.WriteLine(dev.name);
            }
        }

        public void CheckForDisc(object state)
        {
            foreach (var drive in DiscDrives)
            {
                _logger.LogInformation($"{DateTime.Now} - No Disc in {drive.Id}");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
