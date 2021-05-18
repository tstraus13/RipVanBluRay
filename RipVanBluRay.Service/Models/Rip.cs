using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RipVanBluRay.Models
{
    public class Rip
    {
        public string DiscDriveId { get; set; }
        public string DiscLabel { get; set; }
        public long TempFileSize { get; set; }
        public MediaType DiscMedia { get; set; }
        public IEnumerable<string> LogFiles { get; set; }
    }
}
