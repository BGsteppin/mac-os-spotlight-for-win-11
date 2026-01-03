using System;
using System.Windows.Media;

namespace WinSpotlight.Search
{
    public enum ResultType
    {
        Application,
        Shortcut,
        File,
        Folder,
        Url,
        Path
    }

    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public ResultType Type { get; set; }
        public ImageSource? Icon { get; set; }
        public DateTime LastUsed { get; set; } = DateTime.MinValue;
    }
}
