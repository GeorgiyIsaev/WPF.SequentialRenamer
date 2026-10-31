namespace WPF.SequentialRenamer.Services.Files
{
    /// <summary>Файл из подпапки, который поднимается в открытую папку.</summary>
    public class MoveUpItem
    {
        public string SourcePath { get; set; }
        public string FolderName { get; set; }
        public string TargetPath { get; set; }
    }
}
