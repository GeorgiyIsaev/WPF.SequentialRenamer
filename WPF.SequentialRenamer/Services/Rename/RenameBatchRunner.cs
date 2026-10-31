using System;
using System.Collections.Generic;
using System.Linq;
using WPF.SequentialRenamer.Services.Common;

namespace WPF.SequentialRenamer.Services.Rename
{
    public class RenameBatchResult
    {
        public List<RenameItem> Done { get; } = new List<RenameItem>();
        public List<FileSystemError> Errors { get; } = new List<FileSystemError>();

        public bool HasErrors => Errors.Count > 0;
        public bool IsEmpty => Done.Count == 0 && Errors.Count == 0;
    }

    /// <summary>
    /// Выполняет пакет переименования в два прохода: сначала все элементы получают
    /// временные имена, затем — итоговые. Так переименование не зависит от порядка
    /// и не конфликтует само с собой (в том числе при смене только регистра).
    /// </summary>
    public class RenameBatchRunner
    {
        private readonly FileSystemMover _mover;

        public RenameBatchRunner() : this(new FileSystemMover())
        {
        }

        public RenameBatchRunner(FileSystemMover mover)
        {
            _mover = mover ?? throw new ArgumentNullException(nameof(mover));
        }

        public RenameBatchResult Run(IEnumerable<RenameItem> renameItems, bool isFolders)
        {
            var result = new RenameBatchResult();
            if (renameItems == null)
                return result;

            List<RenameItem> items = renameItems.ToList();

            MoveToTempPaths(items, result, isFolders);
            MoveToTargetPaths(items, result, isFolders);

            return result;
        }

        private void MoveToTempPaths(List<RenameItem> items, RenameBatchResult result, bool isFolders)
        {
            foreach (RenameItem item in items)
            {
                if (item?.OldPath == null || item.NewPath == null)
                {
                    result.Errors.Add(new FileSystemError(item?.OldPath ?? string.Empty, "Не задан старый или новый путь."));
                    continue;
                }

                string tempPath = FileSystemMover.BuildTempPath(item.OldPath);
                try
                {
                    _mover.MoveRaw(item.OldPath, tempPath, isFolders);
                    item.TempPath = tempPath;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new FileSystemError(item.OldPath, ex.Message));
                }
            }
        }

        private void MoveToTargetPaths(List<RenameItem> items, RenameBatchResult result, bool isFolders)
        {
            foreach (RenameItem item in items)
            {
                if (item == null || item.TempPath == null)
                    continue;

                string tempPath = item.TempPath;
                try
                {
                    _mover.MoveRaw(tempPath, item.NewPath, isFolders);
                    item.TempPath = null;
                    result.Done.Add(item);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new FileSystemError(tempPath, ex.Message));
                    RestoreOriginalPath(item, tempPath, isFolders);
                }
            }
        }

        private void RestoreOriginalPath(RenameItem item, string tempPath, bool isFolders)
        {
            try
            {
                _mover.MoveRaw(tempPath, item.OldPath, isFolders);
                item.TempPath = null;
            }
            catch (Exception)
            {
                // Файл остался под временным именем — сообщение об ошибке уже добавлено.
            }
        }
    }
}
