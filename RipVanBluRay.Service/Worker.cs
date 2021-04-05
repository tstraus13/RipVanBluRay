using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace RipVanBluRay
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
            _logger.LogInformation("Rip Van BluRay Service running.");

            if (!Settings.IsMakeMKVAvailable)
                _logger.LogInformation($"makemkvcon executable was not found! Will not Rip any DVDs, BluRays, or UHD Discs");

            if (!Settings.IsAbcdeAvailable)
                _logger.LogInformation($"abcde executable was not found! Will not Rip any Music CDs");

            _timer = new Timer(CheckDiscDrives, null, TimeSpan.Zero, TimeSpan.FromSeconds(20));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Rip Van BluRay is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DetectDiscDrives()
        {
            var json = JsonSerializer.Deserialize<Linux.LsBlkJson>(LocalSystem.ExecuteCommand("lsblk -I 11 -d -J -o NAME"));

            foreach (var dev in json.blockdevices)
                DiscDrives.Add(new DiscDrive(dev.name));
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
                                if (Settings.IsAbcdeAvailable)
                                    drive.RipProcess = RipMusic(drive);;
                                break;
                            case MediaType.BluRay:
                                if (Settings.IsMakeMKVAvailable)
                                    drive.RipProcess = RipMovie(drive);
                                break;
                            case MediaType.DVD:
                                if (Settings.IsMakeMKVAvailable)
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
                    _logger.LogInformation($"Drive {drive.Id} has finished ripping. Ejecting Disc...");

                    if (drive.RipProcess.ExitCode != 0)
                        _logger.LogWarning($"The Rip for {drive.Id} has exited with an abnormal code!");

                    drive.RipProcess.Close();
                    drive.RipProcess = null;
                    drive.Eject();

                    var files = Directory.GetFiles(drive.TempDirectoryPath, "*.mkv");

                    foreach (var file in files)
                    {
                        var cmd = $@"mv ""{file}"" ""{Path.Combine(Settings.CompletedDirectory, $@"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff")}.mkv")}""";
                        LocalSystem.ExecuteBackgroundCommand(cmd);
                    }
                }

                else if (drive.RipProcess.HasExited && drive.RipProcess.StartInfo.Arguments.Contains("abcde"))
                {
                    _logger.LogInformation($"Drive {drive.Id} has finished ripping. Ejecting Disc...");
                    
                    if (drive.RipProcess.ExitCode != 0)
                        _logger.LogWarning($"The Rip for {drive.Id} has exited with an abnormal code!");

                    drive.RipProcess.Close();
                    drive.RipProcess = null;
                    drive.Eject();
                }
            }
        }

        private Process RipMovie(DiscDrive drive)
        {
            _logger.LogInformation($"Drive {drive.Id} has begun ripping");

            var logFileName = $"log_makemkv_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
            var logFilePath = Path.Combine(drive.LogDirectoryPath, logFileName);

            Directory.CreateDirectory(drive.TempDirectoryPath);
            Directory.CreateDirectory(drive.LogDirectoryPath);

            return LocalSystem.ExecuteBackgroundCommand($@"{Settings.MakeMKVPath} --messages=""{logFilePath}"" --robot mkv dev:{drive.Path} 0 --minlength={Settings.MinimumLength} ""{drive.TempDirectoryPath}""");
        }

        private Process RipMusic(DiscDrive drive)
        {
            // abcde -d /dev/sr1 -o flac -j 4 -N -D 2>logfile
            _logger.LogInformation($"Drive {drive.Id} has begun ripping");

            var logFileName = $"log_abcde_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
            var logFilePath = Path.Combine(drive.LogDirectoryPath, logFileName);

            Directory.CreateDirectory(drive.TempDirectoryPath);
            Directory.CreateDirectory(drive.LogDirectoryPath);

            Dictionary<string,string> env = new Dictionary<string, string>()
            {
                { "OUTPUTDIR", Settings.CompletedDirectory},
                { "WAVOUTPUTDIR", drive.TempDirectoryPath}
            };

            return LocalSystem.ExecuteBackgroundCommand($@"{Settings.AbcdePath} -d {drive.Path} -o {Settings.FileType} -j {Settings.EncoderJobs} -N -D 2>""{logFilePath}""", null, env);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
