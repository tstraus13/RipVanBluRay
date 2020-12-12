using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RipVanBluRay.Library;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace RipVanBluRay.Service
{
    public class Worker : IHostedService, IDisposable
    {
        private readonly ILogger<Worker> _logger;
        private Timer _timer;

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
                // Detect Drives on Windows
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
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }

    public class LsBlkJson
    {
        public IList<LsBlkDevice> blockdevices { get; set; }
    }

    public class LsBlkDevice
    {
        public string name { get; set; }
    }
}
