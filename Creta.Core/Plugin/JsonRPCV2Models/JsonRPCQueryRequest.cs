using System.Collections.Generic;
using Flow.Launcher.Plugin;

namespace Creta.Core.Plugin.JsonRPCV2Models
{
    public record JsonRPCQueryRequest(
        List<JsonRPCResult> Results
    );
}
