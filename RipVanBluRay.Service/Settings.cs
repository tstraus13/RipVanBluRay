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
        public static string MinimumLength { get; private set; }

        private static IConfigurationRoot ConfigFile { get; set; }

        /// <summary>
        /// Initializes the settings and loads any settings from
        /// the app settings files
        /// </summary>
        public static void Init()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env}.json", true, true);

            ConfigFile = builder.Build();

            if (!string.IsNullOrEmpty(ConfigFile["TempDirectory"]))
                TempDirectory = ConfigFile["TempDirectory"];

            if (!string.IsNullOrEmpty(ConfigFile["CompletedDirectory"]))
                CompletedDirectory = ConfigFile["CompletedDirectory"];

            if (!string.IsNullOrEmpty(ConfigFile.GetSection("MakeMKV")["MinimumLength"]))
                MinimumLength = ConfigFile.GetSection("MakeMKV")["MinimumLength"];
        }
    }
}
