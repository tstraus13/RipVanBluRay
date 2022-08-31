using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using RipVanBluRay.Models;
using StrausTech.CommonLib;

namespace RipVanBluRay
{
    public class Worker : IHostedService, IDisposable
    {
        private readonly ILogger<Worker> Logger;
        private bool CheckDiscRunning = false;

        private Timer DiscTimer;
        private Timer MoveTimer;

        private List<DiscDrive> DiscDrives = new List<DiscDrive>();

        private List<Process> MoveProcesses = new List<Process>();
        private ConcurrentQueue<string> FilesToMove = new ConcurrentQueue<string>();

        public Worker(ILogger<Worker> ilogger)
        {
            Logger = ilogger;

            Settings.Init();
            DetectDiscDrives();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Rip Van BluRay Service running.");

            if (!Settings.IsMakeMKVAvailable)
                Logger.LogInformation($"makemkvcon executable was not found! Will not Rip any DVDs, BluRays, or UHD Discs");

            if (!Settings.IsAbcdeAvailable)
                Logger.LogInformation($"abcde executable was not found! Will not Rip any Music CDs");

            DiscTimer = new Timer(CheckDiscDrives, null, TimeSpan.Zero, TimeSpan.FromSeconds(20));
            MoveTimer = new Timer(MoveFile, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Rip Van BluRay is stopping.");

            DiscTimer?.Change(Timeout.Infinite, 0);
            MoveTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DetectDiscDrives()
        {
            var process = LocalSystem.Linux.Execute("lsblk -I 11 -d -J -o NAME");

            var json = JsonSerializer.Deserialize<Linux.LsBlkJson>(process.StdOut);

            foreach (var dev in json.blockdevices)
                DiscDrives.Add(new DiscDrive(dev.name));
        }

        private void CheckDiscDrives(object state)
        {
            if (CheckDiscRunning)
                return;

            CheckDiscRunning = true;

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
                    Logger.LogInformation($"Drive {drive.Id} has finished ripping. Ejecting Disc...");

                    if (drive.RipProcess.ExitCode != 0)
                        Logger.LogWarning($"The Rip for {drive.Id} has exited with an abnormal code!");

                    drive.RipProcess.Close();
                    drive.RipProcess = null;
                    drive.Eject();

                    var files = Directory.GetFiles(drive.TempDirectoryPath, "*.mkv");

                    foreach (var file in files)
                    {
                        // Rename the files in case another rip finishes it
                        // doesn't attempt to move the files again that are
                        // in progress of being moved
                        var mvName = Path.Combine(drive.TempDirectoryPath, $"{drive.Label}.{Guid.NewGuid().ToString()}.tmp");
                        var rename = $@"mv ""{file}"" ""{mvName}""";
                        LocalSystem.Linux.Execute(rename);

                        var cmd = $@"mv ""{mvName}"" ""{Path.Combine(Settings.CompletedDirectory, $@"{drive.Label}.{Guid.NewGuid().ToString()}.mkv")}""";
                        //LocalSystem.ExecuteBackgroundCommand(cmd);
                        FilesToMove.Enqueue(cmd);
                    }
                }

                else if (drive.RipProcess.HasExited && drive.RipProcess.StartInfo.Arguments.Contains("abcde"))
                {
                    Logger.LogInformation($"Drive {drive.Id} has finished ripping. Ejecting Disc...");
                    
                    if (drive.RipProcess.ExitCode != 0)
                        Logger.LogWarning($"The Rip for {drive.Id} has exited with an abnormal code!");

                    drive.RipProcess.Close();
                    drive.RipProcess = null;
                    drive.Eject();
                }
            }

            CheckDiscRunning = false;
        }

        private Process RipMovie(DiscDrive drive)
        {
            Logger.LogInformation($"Drive {drive.Id} has started ripping");

            var logFileName = $"log_makemkv_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
            var logFilePath = Path.Combine(drive.LogDirectoryPath, logFileName);

            Directory.CreateDirectory(drive.TempDirectoryPath);
            Directory.CreateDirectory(drive.LogDirectoryPath);

            drive.Label = LocalSystem.Linux.Execute($"blkid -o value -s LABEL {drive.Path}").StdOut.Trim();

            return LocalSystem.Linux.ExecuteBackground($@"{Settings.MakeMKVPath} --messages=""{logFilePath}"" --robot mkv dev:{drive.Path} 0 --minlength={Settings.MinimumLength} ""{drive.TempDirectoryPath}""");
        }

        private Process RipMusic(DiscDrive drive)
        {
            // abcde -d /dev/sr1 -o flac -j 4 -N -D 2>logfile
            Logger.LogInformation($"Drive {drive.Id} has started ripping");

            var logFileName = $"log_abcde_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
            var logFilePath = Path.Combine(drive.LogDirectoryPath, logFileName);

            Directory.CreateDirectory(drive.TempDirectoryPath);
            Directory.CreateDirectory(drive.LogDirectoryPath);

            Dictionary<string,string> env = new Dictionary<string, string>()
            {
                { "OUTPUTDIR", Settings.CompletedDirectory},
                { "WAVOUTPUTDIR", drive.TempDirectoryPath}
            };

            return LocalSystem.Linux.ExecuteBackground($@"{Settings.AbcdePath} -d {drive.Path} -o {Settings.FileType} -j {Settings.EncoderJobs} -N -D 2>""{logFilePath}""", null, env);
        }

        private void MoveFile(object state)
        {
            if (!FilesToMove.IsEmpty)
            {
                MoveProcesses.RemoveAll(m => m.HasExited);

                var nMoves = Settings.ConcurrentMoves - MoveProcesses.Count;

                if (nMoves > 0)
                {
                    for (int i = 0; i < Math.Min(nMoves, FilesToMove.Count); i++)
                    {
                        if (FilesToMove.TryDequeue(out string cmd))
                        {
                            MoveProcesses.Add(LocalSystem.Linux.ExecuteBackground(cmd));
                        }
                    }

                    Logger.LogInformation($"Moving File(s)... {FilesToMove.Count} File(s) Remaining");
                }
            }
        }

        public void Dispose()
        {
            DiscTimer?.Dispose();
            MoveTimer?.Dispose();
        }
    }
}
