using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WPF.SequentialRenamer.Models;
using WPF.SequentialRenamer.Services.Undo;

namespace WPF.SequentialRenamer.Services.Rename
{
    /// <summary>Строит план переименования для папок и файлов.</summary>
    public class RenamePlanner
    {
        public List<RenameItem> BuildRenameItemsForFolder(
            IEnumerable<FolderRow> folderRows,
            RenameConfig config,
            string rootPath)
        {
            var items = new List<RenameItem>();
            if (folderRows == null)
                return items;

            long counter = config.Start;

            foreach (FolderRow row in folderRows)
            {
                items.Add(new RenameItem
                {
                    OldPath = row.Path,
                    NewPath = Path.Combine(rootPath, RenameFileNameUtils.BuildFileName(
                        config.Prefix, config.Suffix, config.Digits, counter, string.Empty))
                });
                counter++;
            }

            return items;
        }

        public List<RenameItem> BuildRenameItemsForFiles(
            IEnumerable<FileRow> fileRows,
            RenameConfig config,
            string folderPath)
        {
            var items = new List<RenameItem>();
            if (fileRows == null)
                return items;

            long counter = config.Start;

            foreach (FileRow row in fileRows)
            {
                string normalizedExtension = RenameFileNameUtils.NormalizeExtension(row.Extension);
                items.Add(new RenameItem
                {
                    OldPath = row.Path,
                    NewPath = Path.Combine(folderPath, RenameFileNameUtils.BuildFileName(
                        config.Prefix, config.Suffix, config.Digits, counter, normalizedExtension))
                });
                counter++;
            }

            return items;
        }

        /// <summary>
        /// Проверяет итоговые имена всех элементов плана: обрежет ли Windows имя (завершающие
        /// точки/пробелы), нет ли зарезервированных имён устройств и недопустимых символов.
        /// Возвращает уникальные описания проблем; пустой список — все имена допустимы.
        /// </summary>
        public static List<string> FindFinalNameProblems(IEnumerable<RenameItem> items)
        {
            var problems = new List<string>();
            if (items == null)
                return problems;

            var reported = new HashSet<string>(StringComparer.Ordinal);
            foreach (RenameItem item in items)
            {
                if (item?.NewPath == null)
                    continue;

                string problem = RenameFileNameUtils.FindFinalNameProblem(Path.GetFileName(item.NewPath));
                if (problem != null && reported.Add(problem))
                    problems.Add(problem);
            }

            return problems;
        }

        public List<UndoItem> BuildUndoItems(IEnumerable<RenameItem> renameItems)
        {
            var list = renameItems as IList<RenameItem> ?? renameItems.ToList();
            var items = new List<UndoItem>(list.Count);

            foreach (RenameItem item in list)
            {
                items.Add(new UndoItem
                {
                    CurrentPath = item.NewPath,
                    OriginalPath = item.OldPath
                });
            }

            return items;
        }
    }
}
