using System.Collections.Generic;
using Creta.Core.Plugin;
using Creta.Infrastructure.UserSettings;
using Flow.Launcher.Plugin;

namespace Creta.Core.ExternalPlugins.Environments
{
    internal class PythonV2Environment : PythonEnvironment
    {
        internal override string Language => AllowedLanguage.PythonV2;

        internal override PluginPair CreatePluginPair(string filePath, PluginMetadata metadata)
        {
            return new PluginPair
            {
                Plugin = new PythonPluginV2(filePath),
                Metadata = metadata
            };
        }
        
        internal PythonV2Environment(List<PluginMetadata> pluginMetadataList, PluginsSettings pluginSettings) : base(pluginMetadataList, pluginSettings) { }
    }
}
