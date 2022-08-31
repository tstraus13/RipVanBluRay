using Microsoft.Extensions.Configuration;
using StrausTech.CommonLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RipVanBluRay
{
    public static class Settings
    {
        public static string TempDirectory { get; private set; }
        public static string CompletedDirectory { get; private set; }
        public static string LogsDirectory {get; private set; }
        public static bool IsMakeMKVAvailable 
        { 
            get { return !string.IsNullOrEmpty(MakeMKVPath); }
        }

        public static bool IsAbcdeAvailable
        {
            get { return !string.IsNullOrEmpty(AbcdePath); }
        }

        public static string MakeMKVPath {get; private set; }
        public static string MinimumLength { get; private set; }
        public static int ConcurrentMoves { get; private set; }

        public static string AbcdePath { get; private set; }
        public static string FileType { get; private set; }
        public static string EncoderJobs { get; private set; }

        private static IConfigurationRoot ConfigFile { get; set; }

        public static string UserDirectory
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
        }

        private static readonly string DefaultDirectory = Path.Combine(UserDirectory, ".RipVanBluRay");

        /// <summary>
        /// Initializes the settings and loads any settings from
        /// the app settings files
        /// </summary>
        public static void Init()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                //.AddJsonFile($"appsettings.{env}.json", true, true)
                .AddJsonFile(Path.Combine(DefaultDirectory, "settings.json"), true, true);

            ConfigFile = builder.Build();

            Directory.CreateDirectory(DefaultDirectory);

            MakeMKVPath = !string.IsNullOrEmpty(ConfigFile.GetSection("MakeMKV")["Path"])
                ? ConfigFile.GetSection("MakeMKV")["Path"]
                : LocalSystem.Linux.Execute("which makemkvcon").StdOut.Trim();
            
            AbcdePath = !string.IsNullOrEmpty(ConfigFile.GetSection("ABCDE")["Path"])
                ? ConfigFile.GetSection("ABCDE")["Path"]
                : LocalSystem.Linux.Execute("which abcde").StdOut.Trim();

            if (!string.IsNullOrEmpty(ConfigFile["TempDirectory"]))
            {
                TempDirectory = ConfigFile["TempDirectory"];

                Directory.CreateDirectory(TempDirectory);
            }

            else
            {
                TempDirectory = Path.Combine(DefaultDirectory, "temp");

                Directory.CreateDirectory(TempDirectory);
            }

            if (!string.IsNullOrEmpty(ConfigFile["CompletedDirectory"]))
            {
                CompletedDirectory = ConfigFile["CompletedDirectory"];

                Directory.CreateDirectory(CompletedDirectory);
            }

            else
            {
                CompletedDirectory = Path.Combine(DefaultDirectory, "completed");

                Directory.CreateDirectory(CompletedDirectory);
            }

            if (!string.IsNullOrEmpty(ConfigFile["LogsDirectory"]))
            {
                LogsDirectory = ConfigFile["LogsDirectory"];

                Directory.CreateDirectory(LogsDirectory);
            }

            else
            {
                LogsDirectory = Path.Combine(DefaultDirectory, "logs");

                Directory.CreateDirectory(LogsDirectory);
            }

            if (!string.IsNullOrEmpty(ConfigFile.GetSection("MakeMKV")["Key"]))
            {
                var key = ConfigFile.GetSection("MakeMKV")["Key"];
                var makeMkvSettingsFilePath = Path.Combine(UserDirectory, ".MakeMKV", "settings.conf");

                if (File.Exists(makeMkvSettingsFilePath))
                {
                    var makeMkvSettingsFileContent = new List<string>(File.ReadAllLines(makeMkvSettingsFilePath));
                    
                    if (makeMkvSettingsFileContent.Exists(l => l.Contains("app_Key")))
                    {
                        if (!makeMkvSettingsFileContent.Exists(l => l.Contains(key)))
                        {
                            makeMkvSettingsFileContent.RemoveAll(l => l.Contains("app_Key"));
                            makeMkvSettingsFileContent = makeMkvSettingsFileContent.Append($@"app_Key = ""{key}""").ToList();
                            File.WriteAllLines(makeMkvSettingsFilePath, makeMkvSettingsFileContent);
                        }
                    }

                    else
                    {
                        makeMkvSettingsFileContent = makeMkvSettingsFileContent.Append($@"app_Key = ""{key}""").ToList();
                        File.WriteAllLines(makeMkvSettingsFilePath, makeMkvSettingsFileContent);
                    }
                }
            }

            else
            {
                // Could try to retrieve beta key here but I think would prefer for the user to manually do this
                // so the creator of MakeMKV would get more purchases
            }

            if (!string.IsNullOrEmpty(ConfigFile.GetSection("MakeMKV")["MinimumLength"]))
                MinimumLength = ConfigFile.GetSection("MakeMKV")["MinimumLength"];
            else
                MinimumLength = "3600";

            if (!string.IsNullOrEmpty(ConfigFile.GetSection("MakeMKV")["ConcurrentMoves"]))
            {
                if (int.TryParse(ConfigFile.GetSection("MakeMKV")["ConcurrentMoves"], out int cMoves))
                    ConcurrentMoves = cMoves;
                else
                    ConcurrentMoves = 1;
            }
            else
                ConcurrentMoves = 1;

            if (!string.IsNullOrEmpty(ConfigFile.GetSection("ABCDE")["FileType"]))
                FileType = ConfigFile.GetSection("ABCDE")["FileType"];
            else
                FileType = "flac";

            if (!string.IsNullOrEmpty(ConfigFile.GetSection("ABCDE")["EncoderJobs"]))
                EncoderJobs = ConfigFile.GetSection("ABCDE")["EncoderJobs"];
            else
                EncoderJobs = "2";
        }
    }
}
