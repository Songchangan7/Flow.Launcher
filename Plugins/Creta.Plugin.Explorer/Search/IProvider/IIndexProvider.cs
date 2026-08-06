using System.Collections.Generic;
using System.Threading;

namespace Creta.Plugin.Explorer.Search.IProvider
{
    public interface IIndexProvider
    {
        public IAsyncEnumerable<SearchResult> SearchAsync(string search, CancellationToken token);
    }
}
