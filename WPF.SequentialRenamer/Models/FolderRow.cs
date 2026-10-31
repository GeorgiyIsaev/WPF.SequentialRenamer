using System;

namespace WPF.SequentialRenamer.Models
{
    /// <summary>Строка блока папок.</summary>
    public class FolderRow : IEquatable<FolderRow>
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int FileCount { get; set; }
        public int SubfolderCount { get; set; }

        public bool Equals(FolderRow other)
        {
            if (other is null) return false;
            return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FolderRow);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Path?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 23 + (Name?.ToLowerInvariant().GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
