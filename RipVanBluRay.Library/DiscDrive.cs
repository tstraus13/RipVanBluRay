using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace RipVanBluRay.Library
{
    public class DiscDrive
    {
        public string Id { get; set; }

        public Process RipProcess {get; set;}
        
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
                    if (LocalSystem.isLinux)
                        return $"/dev/{Id}";
                    else if (LocalSystem.isWindows)
                        return $@"{Id}\";
                    else
                        return null;
                }

                else
                    return null;
            }
        }

        public string DriveLetter
        {
            get
            {
                if (!string.IsNullOrEmpty(Id) && LocalSystem.isWindows)
                    return Id[0].ToString();
                else
                    return null;
            }
        }

        public bool DiscPresent
        {
            get
            {
                if (LocalSystem.isLinux)
                    return LocalSystem.ExecuteCommand($"udevadm info -q property {Path}").Contains("ID_CDROM_MEDIA=1");
                
                else if (LocalSystem.isWindows)
                    return DriveInfo.GetDrives().FirstOrDefault(d => d.Name == Path).IsReady;
                
                return false;
            }
        }

        public MediaType DiscMedia
        {
            get
            {
                if (LocalSystem.isLinux)
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

                else if (LocalSystem.isWindows)
                {
                    return MediaType.None;
                }

                else
                    return MediaType.None;
            }
        }

        public void Eject()
        {
            if (LocalSystem.isWindows)
            {
                Windows.mciSendStringA($"open {Id} type CDaudio alias drive{DriveLetter}", null, 0, 0);
                Windows.mciSendStringA($"set drive{DriveLetter} door open", null, 0, 0);
            }

            else if (LocalSystem.isLinux)
            {
                LocalSystem.ExecuteCommand($"eject {Path}");
            }
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
