using System.Diagnostics;
using System.Text.Json.Serialization;
using StrausTech.CommonLib;

namespace RipVanBluRay.Models;

public class DiscDrive
{
    public string Id { get; set; }

    public string Label { get; set; }

    [JsonIgnore]
    public bool InUse => RipProcess != null;
    
    [JsonIgnore]
    public Process RipProcess {get; set;}

    public string TempDirectoryPath => System.IO.Path.Combine(Settings.TempDirectory, Id);

    public string LogDirectoryPath => System.IO.Path.Combine(Settings.LogsDirectory, Id);

    public string CurrentLogFileContents
    {
        get
        {
            if (_ejecting)
                return string.Empty;

            if (!DiscPresent)
                return string.Empty;

            var dir = new DirectoryInfo(LogDirectoryPath);
            var file = dir.GetFiles().MaxBy(f => f.CreationTime);

            if (file == null)
                return string.Empty;

            return System.IO.File.ReadAllText(file.FullName);
        }
    }

    public long CurrentRipSizeBytes
    {
        get
        {
            if (_ejecting)
                return 0;

            if (!DiscPresent)
                return 0;

            var dir = new DirectoryInfo(TempDirectoryPath);
            var file = dir.GetFiles().MaxBy(f => f.CreationTime);

            if (file == null)
                return 0;

            return file.Length;
        }
    }
    
    public int CurrentRipSizeMegaBytes => (int) Math.Round(CurrentRipSizeBytes / 1000000.0);

    public int CurrentRipSizeGigaBytes => (int) Math.Round(CurrentRipSizeBytes / 1000000000.0);
    
        public string Path
    {
        get
        {
            if (!string.IsNullOrEmpty(Id))
            {
                return $"/dev/{Id}";
            }
            
            return null;
        }
    }

    public bool DiscPresent => LocalSystem.Linux.Execute($"udevadm info -q property {Path}")
        .StdOut.Contains("ID_CDROM_MEDIA=1");

    public MediaType DiscMedia
    {
        get
        {
            switch (LocalSystem.Linux.Execute($"udevadm info -q property {Path}").StdOut)
            {
                case string a when a.Contains("ID_CDROM_MEDIA_CD=1") && a.Contains("ID_CDROM_MEDIA_TRACK_COUNT_AUDIO"):
                    return MediaType.Audio;
                case string a when a.Contains("ID_CDROM_MEDIA_BD=1"):
                    return MediaType.BluRay;
                case string a when a.Contains("ID_CDROM_MEDIA_DVD=1"):
                    return MediaType.DVD;
                default:
                    return MediaType.None;
            }
        }
    }

    public long SizeBytes
    {
        get
        {
            if (_ejecting)
                return 0;

            if (!DiscPresent)
                return 0;
            
            if (long.TryParse(LocalSystem.Linux.Execute($"blockdev --getsize64 {Path}").StdOut, out long result))
                return result;
            
            return 0;
        }
    }

    public int SizeMegaBytes => (int) Math.Round(SizeBytes / 1000000.0);

    public int SizeGigaBytes => (int) Math.Round(SizeBytes / 1000000000.0);

    private bool _ejecting;

    public DiscDrive()
    {
        _ejecting = false;
    }

    public DiscDrive(string id)
    {
        Id = id;
        _ejecting = false;
        
        Directory.CreateDirectory(LogDirectoryPath);
        Directory.CreateDirectory(TempDirectoryPath);
    }

    public void Eject()
    {
        _ejecting = true;
        LocalSystem.Linux.Execute($"eject {Path}");
        Task.Delay(3000).GetAwaiter().GetResult();
        _ejecting = false;
    }
}

public enum MediaType
{
    Audio,
    DVD,
    BluRay,
    None
}