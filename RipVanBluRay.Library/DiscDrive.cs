using System;
using System.Collections.Generic;
using System.Text;

namespace RipVanBluRay.Library
{
    public class DiscDrive
    {
        public string Id { get; set; }
        
        public bool InUse { get; set; }

        public DiscDrive()
        {

        }

        public DiscDrive(string id)
        {
            Id = id;
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
}
