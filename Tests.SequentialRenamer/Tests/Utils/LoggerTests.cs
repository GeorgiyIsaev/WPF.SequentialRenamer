using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.SequentialRenamer.Common;
using WPF.SequentialRenamer.Utils;

namespace Tests.SequentialRenamer.Utils
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void Warn_CreatesMissingFileWithLevelAndMessage()
        {
            using (var dir = new TempDirectory())
            {
                var logger = new Logger(Path.Combine(dir.Path, "error_log.txt"));

                logger.Warn("что-то подозрительное");

                string text = File.ReadAllText(Path.Combine(dir.Path, "error_log.txt"));
                StringAssert.Contains(text, "[WARN]");
                StringAssert.Contains(text, "что-то подозрительное");
            }
        }

        [TestMethod]
        public void Error_WritesErrorLevel()
        {
            using (var dir = new TempDirectory())
            {
                var logger = new Logger(Path.Combine(dir.Path, "error_log.txt"));

                logger.Error("что-то сломалось");

                string text = File.ReadAllText(Path.Combine(dir.Path, "error_log.txt"));
                StringAssert.Contains(text, "[ERROR]");
                StringAssert.Contains(text, "что-то сломалось");
            }
        }

        [TestMethod]
        public void Error_WithException_WritesTypeMessageAndStackTrace()
        {
            using (var dir = new TempDirectory())
            {
                var logger = new Logger(Path.Combine(dir.Path, "error_log.txt"));
                Exception failure;
                try
                {
                    throw new InvalidOperationException("сломалось специально");
                }
                catch (Exception ex)
                {
                    failure = ex;
                }

                logger.Error("операция не удалась", failure);

                string text = File.ReadAllText(Path.Combine(dir.Path, "error_log.txt"));
                StringAssert.Contains(text, "InvalidOperationException");
                StringAssert.Contains(text, "сломалось специально");
                StringAssert.Contains(text, "LoggerTests");
            }
        }

        [TestMethod]
        public void Write_SecondRecord_AppendsAfterExistingContent()
        {
            using (var dir = new TempDirectory())
            {
                var logger = new Logger(Path.Combine(dir.Path, "error_log.txt"));

                logger.Warn("первая запись");
                logger.Error("вторая запись");

                string text = File.ReadAllText(Path.Combine(dir.Path, "error_log.txt"));
                int first = text.IndexOf("первая запись", StringComparison.Ordinal);
                int second = text.IndexOf("вторая запись", StringComparison.Ordinal);

                Assert.IsTrue(first >= 0, "Первая запись не найдена.");
                Assert.IsTrue(second > first, "Вторая запись не дописана после первой.");
            }
        }

        [TestMethod]
        public void Write_UnreachablePath_DoesNotThrow()
        {
            using (var dir = new TempDirectory())
            {
                // Папка «missing» не создана — запись не может выполниться.
                var logger = new Logger(Path.Combine(dir.Path, "missing", "error_log.txt"));

                logger.Warn("тест без исключения");
            }
        }
    }
}
