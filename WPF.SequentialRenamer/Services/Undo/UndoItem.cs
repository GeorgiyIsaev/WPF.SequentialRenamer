namespace WPF.SequentialRenamer.Services.Undo
{
    /// <summary>Одна операция отмены: вернуть элемент с текущего пути на исходный.</summary>
    public class UndoItem
    {
        public string OriginalPath { get; set; }
        public string CurrentPath { get; set; }
    }
}
