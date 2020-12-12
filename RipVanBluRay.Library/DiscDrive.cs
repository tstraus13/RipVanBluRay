using System;
using System.Collections.Generic;
using System.Text;

namespace RipVanBluRay.Library
{
    public class DiscDrive
    {
        public string Id { get; set; }
        public bool InUse { get; set; }

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
    }
}
