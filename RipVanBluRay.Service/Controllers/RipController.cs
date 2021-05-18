using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RipVanBluRay.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RipVanBluRay.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RipController : ControllerBase
    {
        [HttpGet(Name="DiscDrives")]
        public IEnumerable<string> DiscDrives()
        {
            return Worker.DiscDrives.Select(d => d.Id);
        }

        [HttpGet(Name="Rip")]
        public IEnumerable<Rip> Rips()
        {
            var rips = new List<Rip>();

            foreach(var disc in Worker.DiscDrives)
            {
                var tempDir = new DirectoryInfo(disc.TempDirectoryPath);
                var tempFile = tempDir.GetFiles("*.tmp")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                var logDir = new DirectoryInfo(disc.LogDirectoryPath);
                var logFiles = logDir.GetFiles("*.txt")
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => f.Name);

                rips.Add(new Rip()
                {
                    DiscDriveId = disc.Id,
                    DiscLabel = disc.Label,
                    TempFileSize = tempFile.Length,
                    DiscMedia = disc.DiscMedia,
                    LogFiles = logFiles
                });
            }

            return rips;
        }

        [HttpPost(Name="Rip")]
        public Rip Rip(string id)
        {
            var rip = new Rip();

            var discDrive = Worker.DiscDrives.Where(d => d.Id == id).FirstOrDefault();

            if (discDrive == null || discDrive == default(DiscDrive))
                return rip;

            var tempDir = new DirectoryInfo(discDrive.TempDirectoryPath);
            var tempFile = tempDir.GetFiles("*.tmp")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            var logDir = new DirectoryInfo(discDrive.LogDirectoryPath);
            var logFiles = logDir.GetFiles("*.txt")
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => f.Name); 

            rip.DiscDriveId = discDrive.Id;
            rip.DiscLabel = discDrive.Label;
            rip.TempFileSize = tempFile.Length;
            rip.DiscMedia = discDrive.DiscMedia;
            rip.LogFiles = logFiles;

            return rip;
        }

        [HttpPost(Name="LogFileContent")]
        public List<string> LogFileContent(string id, string logFileName)
        {
            var discDrive = Worker.DiscDrives.Where(d => d.Id == id).FirstOrDefault();

            if (discDrive == null || discDrive == default(DiscDrive))
                return null;

            var filePath = Path.Combine(discDrive.LogDirectoryPath, logFileName);

            if (!System.IO.File.Exists(filePath))
                return null;

            var content = System.IO.File.ReadAllLines(filePath);

            return content.ToList();
        }
    }
}
