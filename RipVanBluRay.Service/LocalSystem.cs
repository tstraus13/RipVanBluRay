using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RipVanBluRay.Service
{
    public static class LocalSystem
    {
        public static string UserDirectory
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public static string ExecuteCommand(string command, string workingDir = null, Dictionary<string, string> env = null, bool debug = false)
        {
            var processInfo = new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = !string.IsNullOrEmpty(workingDir) ? workingDir : ""
            };

            if (env != null)    
                foreach (var var in env)
                    processInfo.Environment.Add(var.Key, var.Value);

            var process = Process.Start(processInfo);

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            process.Close();

            if (string.IsNullOrEmpty(error))
                return output;
            else
                return error;
        }

        public static Process ExecuteBackgroundCommand(string command, string workingDir = null, Dictionary<string, string> env = null, bool debug = false)
        {

            var processInfo = new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = !string.IsNullOrEmpty(workingDir) ? workingDir : ""
            };

            if (env != null)    
                foreach (var var in env)
                    processInfo.Environment.Add(var.Key, var.Value);

            var process = Process.Start(processInfo);

            return process;
        }
    }

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
