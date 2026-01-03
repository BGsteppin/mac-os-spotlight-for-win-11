using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WinSpotlight.Search;
using WinSpotlight.Services;

namespace WinSpotlight
{
    public partial class MainWindow : Window
    {
        private readonly HotkeyManager _hotkeyManager;
        private readonly SearchCoordinator _searchCoordinator;
        private readonly IconService _iconService;
        private readonly Debouncer _debouncer;
        private readonly object _resultLock = new();
        private IList<SearchResult> _currentResults = new List<SearchResult>();

        public MainWindow()
        {
            InitializeComponent();

            _iconService = new IconService();
            _searchCoordinator = new SearchCoordinator(_iconService);
            _hotkeyManager = new HotkeyManager(ToggleVisibility);
            _debouncer = new Debouncer(TimeSpan.FromMilliseconds(120));

            ResultsList.ItemsSource = _currentResults;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableMicaBackdrop();
            PositionWindow();
            _hotkeyManager.Register();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            HideWindow();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideWindow();
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LaunchSelected();
            }
        }

        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LaunchSelected();
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var query = SearchBox.Text;
            _debouncer.Run(async () => await PerformSearch(query));
        }

        private void ToggleVisibility()
        {
            if (IsVisible)
            {
                HideWindow();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    ShowWindow();
                });
            }
        }

        private void ShowWindow()
        {
            PositionWindow();
            Show();
            Activate();
            SearchBox.Text = string.Empty;
            SearchBox.Focus();
        }

        private void HideWindow()
        {
            Hide();
        }

        private async Task PerformSearch(string query)
        {
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var results = await _searchCoordinator.QueryAsync(query, tokenSource.Token);
            lock (_resultLock)
            {
                _currentResults = results.ToList();
            }

            Dispatcher.Invoke(() =>
            {
                ResultsList.ItemsSource = _currentResults;
                if (_currentResults.Any())
                {
                    ResultsList.SelectedIndex = 0;
                    ResultsList.ScrollIntoView(_currentResults[0]);
                }
            });
        }

        private void MoveSelection(int offset)
        {
            if (!_currentResults.Any())
            {
                return;
            }

            var newIndex = ResultsList.SelectedIndex + offset;
            if (newIndex < 0)
            {
                newIndex = 0;
            }
            else if (newIndex >= _currentResults.Count)
            {
                newIndex = _currentResults.Count - 1;
            }

            ResultsList.SelectedIndex = newIndex;
            ResultsList.ScrollIntoView(_currentResults[newIndex]);
        }

        private void LaunchSelected()
        {
            if (ResultsList.SelectedItem is SearchResult result)
            {
                try
                {
                    ResultLauncher.Launch(result);
                    HideWindow();
                }
                catch (Exception ex)
                {
                    LogService.Logger.Error(ex, "Failed to launch result {Title}", result.Title);
                }
            }
        }

        private void PositionWindow()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            Left = (screenWidth - Width) / 2;
            Top = screenHeight / 3 - Height / 2;
        }

        private void EnableMicaBackdrop()
        {
            var windowHelper = new WindowInteropHelper(this);
            var hwnd = windowHelper.Handle;
            DwmApi.EnableMica(hwnd);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _hotkeyManager.Dispose();
            base.OnClosing(e);
        }
    }
}
