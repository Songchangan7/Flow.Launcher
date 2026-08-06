using System.Collections.Generic;
using Creta.Infrastructure.UserSettings;
using Flow.Launcher.Plugin;

namespace Creta.Core.ExternalPlugins.Environments
{
    internal class JavaScriptV2Environment : TypeScriptV2Environment
    {
        internal override string Language => AllowedLanguage.JavaScriptV2;

        internal JavaScriptV2Environment(List<PluginMetadata> pluginMetadataList, PluginsSettings pluginSettings) : base(pluginMetadataList, pluginSettings) { }
    }
}
