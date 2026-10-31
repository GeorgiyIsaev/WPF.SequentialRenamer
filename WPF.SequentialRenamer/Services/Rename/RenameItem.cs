namespace WPF.SequentialRenamer.Services.Rename
{
    /// <summary>Элемент пакета переименования: старый путь, новый путь и промежуточный путь.</summary>
    public class RenameItem
    {
        public string OldPath { get; set; }
        public string NewPath { get; set; }
        public string TempPath { get; set; }
    }
}
