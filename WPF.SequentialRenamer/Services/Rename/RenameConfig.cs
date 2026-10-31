namespace WPF.SequentialRenamer.Services.Rename
{
    /// <summary>Проверенная конфигурация переименования: префикс, суффикс, разрядность и начальный номер.</summary>
    public class RenameConfig
    {
        public string Prefix { get; }
        public string Suffix { get; }
        public int Digits { get; }
        public long Start { get; }

        public RenameConfig(string prefix, string suffix, int digits, long start)
        {
            Prefix = prefix ?? string.Empty;
            Suffix = suffix ?? string.Empty;
            Digits = digits;
            Start = start;
        }
    }
}
