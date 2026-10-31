using System;
using System.Collections.Generic;

namespace WPF.SequentialRenamer.Services.Undo
{
    /// <summary>Стек шагов отмены. Повторная отмена снимает следующий шаг.</summary>
    public class RenameUndoLog
    {
        private readonly Stack<UndoEntry> _entries = new Stack<UndoEntry>();

        public bool HasEntries => _entries.Count > 0;

        public void Push(UndoEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (entry.Items.Count == 0)
                return;

            _entries.Push(entry);
        }

        public UndoEntry Pop()
        {
            if (_entries.Count == 0)
                throw new InvalidOperationException("Отмена: нет записей для отмены.");

            return _entries.Pop();
        }
    }
}
