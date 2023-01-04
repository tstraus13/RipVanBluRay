using System.Diagnostics;
using StrausTech.CommonLib;

namespace RipVanBluRay.Models;

public class DiscDrive
{
    public string Id { get; set; }

    public string Label { get; set; }

    public Process RipProcess {get; set;}

    public string TempDirectoryPath
    {
        get
        {
            return System.IO.Path.Combine(Settings.TempDirectory, Id);
        }
    }

    public string LogDirectoryPath
    {
        get
        {
            return System.IO.Path.Combine(Settings.LogsDirectory, Id);
        }
    }
    
    public DiscDrive()
    {

    }

    public DiscDrive(string id)
    {
        Id = id;
    }

    public bool InUse 
    { 
        get
        {
            return RipProcess != null;
        }
    }

    public string Path
    {
        get
        {
            if (!string.IsNullOrEmpty(Id))
            {
                return $"/dev/{Id}";
            }

            else
                return null;
        }
    }

    public bool DiscPresent
    {
        get
        {
            return LocalSystem.Linux.Execute($"udevadm info -q property {Path}").StdOut.Contains("ID_CDROM_MEDIA=1");
        }
    }

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
            if (long.TryParse(LocalSystem.Linux.Execute($"blockdev --getsize64 {Path}").StdOut, out long result))
                return result;
            else
                return 0;
        }
    }

    public int SizeMegaBytes
    {
        get
        {
            return (int) Math.Round(SizeBytes / 1000000.0);
        }
    }

    public int SizeGigaBytes
    {
        get
        {
            return (int) Math.Round(SizeBytes / 1000000000.0);
        }
    }

    public void Eject()
    {
        LocalSystem.Linux.ExecuteBackground($"eject {Path}");
    }
}

public enum MediaType
{
    Audio,
    DVD,
    BluRay,
    None
}