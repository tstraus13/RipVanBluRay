using System.Text.Json;
using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using RipVanBluRay.Models;
using StrausTech.CommonLib;

namespace RipVanBluRay;

public class Worker : IHostedService, IDisposable
{
    private HubConnection _ripHubClient;
    
    private readonly ILogger<Worker> _logger;
    private bool _checkDiscRunning = false;

    private Timer _discTimer;
    private Timer _moveTimer;

    private SharedState _sharedState;

    public Worker(ILogger<Worker> logger, SharedState sharedState)
    {
        _logger = logger;
        _sharedState = sharedState;

        ConfigureHubConnection();
        
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

        _discTimer = new Timer(CheckDiscDrives, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
        _moveTimer = new Timer(MoveFile, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rip Van BluRay is stopping.");

        _discTimer?.Change(Timeout.Infinite, 0);
        _moveTimer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private void DetectDiscDrives()
    {
        var process = LocalSystem.Linux.Execute("lsblk -I 11 -d -J -o NAME");

        var json = JsonSerializer.Deserialize<Linux.LsBlkJson>(process.StdOut);

        foreach (var dev in json.blockdevices)
            _sharedState.DiscDrives.Add(new DiscDrive(dev.name));
    }

    private void CheckDiscDrives(object state)
    {
        if (_checkDiscRunning)
            return;

        _checkDiscRunning = true;

        foreach (var drive in _sharedState.DiscDrives)
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
                    // Rename the files in case another rip finishes it
                    // doesn't attempt to move the files again that are
                    // in progress of being moved
                    var mvName = Path.Combine(drive.TempDirectoryPath, $"{drive.Label}.{Guid.NewGuid().ToString()}.tmp");
                    var rename = $@"mv ""{file}"" ""{mvName}""";
                    LocalSystem.Linux.Execute(rename);

                    var cmd = $@"mv ""{mvName}"" ""{Path.Combine(Settings.CompletedDirectory, $@"{drive.Label}.{Guid.NewGuid().ToString()}.mkv")}""";
                    //LocalSystem.ExecuteBackgroundCommand(cmd);
                    _sharedState.FilesToMove.Enqueue(cmd);
                }
                
                drive.Label = "";
            }

            else if (drive.RipProcess.HasExited && drive.RipProcess.StartInfo.Arguments.Contains("abcde"))
            {
                _logger.LogInformation($"Drive {drive.Id} has finished ripping. Ejecting Disc...");
                
                if (drive.RipProcess.ExitCode != 0)
                    _logger.LogWarning($"The Rip for {drive.Id} has exited with an abnormal code!");

                drive.RipProcess.Close();
                drive.RipProcess = null;
                drive.Label = "";
                drive.Eject();
            }
        }

        _checkDiscRunning = false;
        
        if (_ripHubClient.State != HubConnectionState.Connected)
            HubConnect();

        foreach (var drive in _sharedState.DiscDrives)
            _ripHubClient.InvokeAsync("SendDiscDriveUpdate", drive)
                .GetAwaiter().GetResult();
    }

    private Process? RipMovie(DiscDrive drive)
    {
        _logger.LogInformation($"Drive {drive.Id} has started ripping");

        var logFileName = $"log_makemkv_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
        var logFilePath = Path.Combine(drive.LogDirectoryPath, logFileName);

        Directory.CreateDirectory(drive.TempDirectoryPath);
        Directory.CreateDirectory(drive.LogDirectoryPath);

        drive.Label = LocalSystem.Linux.Execute($"blkid -o value -s LABEL {drive.Path}").StdOut.Trim();

        var process = LocalSystem.Linux.ExecuteBackground($@"{Settings.MakeMKVPath} --messages=""{logFilePath}"" --robot mkv dev:{drive.Path} 0 --minlength={Settings.MinimumLength} ""{drive.TempDirectoryPath}""");

        return process;
    }

    private Process RipMusic(DiscDrive drive)
    {
        // abcde -d /dev/sr1 -o flac -j 4 -N -D 2>logfile
        _logger.LogInformation($"Drive {drive.Id} has started ripping");

        var logFileName = $"log_abcde_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
        var logFilePath = Path.Combine(drive.LogDirectoryPath, logFileName);

        Directory.CreateDirectory(drive.TempDirectoryPath);
        Directory.CreateDirectory(drive.LogDirectoryPath);

        Dictionary<string,string> env = new Dictionary<string, string>()
        {
            { "OUTPUTDIR", Settings.CompletedDirectory},
            { "WAVOUTPUTDIR", drive.TempDirectoryPath}
        };

        drive.Label = LocalSystem.Linux.Execute($"blkid -o value -s LABEL {drive.Path}").StdOut.Trim();
        
        return LocalSystem.Linux.ExecuteBackground($@"{Settings.AbcdePath} -d {drive.Path} -o {Settings.FileType} -j {Settings.EncoderJobs} -N -D 2>""{logFilePath}""", null, env);
    }

    private void MoveFile(object state)
    {
        if (!_sharedState.FilesToMove.IsEmpty)
        {
            _sharedState.MoveProcesses.RemoveAll(m => m.HasExited);

            var nMoves = Settings.ConcurrentMoves - _sharedState.MoveProcesses.Count;

            if (nMoves > 0)
            {
                for (int i = 0; i < Math.Min(nMoves, _sharedState.FilesToMove.Count); i++)
                {
                    if (_sharedState.FilesToMove.TryDequeue(out string cmd))
                    {
                        _sharedState.MoveProcesses.Add(LocalSystem.Linux.ExecuteBackground(cmd));
                    }
                }

                _logger.LogInformation($"Moving File(s)... {_sharedState.FilesToMove.Count} File(s) Remaining");
            }
        }
    }

    private void ConfigureHubConnection()
    {
        _ripHubClient = new HubConnectionBuilder()
            .WithUrl("https://localhost:5001/hubs/rip", options =>
            {
                options.HttpMessageHandlerFactory = (msg) =>
                {
                    if (msg is HttpClientHandler clientHandler)
                    {
                        // bypass SSL certificate
                        clientHandler.ServerCertificateCustomValidationCallback +=
                            (sender, certificate, chain, sslPolicyErrors) => { return true; };
                    }

                    return msg;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _ripHubClient.Closed += async (error) =>
        {
            _logger.LogError(error, "Hub Connection Error!");
            await Task.Delay(new Random().Next(0,5) * 1000);
            await _ripHubClient.StartAsync();
        };
    }

    private void HubConnect()
    {
        try
        {
            _ripHubClient.StartAsync()
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Connecting to Hub!");
        }
    }
    
    public void Dispose()
    {
        _ripHubClient.StopAsync()
            .GetAwaiter().GetResult();
        _ripHubClient.DisposeAsync()
            .GetAwaiter().GetResult();
        
        _discTimer?.Dispose();
        _moveTimer?.Dispose();
    }
}
