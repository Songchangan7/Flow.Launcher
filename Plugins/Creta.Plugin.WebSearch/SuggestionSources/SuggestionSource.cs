using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Creta.Plugin.WebSearch.SuggestionSources
{
    public abstract class SuggestionSource
    {
        public abstract Task<List<string>> SuggestionsAsync(string query, CancellationToken token);
    }
}
