namespace WPF.SequentialRenamer.Services.Common
{
    /// <summary>Ошибка файловой операции: путь и описание причины.</summary>
    public class FileSystemError
    {
        public string Path { get; }
        public string Message { get; }

        public FileSystemError(string path, string message)
        {
            Path = path;
            Message = message;
        }

        public override string ToString()
        {
            return Path + " — " + Message;
        }
    }
}
