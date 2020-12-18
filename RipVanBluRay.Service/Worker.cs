using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            Settings.Init();
            DetectDiscDrives();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(CheckDiscDrives, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

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

        private void CheckDiscDrives(object state)
        {
            foreach (var drive in DiscDrives)
            {
                if (!drive.InUse)
                {
                    if (drive.DiscPresent)
                    {
                        switch (drive.DiscMedia)
                        {
                            case MediaType.Audio:
                                drive.RipProcess = RipMusic(drive);;
                                break;
                            case MediaType.BluRay:
                                drive.RipProcess = RipMovie(drive);
                                break;
                            case MediaType.DVD:
                                drive.RipProcess = RipMovie(drive);
                                break;
                            case MediaType.None:
                                break;
                            default:
                                break;
                        }
                    }
                }

                else if (drive.RipProcess.HasExited && drive.RipProcess.StartInfo.Arguments.Contains("makemkvcon"))
                {
                    _logger.LogInformation($"{DateTime.Now} - Drive {drive.Id} has finished ripping. Exit code was {drive.RipProcess.ExitCode}. Ejecting Disc...");

                    drive.Eject();
                    drive.RipProcess = null;

                    var files = Directory.GetFiles(drive.TempDirectoryPath, "*.mkv");

                    foreach (var file in files)
                    {
                        File.Move(file, Path.Combine(Settings.CompletedDirectory, $@"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.mkv"));
                    }
                }

                else if (drive.RipProcess.HasExited && drive.RipProcess.StartInfo.Arguments.Contains("abcde"))
                {
                    _logger.LogInformation($"{DateTime.Now} - Drive {drive.Id} has finished ripping. Ejecting Disc...");

                    drive.Eject();
                    drive.RipProcess = null;
                }
            }
        }

        private Process RipMovie(DiscDrive drive)
        {
            _logger.LogInformation($"{DateTime.Now} - Drive {drive.Id} is has begun ripping");

            var logFileName = $"log_makemkv_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";

            Directory.CreateDirectory(drive.TempDirectoryPath);
            Directory.CreateDirectory(drive.LogDirectoryPath);

            return LocalSystem.ExecuteBackgroundCommand($@"makemkvcon --messages=""{Path.Combine(drive.LogDirectoryPath, logFileName)}"" --robot mkv dev:{drive.Path} 0 --minlength={Settings.MinimumLength} ""{drive.TempDirectoryPath}""");
        }

        private Process RipMusic(DiscDrive drive)
        {
            // abcde -d /dev/sr1 -o flac -j 4 -N -D 2>logfile
            _logger.LogInformation($"{DateTime.Now} - Drive {drive.Id} is has begun ripping");

            var logFileName = $"log_abcde_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";

            Directory.CreateDirectory(drive.TempDirectoryPath);
            Directory.CreateDirectory(drive.LogDirectoryPath);

            Dictionary<string,string> env = new Dictionary<string, string>()
            {
                { "OUTPUTDIR", Settings.CompletedDirectory},
                { "WAVOUTPUTDIR", drive.TempDirectoryPath}
            };

            return LocalSystem.ExecuteBackgroundCommand($@"abcde -d {drive.Path} -o {Settings.FileType} -j {Settings.EncoderJobs} -N -D 2>""{Path.Combine(drive.LogDirectoryPath, logFileName)}""", null, env);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
