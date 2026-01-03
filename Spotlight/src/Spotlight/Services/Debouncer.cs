using System;
using System.Threading;
using System.Threading.Tasks;

namespace WinSpotlight.Services
{
    public sealed class Debouncer
    {
        private readonly TimeSpan _delay;
        private CancellationTokenSource? _cts;

        public Debouncer(TimeSpan delay)
        {
            _delay = delay;
        }

        public void Run(Func<Task> action)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_delay, token);
                    if (!token.IsCancellationRequested)
                    {
                        await action();
                    }
                }
                catch (TaskCanceledException)
                {
                }
            }, token);
        }
    }
}
