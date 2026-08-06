using System.IO;
using System.Threading.Tasks;
using Creta.Infrastructure.Logger;
using Creta.Infrastructure.UserSettings;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.SharedCommands;

namespace Creta.Infrastructure.Storage
{
    // Expose ISaveable interface in derived class to make sure we are calling the new version of Save method
    public class CretaJsonStorage<T> : JsonStorage<T>, ISavable where T : new()
    {
        private static readonly string ClassName = "CretaJsonStorage";

        public CretaJsonStorage()
        {
            DirectoryPath = Path.Combine(DataLocation.DataDirectory(), DirectoryName);
            FilesFolders.ValidateDirectory(DirectoryPath);

            var filename = typeof(T).Name;
            FilePath = Path.Combine(DirectoryPath, $"{filename}{FileSuffix}");
        }

        public new void Save()
        {
            try
            {
                base.Save();
            }
            catch (System.Exception e)
            {
                Log.Exception(ClassName, $"Failed to save FL settings to path: {FilePath}", e);
            }
        }

        public new async Task SaveAsync()
        {
            try
            {
                await base.SaveAsync();
            }
            catch (System.Exception e)
            {
                Log.Exception(ClassName, $"Failed to save FL settings to path: {FilePath}", e);
            }
        }
    }
}
