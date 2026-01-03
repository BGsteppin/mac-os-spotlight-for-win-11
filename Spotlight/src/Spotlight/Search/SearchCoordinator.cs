using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinSpotlight.Services;

namespace WinSpotlight.Search
{
    public class SearchCoordinator
    {
        private readonly EverythingSearch _everything;
        private readonly IndexingService _indexing;
        private readonly ShortcutCatalog _shortcuts;
        private readonly IconService _icons;

        public SearchCoordinator(IconService icons)
        {
            _icons = icons;
            _everything = new EverythingSearch();
            _indexing = new IndexingService();
            _shortcuts = new ShortcutCatalog(icons);
        }

        public async Task<IReadOnlyList<SearchResult>> QueryAsync(string query, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<SearchResult>();
            }

            var tasks = new List<Task<IReadOnlyList<SearchResult>>>();
            tasks.Add(_shortcuts.QueryAsync(query, 50, token));
            tasks.Add(_indexing.QueryAsync(query, 50, token));

            if (_everything.IsAvailable)
            {
                tasks.Add(_everything.QueryAsync(query, 30, token));
            }

            var results = (await Task.WhenAll(tasks)).SelectMany(r => r).ToList();
            results = results
                .GroupBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(r => Score(r)).First())
                .OrderByDescending(r => Score(r))
                .ThenBy(r => r.Title)
                .Take(50)
                .ToList();

            foreach (var result in results)
            {
                result.Icon ??= _icons.GetIcon(result.Path);
            }

            return results;
        }

        private double Score(SearchResult result)
        {
            double baseScore = result.Type switch
            {
                ResultType.Application => 100,
                ResultType.Shortcut => 90,
                ResultType.File => 70,
                ResultType.Folder => 60,
                _ => 40
            };

            if (result.LastUsed != DateTime.MinValue)
            {
                var age = (DateTime.UtcNow - result.LastUsed).TotalHours;
                baseScore += Math.Max(0, 50 - age);
            }

            return baseScore;
        }
    }
}
