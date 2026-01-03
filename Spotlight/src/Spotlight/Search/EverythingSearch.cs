using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinSpotlight.Search
{
    public class EverythingSearch
    {
        private const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        private const int EVERYTHING_REQUEST_PATH = 0x00000002;
        private const int EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;
        private const int EVERYTHING_REQUEST_SIZE = 0x00000010;
        private const int EVERYTHING_REQUEST_EXTENSION = 0x00000008;

        public bool IsAvailable => NativeMethods.Everything_SetSearch != null && NativeMethods.TryLoad();

        public Task<IReadOnlyList<SearchResult>> QueryAsync(string query, int maxResults, CancellationToken token)
        {
            return Task.Run(() => Query(query, maxResults), token);
        }

        private IReadOnlyList<SearchResult> Query(string query, int maxResults)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return results;
            }

            NativeMethods.Everything_SetSearch(query);
            NativeMethods.Everything_SetMax(maxResults);
            NativeMethods.Everything_SetRequestFlags(EVERYTHING_REQUEST_FILE_NAME | EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_DATE_MODIFIED | EVERYTHING_REQUEST_EXTENSION | EVERYTHING_REQUEST_SIZE);
            if (!NativeMethods.Everything_Query(true))
            {
                return results;
            }

            int count = NativeMethods.Everything_GetNumResults();
            for (int i = 0; i < count; i++)
            {
                var builder = new StringBuilder(260);
                NativeMethods.Everything_GetResultFullPathName(i, builder, builder.Capacity);
                var path = builder.ToString();
                var title = NativeMethods.Everything_GetResultFileName(i);
                results.Add(new SearchResult
                {
                    Title = title,
                    Path = path,
                    Subtitle = path,
                    Type = NativeMethods.Everything_IsFolderResult(i) ? ResultType.Folder : ResultType.File
                });
            }

            return results;
        }

        private static class NativeMethods
        {
            private const string DllName = "Everything64.dll";
            private static bool _loaded;

            internal static bool TryLoad()
            {
                if (_loaded)
                {
                    return true;
                }

                try
                {
                    _loaded = Everything_Query(true);
                }
                catch
                {
                    _loaded = false;
                }

                return _loaded;
            }

            [DllImport(DllName, CharSet = CharSet.Unicode, EntryPoint = "Everything_SetSearchW", CallingConvention = CallingConvention.StdCall)]
            internal static extern void Everything_SetSearch(string lpSearchString);

            [DllImport(DllName, CharSet = CharSet.Unicode, EntryPoint = "Everything_SetRequestFlagsW", CallingConvention = CallingConvention.StdCall)]
            internal static extern void Everything_SetRequestFlags(uint dwRequestFlags);

            [DllImport(DllName, CharSet = CharSet.Unicode, EntryPoint = "Everything_SetMaxW", CallingConvention = CallingConvention.StdCall)]
            internal static extern void Everything_SetMax(int max);

            [DllImport(DllName, CharSet = CharSet.Unicode, EntryPoint = "Everything_QueryW", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool Everything_Query(bool bWait);

            [DllImport(DllName, CharSet = CharSet.Unicode, EntryPoint = "Everything_GetNumResults", CallingConvention = CallingConvention.StdCall)]
            internal static extern int Everything_GetNumResults();

            [DllImport(DllName, CharSet = CharSet.Unicode, EntryPoint = "Everything_GetResultFullPathNameW", CallingConvention = CallingConvention.StdCall)]
            internal static extern uint Everything_GetResultFullPathName(int nIndex, StringBuilder lpString, int nMaxCount);

            [DllImport(DllName, CharSet = CharSet.Unicode, EntryPoint = "Everything_GetResultFileNameW", CallingConvention = CallingConvention.StdCall)]
            internal static extern string Everything_GetResultFileName(int nIndex);

            [DllImport(DllName, EntryPoint = "Everything_IsFolderResult", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool Everything_IsFolderResult(int nIndex);
        }
    }
}
