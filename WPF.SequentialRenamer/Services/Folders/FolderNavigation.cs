using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WPF.SequentialRenamer.Models;

namespace WPF.SequentialRenamer.Services.Folders
{
    /// <summary>Навигация по папкам: чтение содержимого и переход к родительской папке.</summary>
    public class FolderNavigation
    {
        public FolderNavigation()
        {
        }

        public FolderNavigateResult NavigateTo(string path)
        {
            if (string.IsNullOrEmpty(path))
                return new FolderNavigateResult(null, false, "Путь не указан");

            if (!Directory.Exists(path))
                return new FolderNavigateResult(null, false, "Папка не существует");

            string normalized = Path.GetFullPath(path);
            string parent = Directory.GetParent(normalized)?.FullName;
            List<FolderRow> folders = LoadFoldersViaEnumerator(normalized);

            return new FolderNavigateResult(new FolderNavigateState(normalized, parent, folders), true, null);
        }

        /// <summary>Возвращает файлы папки. Список всегда читается с диска заново.</summary>
        public IReadOnlyList<FileRow> LoadFiles(string path)
        {
            return LoadFilesViaEnumerator(path);
        }

        /// <summary>
        /// Ищет ближайшую существующую папку для недоступного пути: если сам путь не существует,
        /// поднимается вверх по иерархии, пока не встретит доступную папку.
        /// Возвращает false, если доступных папок в иерархии нет (включая пустой путь).
        /// </summary>
        public bool TryFindAccessibleFolder(string path, out string accessiblePath)
        {
            accessiblePath = null;
            if (string.IsNullOrEmpty(path))
                return false;

            string current;
            try
            {
                current = Path.GetFullPath(path);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }

            if (Directory.Exists(current))
            {
                accessiblePath = current;
                return true;
            }

            while (true)
            {
                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null)
                    return false;

                if (Directory.Exists(parent.FullName))
                {
                    accessiblePath = parent.FullName;
                    return true;
                }

                current = parent.FullName;
            }
        }

        /// <summary>
        /// Проверяет, что показанный список папок совпадает с фактическим содержимым папки на диске:
        /// совпадают количество и имена (смена только регистра тоже считается изменением).
        /// </summary>
        public bool IsFolderListActual(string rootPath, IReadOnlyList<FolderRow> displayedFolders)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                return false;

            List<FolderRow> actual = LoadFoldersViaEnumerator(rootPath);
            return FolderListsMatch(displayedFolders, actual);
        }

        /// <summary>Сравнивает два списка папок по количеству и именам без учёта порядка.</summary>
        public static bool FolderListsMatch(IReadOnlyList<FolderRow> first, IReadOnlyList<FolderRow> second)
        {
            List<string> firstNames = ToSortedNames(first);
            List<string> secondNames = ToSortedNames(second);

            if (firstNames.Count != secondNames.Count)
                return false;

            for (int i = 0; i < firstNames.Count; i++)
            {
                if (!string.Equals(firstNames[i], secondNames[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static List<string> ToSortedNames(IReadOnlyList<FolderRow> rows)
        {
            var names = new List<string>(rows == null ? 0 : rows.Count);
            if (rows == null)
                return names;

            foreach (FolderRow row in rows)
                names.Add(row == null ? null : row.Name);

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        public List<FolderRow> LoadFoldersViaEnumerator(string path)
        {
            var rows = new List<FolderRow>();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return rows;

            try
            {
                foreach (string dir in Directory.EnumerateDirectories(path))
                {
                    int fileCount = 0;
                    int subfolderCount = 0;

                    try { fileCount = Directory.EnumerateFiles(dir).Count(); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }

                    try { subfolderCount = Directory.EnumerateDirectories(dir).Count(); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }

                    rows.Add(new FolderRow
                    {
                        Name = Path.GetFileName(dir),
                        Path = dir,
                        FileCount = fileCount,
                        SubfolderCount = subfolderCount
                    });
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return rows;
        }

        public List<FileRow> LoadFilesViaEnumerator(string path)
        {
            var rows = new List<FileRow>();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return rows;

            try
            {
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    rows.Add(new FileRow
                    {
                        NameWithoutExt = Path.GetFileNameWithoutExtension(file),
                        Extension = Path.GetExtension(file),
                        Path = file
                    });
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return rows;
        }

        public bool TryGetParentPath(string path, out string parentPath)
        {
            parentPath = null;
            if (string.IsNullOrEmpty(path))
                return false;

            DirectoryInfo parent = Directory.GetParent(path);
            if (parent == null)
                return false;

            parentPath = parent.FullName;
            return true;
        }
    }

    public class FolderNavigateResult
    {
        public FolderNavigateState State { get; }
        public bool Success { get; }
        public string Error { get; }

        public FolderNavigateResult(FolderNavigateState state, bool success, string error)
        {
            State = state;
            Success = success;
            Error = error;
        }
    }

    public class FolderNavigateState
    {
        public string Path { get; }
        public string ParentPath { get; }
        public IReadOnlyList<FolderRow> Folders { get; }

        public FolderNavigateState(string path, string parentPath, IReadOnlyList<FolderRow> folders)
        {
            Path = path;
            ParentPath = parentPath;
            Folders = folders;
        }
    }
}
