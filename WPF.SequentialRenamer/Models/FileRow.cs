using System;

namespace WPF.SequentialRenamer.Models
{
    /// <summary>Строка блока файлов (сортировка по имени без расширения).</summary>
    public class FileRow : IEquatable<FileRow>
    {
        public string NameWithoutExt { get; set; }
        public string Extension { get; set; }
        public string Path { get; set; }

        public bool Equals(FileRow other)
        {
            if (other is null) return false;
            return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileRow);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Path?.ToLowerInvariant().GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
