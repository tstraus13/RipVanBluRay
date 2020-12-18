using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RipVanBluRay.Service
{
    public static class Settings
    {
        public static string TempDirectory { get; private set; }
        public static string CompletedDirectory { get; private set; }
        public static string LogsDirectory {get; private set; }
        public static string MinimumLength { get; private set; }

        private static IConfigurationRoot ConfigFile { get; set; }

        private static readonly string DefaultDirectory = Path.Combine(LocalSystem.UserDirectory, ".RipVanBluRay");

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

            if (!string.IsNullOrEmpty(ConfigFile.GetSection("MakeMKV")["MinimumLength"]))
                MinimumLength = ConfigFile.GetSection("MakeMKV")["MinimumLength"];
            else
                MinimumLength = "3600";
        }
    }
}
