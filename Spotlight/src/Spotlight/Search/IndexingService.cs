using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using WinSpotlight.Services;

namespace WinSpotlight.Search
{
    public class IndexingService : IDisposable
    {
        private readonly string _databasePath;
        private readonly string[] _defaultFolders;
        private readonly Timer _maintenanceTimer;
        private readonly object _lock = new();
        private bool _initialized;

        public IndexingService()
        {
            _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinSpotlight", "index.db");
            _defaultFolders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };
            Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
            _maintenanceTimer = new Timer(_ => RunMaintenance(), null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(15));
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            lock (_lock)
            {
                if (_initialized)
                {
                    return;
                }

                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS entries(path TEXT PRIMARY KEY, title TEXT, kind TEXT, modified INTEGER, size INTEGER);";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS recent(path TEXT PRIMARY KEY, last_used INTEGER);";
                cmd.ExecuteNonQuery();
                _initialized = true;
            }

            Task.Run(() => IndexFolders(_defaultFolders));
        }

        public void RecordUsage(string path)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO recent(path, last_used) VALUES ($p, $t) ON CONFLICT(path) DO UPDATE SET last_used = excluded.last_used;";
            cmd.Parameters.AddWithValue("$p", path);
            cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
        }

        public async Task<IReadOnlyList<SearchResult>> QueryAsync(string query, int maxResults, CancellationToken token)
        {
            Initialize();
            return await Task.Run(() => QueryInternal(query, maxResults), token);
        }

        private IReadOnlyList<SearchResult> QueryInternal(string query, int maxResults)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return results;
            }

            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT path, title, kind FROM entries WHERE title LIKE $q OR path LIKE $q ORDER BY kind = 'app' DESC, kind = 'shortcut' DESC, kind = 'file' DESC LIMIT $max";
            cmd.Parameters.AddWithValue("$q", $"%{query}%");
            cmd.Parameters.AddWithValue("$max", maxResults);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var path = reader.GetString(0);
                var title = reader.GetString(1);
                var kind = reader.GetString(2);
                results.Add(new SearchResult
                {
                    Path = path,
                    Title = title,
                    Subtitle = path,
                    Type = MapKind(kind)
                });
            }

            return results;
        }

        private void IndexFolders(IEnumerable<string> folders)
        {
            foreach (var folder in folders)
            {
                if (Directory.Exists(folder))
                {
                    IndexFolder(folder);
                }
            }
        }

        private void IndexFolder(string folder)
        {
            try
            {
                foreach (var path in Directory.EnumerateFileSystemEntries(folder, "*", SearchOption.AllDirectories))
                {
                    AddOrUpdate(path);
                }
            }
            catch (Exception ex)
            {
                LogService.Logger.Error(ex, "Indexing failed for {Folder}", folder);
            }
        }

        private void AddOrUpdate(string path)
        {
            var info = new FileInfo(path);
            var kind = info.Attributes.HasFlag(FileAttributes.Directory) ? "folder" : "file";
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO entries(path, title, kind, modified, size) VALUES ($p, $t, $k, $m, $s);";
            cmd.Parameters.AddWithValue("$p", path);
            cmd.Parameters.AddWithValue("$t", info.Name);
            cmd.Parameters.AddWithValue("$k", kind);
            cmd.Parameters.AddWithValue("$m", info.LastWriteTimeUtc.ToFileTimeUtc());
            cmd.Parameters.AddWithValue("$s", info.Exists ? info.Length : 0);
            cmd.ExecuteNonQuery();
        }

        private ResultType MapKind(string kind)
        {
            return kind switch
            {
                "app" => ResultType.Application,
                "shortcut" => ResultType.Shortcut,
                "folder" => ResultType.Folder,
                _ => ResultType.File
            };
        }

        private void RunMaintenance()
        {
            try
            {
                Initialize();
                var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM entries WHERE NOT EXISTS (SELECT 1 FROM pragma_table_info('entries'))";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogService.Logger.Error(ex, "Maintenance failed");
            }
        }

        public void Dispose()
        {
            _maintenanceTimer.Dispose();
        }
    }
}
