using System.Collections.Generic;
using System.Threading;

namespace Creta.Plugin.Explorer.Search.IProvider
{
    public interface IPathIndexProvider
    {
        public IAsyncEnumerable<SearchResult> EnumerateAsync(string path, string search, bool recursive, CancellationToken token);
    }
}
