using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinSpotlight.Services;

namespace WinSpotlight.Search
{
    public class ShortcutCatalog
    {
        private readonly IconService _iconService;
        private readonly List<string> _shortcutFolders;
        private readonly IndexingService _indexing;

        public ShortcutCatalog(IconService iconService)
        {
            _iconService = iconService;
            _indexing = new IndexingService();
            _shortcutFolders = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs")
            };
        }

        public Task<IReadOnlyList<SearchResult>> QueryAsync(string query, int maxResults, CancellationToken token)
        {
            return Task.Run(() => QueryInternal(query, maxResults), token);
        }

        private IReadOnlyList<SearchResult> QueryInternal(string query, int maxResults)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return results;
            }

            foreach (var folder in _shortcutFolders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(folder, "*.lnk", SearchOption.AllDirectories)
                    .Concat(Directory.EnumerateFiles(folder, "*.url", SearchOption.AllDirectories)))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new SearchResult
                        {
                            Title = name,
                            Subtitle = file,
                            Path = file,
                            Type = ResultType.Shortcut,
                            Icon = _iconService.GetIcon(file)
                        });
                    }
                }
            }

            return results.OrderBy(r => r.Title).Take(maxResults).ToList();
        }
    }
}
