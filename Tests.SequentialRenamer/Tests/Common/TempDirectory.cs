using System;
using System.IO;

namespace Tests.SequentialRenamer.Common
{
    /// <summary>Временная папка, удаляемая после теста.</summary>
    public sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "sqr_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string CreateFile(string name, string content = "content")
        {
            string fullPath = System.IO.Path.Combine(Path, name);
            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        public string CreateFolder(string name)
        {
            string fullPath = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        public string[] GetNames()
        {
            string[] entries = Directory.GetFileSystemEntries(Path);
            for (int i = 0; i < entries.Length; i++)
                entries[i] = System.IO.Path.GetFileName(entries[i]);

            Array.Sort(entries, StringComparer.OrdinalIgnoreCase);
            return entries;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
