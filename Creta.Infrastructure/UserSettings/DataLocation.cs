using System;
using System.IO;

namespace Creta.Infrastructure.UserSettings
{
    public static class DataLocation
    {
        public const string PortableFolderName = "UserData";
        public const string DeletionIndicatorFile = ".dead";
        private const string LegacyRoamingFolderName = "FlowLauncher";
        public static readonly string PortableDataPath = Path.Combine(Constant.ProgramDirectory, PortableFolderName);
        public static readonly string RoamingDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Creta");
        public static readonly string LegacyRoamingDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), LegacyRoamingFolderName);

        static DataLocation()
        {
            TryMigrateLegacyRoamingData();
        }

        private static void TryMigrateLegacyRoamingData()
        {
            try
            {
                if (Directory.Exists(RoamingDataPath) || !Directory.Exists(LegacyRoamingDataPath))
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(RoamingDataPath)!);
                // Prefer move so settings follow the rebranded app without leaving a divergent copy.
                Directory.Move(LegacyRoamingDataPath, RoamingDataPath);
            }
            catch
            {
                // Migration is best-effort; app can still start with fresh settings.
            }
        }

        public static string DataDirectory()
        {
            if (PortableDataLocationInUse())
                return PortableDataPath;

            return RoamingDataPath;
        }

        public static bool PortableDataLocationInUse()
        {
            if (Directory.Exists(PortableDataPath) &&
                !File.Exists(Path.Combine(PortableDataPath, DeletionIndicatorFile)))
                return true;

            return false;
        }

        public static string VersionLogDirectory => Path.Combine(LogDirectory, Constant.Version);
        public static string LogDirectory => Path.Combine(DataDirectory(), Constant.Logs);

        public static readonly string CacheDirectory = Path.Combine(DataDirectory(), Constant.Cache);
        public static readonly string SettingsDirectory = Path.Combine(DataDirectory(), Constant.Settings);
        public static readonly string PluginsDirectory = Path.Combine(DataDirectory(), Constant.Plugins);
        public static readonly string ThemesDirectory = Path.Combine(DataDirectory(), Constant.Themes);

        public static readonly string PluginSettingsDirectory = Path.Combine(SettingsDirectory, Constant.Plugins);
        public static readonly string PluginCacheDirectory = Path.Combine(DataDirectory(), Constant.Cache, Constant.Plugins);

        public const string PythonEnvironmentName = "Python";
        public const string NodeEnvironmentName = "Node.js";
        public const string PluginEnvironments = "Environments";
        public const string PluginDeleteFile = "NeedDelete.txt";
        public static readonly string PluginEnvironmentsPath = Path.Combine(DataDirectory(), PluginEnvironments);
    }
}
