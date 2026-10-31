using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WPF.SequentialRenamer.Services.Rename
{
    /// <summary>
    /// Проверяет, не совпадают ли новые имена с именами, которые уже есть в папке.
    /// Сравнение имён регистронезависимое, как и в Windows.
    /// </summary>
    public class RenameConflictChecker
    {
        public IReadOnlyList<string> FindExistingConflicts(IEnumerable<RenameItem> renameItems)
        {
            return FindExistingConflicts(renameItems, ListDirectoryEntries);
        }

        internal static IReadOnlyList<string> FindExistingConflicts(
            IEnumerable<RenameItem> renameItems,
            Func<string, IReadOnlyList<string>> listEntries)
        {
            var conflicts = new List<string>();
            if (renameItems == null)
                return conflicts;

            var groups = renameItems
                .Where(item => item?.NewPath != null)
                .GroupBy(item => Path.GetDirectoryName(item.NewPath) ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var namesBeingRenamed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (RenameItem item in group)
                {
                    if (item.OldPath != null)
                        namesBeingRenamed.Add(Path.GetFileName(item.OldPath));
                }

                var existingNames = new HashSet<string>(
                    listEntries(group.Key) ?? new List<string>(),
                    StringComparer.OrdinalIgnoreCase);

                var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (RenameItem item in group)
                {
                    string newName = Path.GetFileName(item.NewPath);
                    if (string.IsNullOrEmpty(newName))
                        continue;

                    if (!existingNames.Contains(newName))
                        continue;

                    // Имя освободится другим элементом пакета: он сначала уходит во временное имя.
                    if (namesBeingRenamed.Contains(newName))
                        continue;

                    if (reported.Add(newName))
                        conflicts.Add(newName);
                }
            }

            return conflicts;
        }

        private static IReadOnlyList<string> ListDirectoryEntries(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return new List<string>();

            try
            {
                return Directory.EnumerateFileSystemEntries(directory)
                    .Select(Path.GetFileName)
                    .ToList();
            }
            catch (IOException)
            {
                return new List<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>();
            }
        }
    }
}
