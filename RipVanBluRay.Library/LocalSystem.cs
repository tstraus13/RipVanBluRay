using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RipVanBluRay.Library
{
    public static class LocalSystem
    {
        /// <summary>
        /// Detects if the system is Windows
        /// </summary>
        /// <returns>True if the it is a Windows System</returns>
        public static bool isWindows { get { return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows); } }

        /// <summary>
        /// Detects if the system is Linux
        /// </summary>
        /// <returns>True if the it is a Linux System</returns>
        public static bool isLinux { get { return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux); } }

        /// <summary>
        /// Detects if the system is MacOS
        /// </summary>
        /// <returns>True if the it is a MacOS System</returns>
        public static bool isMacOS { get { return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX); } }

        /// <summary>
        /// Removes the trailing slash from a path
        /// </summary>
        /// <param name="path">The path to check for a trailing slash</param>
        /// <returns>The path without a trailing slash</returns>
        public static string PathRemoveTrailingSlash(string path)
        {
            if (isWindows)
                return path.Last() == '\\' ? path.Remove(path.LastIndexOf(@"\"), 1) : path;
            else
                return path.Last() == '/' ? path.Remove(path.LastIndexOf(@"/"), 1) : path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public static string ExecuteCommand(string command, bool debug = false)
        {
            if (isWindows)
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
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

            else if (isLinux)
            {
                var processInfo = new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
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

            return null;
            /*else if (isMacOS)
            {

            }

            else
            {

            }*/
        }

        public static Process ExecuteBackgroundCommand(string command, bool debug = false)
        {
            if (isWindows)
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                var process = Process.Start(processInfo);

                return process;
            }

            else if (isLinux)
            {
                var processInfo = new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                var process = Process.Start(processInfo);

                return process;
            }

            return null;
            /*else if (isMacOS)
            {

            }

            else
            {

            }*/
        }

        public static void EjectDisc(string driveId)
        {
            if (isWindows)
            {
                Windows.mciSendStringA("open " + driveId + " type CDaudio alias drive" + driveId[0], null, 0, 0);
                Windows.mciSendStringA("set drive" + driveId[0] + " door open", null, 0, 0);
            }
            
            else if (isLinux)
            {
                ExecuteCommand($"eject ");
            }
        }
    }

    public class Linux
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

    public class Windows
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendString")]
        public static extern int mciSendStringA(string lpstrCommand, string lpstrReturnString,
                            int uReturnLength, int hwndCallback);
    }
}
