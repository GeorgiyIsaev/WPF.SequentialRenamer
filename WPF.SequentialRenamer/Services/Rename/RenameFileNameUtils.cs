using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace WPF.SequentialRenamer.Services.Rename
{
    /// <summary>Ошибка проверки конфигурации переименования.</summary>
    public class RenameValidationException : Exception
    {
        public RenameValidationException(string message) : base(message)
        {
        }
    }

    public static class RenameFileNameUtils
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        private static readonly HashSet<string> ReservedDeviceNames = BuildReservedDeviceNames();

        private static HashSet<string> BuildReservedDeviceNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CON", "PRN", "AUX", "NUL"
            };
            for (int i = 1; i <= 9; i++)
            {
                names.Add("COM" + i.ToString(CultureInfo.InvariantCulture));
                names.Add("LPT" + i.ToString(CultureInfo.InvariantCulture));
            }
            return names;
        }

        /// <summary>
        /// Проверяет итоговое имя файла (префикс + счётчик + суффикс + расширение) целиком.
        /// Возвращает краткое описание проблемы или null, если имя допустимо и Windows не изменит его при создании.
        /// </summary>
        public static string FindFinalNameProblem(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "пустое имя";

            int bad = fileName.IndexOfAny(InvalidFileNameChars);
            if (bad >= 0)
                return "недопустимый символ «" + fileName[bad] + "»";

            string stripped = fileName.TrimEnd('.', ' ');
            if (stripped.Length == 0)
                return "имя «" + fileName + "» состоит только из точек и пробелов — Windows обрежет его целиком";
            if (stripped.Length != fileName.Length)
                return "Windows отбрасывает завершающие точки и пробелы: «" + fileName + "» превратится в «" + stripped + "»";

            string head = fileName.Split('.')[0].TrimEnd(' ');
            if (ReservedDeviceNames.Contains(head))
                return "«" + fileName + "» — зарезервированное имя устройства Windows";

            return null;
        }

        public static void ValidateForbiddenCharacters(string prefix, string suffix)
        {
            if (prefix != null)
            {
                int bad = prefix.IndexOfAny(InvalidFileNameChars);
                if (bad >= 0)
                    throw new RenameValidationException("Префикс содержит недопустимые символы: " + prefix[bad]);
            }

            if (suffix != null)
            {
                int bad = suffix.IndexOfAny(InvalidFileNameChars);
                if (bad >= 0)
                    throw new RenameValidationException("Суффикс содержит недопустимые символы: " + suffix[bad]);
            }
        }

        public static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return string.Empty;

            if (extension[0] != '.')
                return "." + extension;

            return extension;
        }

        public static string BuildFileName(string prefix, string suffix, int digits, long counter, string extension)
        {
            string safePrefix = prefix ?? string.Empty;
            string safeSuffix = suffix ?? string.Empty;
            string padded = counter.ToString("D" + digits, CultureInfo.InvariantCulture);
            return safePrefix + padded + safeSuffix + NormalizeExtension(extension);
        }
    }
}
