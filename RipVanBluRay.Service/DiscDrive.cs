using System.Diagnostics;

namespace RipVanBluRay.Service
{
    public class DiscDrive
    {
        public string Id { get; set; }

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
                return RipProcess == null ? false : true;
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
                return LocalSystem.ExecuteCommand($"udevadm info -q property {Path}").Contains("ID_CDROM_MEDIA=1");
            }
        }

        public MediaType DiscMedia
        {
            get
            {
                switch (LocalSystem.ExecuteCommand($"udevadm info -q property {Path}"))
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

        public void Eject()
        {
            LocalSystem.ExecuteCommand($"eject {Path}");
        }
    }

    public enum MediaType
    {
        Audio,
        DVD,
        BluRay,
        None
    }
}
