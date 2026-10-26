using System;
using System.IO;
using System.Threading.Tasks;

namespace SlideShot
{
    internal static class AppPaths
    {
        public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string DataDir = Path.Combine(BaseDir, "data");
        public static readonly string CacheDir = Path.Combine(DataDir, "cache");

        public static void Prepare()
        {
            Directory.CreateDirectory(DataDir);
            DeleteWithRetry(CacheDir, 0);
            Directory.CreateDirectory(CacheDir);
        }

        public static string NewDocumentCache(string tabId)
        {
            string dir = Path.Combine(CacheDir, tabId);
            Directory.CreateDirectory(Path.Combine(dir, "full"));
            Directory.CreateDirectory(Path.Combine(dir, "thumb"));
            return dir;
        }

        public static void DropDocumentCache(string tabId)
        {
            DeleteWithRetry(Path.Combine(CacheDir, tabId), 0);
        }

        public static void ClearCache()
        {
            DeleteWithRetry(CacheDir, 0);
        }

        private static void DeleteWithRetry(string dir, int attempt)
        {
            try
            {
                if (!Directory.Exists(dir)) return;
                Directory.Delete(dir, true);
                return;
            }
            catch
            {
                if (attempt >= 5) return;
            }

            Task.Delay(1200).ContinueWith(delegate { DeleteWithRetry(dir, attempt + 1); });
        }
    }
}
