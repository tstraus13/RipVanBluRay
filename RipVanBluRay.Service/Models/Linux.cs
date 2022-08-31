using System;
using System.Collections.Generic;

namespace RipVanBluRay.Models
{
    public static class Linux
    {
        public class LsBlkJson
        {
            public IList<LsBlkDevice> blockdevices { get; set; }
        }

        public class LsBlkDevice
        {
            public string name { get; set; }
        }
    }
}

