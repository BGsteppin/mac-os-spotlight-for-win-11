using System;
using System.Threading;

namespace WinSpotlight.Services
{
    public sealed class SingleInstanceGuard : IDisposable
    {
        private readonly Mutex _mutex;
        private bool _hasHandle;

        public SingleInstanceGuard(string name)
        {
            _mutex = new Mutex(true, name, out _hasHandle);
        }

        public bool TryAcquire()
        {
            if (_hasHandle)
            {
                return true;
            }

            try
            {
                _hasHandle = _mutex.WaitOne(TimeSpan.Zero, true);
            }
            catch (AbandonedMutexException)
            {
                _hasHandle = true;
            }

            return _hasHandle;
        }

        public void Dispose()
        {
            if (_hasHandle)
            {
                _mutex.ReleaseMutex();
            }
            _mutex.Dispose();
        }
    }
}
