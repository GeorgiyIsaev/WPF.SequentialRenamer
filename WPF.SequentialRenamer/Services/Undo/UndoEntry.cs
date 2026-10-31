using System.Collections.Generic;

namespace WPF.SequentialRenamer.Services.Undo
{
    /// <summary>Один шаг истории отмены.</summary>
    public class UndoEntry
    {
        public bool IsFolders { get; set; }
        public List<UndoItem> Items { get; } = new List<UndoItem>();

        public void Add(IEnumerable<UndoItem> items)
        {
            if (items == null)
                return;

            Items.AddRange(items);
        }
    }
}
