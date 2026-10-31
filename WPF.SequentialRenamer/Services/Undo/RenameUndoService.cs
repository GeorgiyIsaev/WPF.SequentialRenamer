using System;
using System.Collections.Generic;
using WPF.SequentialRenamer.Services.Common;

namespace WPF.SequentialRenamer.Services.Undo
{
    public class RenameUndoResult
    {
        public List<UndoItem> Done { get; } = new List<UndoItem>();
        public List<FileSystemError> Errors { get; } = new List<FileSystemError>();

        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>
    /// Возвращает элементы шага отмены на исходные пути.
    /// Переименование, меняющее только регистр, выполняется через временное имя.
    /// </summary>
    public class RenameUndoService
    {
        private readonly FileSystemMover _mover;

        public RenameUndoService() : this(new FileSystemMover())
        {
        }

        public RenameUndoService(FileSystemMover mover)
        {
            _mover = mover ?? throw new ArgumentNullException(nameof(mover));
        }

        public RenameUndoResult Execute(UndoEntry entry)
        {
            var result = new RenameUndoResult();
            if (entry == null)
                return result;

            foreach (UndoItem item in entry.Items)
            {
                if (item?.CurrentPath == null || item.OriginalPath == null)
                    continue;

                try
                {
                    _mover.Move(item.CurrentPath, item.OriginalPath, entry.IsFolders);
                    result.Done.Add(item);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new FileSystemError(item.CurrentPath, ex.Message));
                }
            }

            return result;
        }
    }
}
