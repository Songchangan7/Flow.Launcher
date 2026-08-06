using Flow.Launcher.Plugin;

namespace Creta.Core.Plugin;

public interface IResultUpdateRegister
{
    /// <summary>
    /// Register a plugin to receive results updated event.
    /// </summary>
    /// <param name="pair"></param>
    void RegisterResultsUpdatedEvent(PluginPair pair);
}
