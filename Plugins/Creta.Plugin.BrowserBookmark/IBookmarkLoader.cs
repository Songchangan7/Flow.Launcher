using Creta.Plugin.BrowserBookmark.Models;
using System.Collections.Generic;

namespace Creta.Plugin.BrowserBookmark;

public interface IBookmarkLoader
{
    public List<Bookmark> GetBookmarks();
}
