using System;
using System.IO;
using System.Text;

namespace WPF.SequentialRenamer.Utils
{
    /// <summary>
    /// Пишет предупреждения (WARN) и ошибки (ERROR) в текстовый файл рядом с exe.
    /// Если файла нет — он создаётся, если есть — записи дописываются в конец.
    /// </summary>
    public class Logger
    {
        private const string DefaultFileName = "error_log.txt";

        private readonly string _filePath;
        private readonly object _writeLock = new object();

        public Logger() : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName))
        {
        }

        public Logger(string filePath)
        {
            _filePath = filePath;
        }

        public string FilePath => _filePath;

        public void Warn(string message)
        {
            Write("WARN", message, null);
        }

        public void Warn(string message, Exception exception)
        {
            Write("WARN", message, exception);
        }

        public void Error(string message)
        {
            Write("ERROR", message, null);
        }

        public void Error(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        private void Write(string level, string message, Exception exception)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] " +
                          (message ?? string.Empty);
            if (exception != null)
                line += Environment.NewLine + exception;

            try
            {
                lock (_writeLock)
                {
                    // AppendAllText сам создаёт файл, если его ещё нет.
                    File.AppendAllText(_filePath, line + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Запись лога не должна ронять приложение — ошибки записи игнорируем.
            }
        }
    }
}
