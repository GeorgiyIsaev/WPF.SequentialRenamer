using System;
using System.IO;

namespace WPF.SequentialRenamer.Services.Common
{
    /// <summary>
    /// Низкоуровневые операции перемещения файлов и папок.
    /// Умеет переименовывать имена, отличающиеся только регистром,
    /// через промежуточное имя — иначе Windows может отказать в таком переименовании.
    /// </summary>
    public class FileSystemMover
    {
        public void Move(string from, string to, bool isFolder)
        {
            if (IsCaseOnlyChange(from, to))
            {
                string tempPath = BuildTempPath(to);
                MoveRaw(from, tempPath, isFolder);
                MoveRaw(tempPath, to, isFolder);
                return;
            }

            MoveRaw(from, to, isFolder);
        }

        public void MoveRaw(string from, string to, bool isFolder)
        {
            if (isFolder)
                Directory.Move(from, to);
            else
                File.Move(from, to);
        }

        /// <summary>Имена различаются только регистром букв (для одной и той же папки).</summary>
        public static bool IsCaseOnlyChange(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return false;

            return string.Equals(from, to, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(from, to, StringComparison.Ordinal);
        }

        /// <summary>Временное имя в той же папке, что и целевой путь.</summary>
        public static string BuildTempPath(string targetPath)
        {
            string directory = Path.GetDirectoryName(targetPath) ?? string.Empty;
            string tempName = "sqr_tmp_" + Guid.NewGuid().ToString("N") + Path.GetExtension(targetPath);
            return Path.Combine(directory, tempName);
        }
    }
}
