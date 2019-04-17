using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DevOps.Common
{
    public class TempDirectory : IDisposable
    {
        public static TempDirectory CreateTemp(string category, [CallerMemberName]string action = null)
        {
            if (category == null) throw new ArgumentNullException(nameof(category));
            if (action == null) throw new ArgumentNullException(nameof(action));

            return CreateFromBase(Path.GetTempPath(), category, action);
        }

        public static TempDirectory CreateFromBase(string baseFolder, string category, [CallerMemberName]string action = null)
        {
            if (baseFolder == null) throw new ArgumentNullException(nameof(baseFolder));
            if (category == null) throw new ArgumentNullException(nameof(category));
            if (action == null) throw new ArgumentNullException(nameof(action));

            string targetPath = Path.Combine(Path.Combine(baseFolder, "DevOps"), category, action);
            Directory.CreateDirectory(targetPath);

            return new TempDirectory(targetPath);
        }

        public string FullPath { get; }

        public string Concat(string path)
        {
            return Path.Combine(FullPath, path);
        }

        public void Dispose()
        {
            Directory.Delete(FullPath, true);
        }

        private TempDirectory(string fullPath)
        {
            FullPath = fullPath;
        }
    }
}
