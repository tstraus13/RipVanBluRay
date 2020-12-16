using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RipVanBluRay.Library;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

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

        private void DetectDiscDrives()
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
                var json = JsonSerializer.Deserialize<Linux.LsBlkJson>(LocalSystem.ExecuteCommand("lsblk -I 11 -d -J -o NAME"));

                foreach (var dev in json.blockdevices)
                    DiscDrives.Add(new DiscDrive(dev.name));
            }
        }

        private void CheckForDisc(object state)
        {
            foreach (var drive in DiscDrives)
            {
                //_logger.LogInformation($"{DateTime.Now} - No Disc in {drive.Id}");
                if (!drive.InUse)
                {
                    if (drive.DiscPresent)
                    {
                        switch (drive.DiscMedia)
                        {
                            case MediaType.Audio:
                                RipMusic(drive);
                                break;
                            case MediaType.BluRay:
                                RipMovie(drive);
                                break;
                            case MediaType.DVD:
                                drive.InUse = true;
                                var process = RipMovie(drive);
                                process.Exited += RipFinished;
                                break;
                            case MediaType.None:
                                break;
                            default:
                                break;
                        }
                    }

                    else
                        _logger.LogInformation($"{DateTime.Now} - {drive.Id} has no Disc present. Skipping...");
                }
                
                else
                    _logger.LogInformation($"{DateTime.Now} - {drive.Id} is currently in use. Must wait for it to finish. Skipping...");
            }
        }

        private Process RipMovie(DiscDrive drive)
        {
            return LocalSystem.ExecuteBackgroundCommand($"makemkvcon --robot mkv dev:{drive.Path} 0 --minlength=3600 /home/tom/test");
        }

        private Process RipMusic(DiscDrive drive)
        {
            return null;
        }

        private void RipFinished(object sender, EventArgs e)
        {
            _logger.LogInformation($"{DateTime.Now} - RipFinished");
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
