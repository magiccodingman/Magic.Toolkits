using Magic.GeneralSystem.Toolkit.Helpers;
using Magic.GeneralSystem.Toolkit.Helpers.Dotnet;
using Magic.GeneralSystem.Toolkit.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit
{
    public class MagicSystemInitialize
    {
        private readonly bool RequiresDotnet;
        private readonly string? StorageFolderName;
        private readonly List<OSPlatform>? SupportedOperatingSystems;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="requiresDotnet">Does the CLI app need to utilize dotnet commands?</param>
        /// <param name="supportedPlatforms">The operating systems your CLI app supports. Leave null for 'any'.</param>
        public MagicSystemInitialize(IEnumerable<OSPlatform>? supportedPlatforms, bool requiresDotnet = false)
        {
            RequiresDotnet = requiresDotnet;
            SupportedOperatingSystems = supportedPlatforms?.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requiresDotnet">Does the CLI app need to utilize dotnet commands?</param>
        public MagicSystemInitialize(bool requiresDotnet = false)
        {
            RequiresDotnet = requiresDotnet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageFolderName">Folder name desired for where you'll place your stored content</param>
        /// <param name="requiresDotnet">Does the CLI app need to utilize dotnet commands?</param>
        /// <param name="supportedPlatforms">The operating systems your CLI app supports. Leave null for 'any'.</param>
        public MagicSystemInitialize(string storageFolderName, IEnumerable<OSPlatform>? supportedPlatforms, bool requiresDotnet = false)
        {
            RequiresDotnet = requiresDotnet;
            SupportedOperatingSystems = supportedPlatforms?.ToList();
            StorageFolderName = storageFolderName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageFolderName">Folder name desired for where you'll place your stored content</param>
        /// <param name="requiresDotnet">Does the CLI app need to utilize dotnet commands?</param>
        public MagicSystemInitialize(string storageFolderName, bool requiresDotnet = false)
        {
            RequiresDotnet = requiresDotnet;
            StorageFolderName = storageFolderName;
        }

        public async Task<AppConfig> BuildAppConfig(string? overrideStorageDirectoryPath = null)
        {
            AppConfig appConfig = new AppConfig()
            {
                InstalledLocation = await ValidateDotnetToolsAvailability(),
                OperatingSystem = OperatingSystemHelper.GetOperatingSystem(),
                RequiresDotnet = this.RequiresDotnet
            };
            if (!string.IsNullOrWhiteSpace(StorageFolderName))
            {
                if (!string.IsNullOrWhiteSpace(overrideStorageDirectoryPath))
                    appConfig.PreferredStoragePath = Path.Combine(overrideStorageDirectoryPath, StorageFolderName);
                else
                    appConfig.PreferredStoragePath = appConfig.FileSystem.FindPreferredStorageLocation(StorageFolderName)?.FullPath;
            }

            bool OsSupported = OperatingSystemHelper.ValidateSupportedOs(SupportedOperatingSystems, appConfig.OperatingSystem);
            if (!OsSupported)
            {
                throw new Exception("The current operating system isn't supported by Magic EF.");
            }

            return appConfig;
        }

        private async Task<InstallLocation> ValidateDotnetToolsAvailability()
        {
            MagicSystemResponse<InstallLocation> response = await ValidateDotnet.ValidateDotnetAvailabilityAsync();
            Console.WriteLine(response.Message);

            if (!response.Success)
            {
                throw new Exception("dotnet commands weren't detected to be available. " +
                    "Please install the necessary NET SDK. Ending process immediately.");
            }

            return response.Result;
        }
    }
}
