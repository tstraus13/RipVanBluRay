using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RipVanMonitor.Data
{
    public class Rip
    {
        public string DriveId { get; set; }

        public string DiscLabel { get; set; }

        public string[] LogFile { get; set; }
    }
}
