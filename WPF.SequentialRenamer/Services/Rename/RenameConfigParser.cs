using System;
using System.Globalization;

namespace WPF.SequentialRenamer.Services.Rename
{
    /// <summary>Результат разбора полей конфигурации: либо готовый конфиг, либо текст ошибки.</summary>
    public class RenameConfigParseResult
    {
        public RenameConfig Config { get; }
        public string Error { get; }

        public RenameConfigParseResult(RenameConfig config)
        {
            Config = config;
            Error = null;
        }

        public RenameConfigParseResult(string error)
        {
            Config = null;
            Error = error;
        }

        public bool HasError => Error != null;
    }

    /// <summary>Разбирает и проверяет введённую пользователем конфигурацию переименования.</summary>
    public class RenameConfigParser
    {
        private const int DefaultDigits = 3;
        private const long DefaultStart = 1;
        private const int MinDigits = 1;
        private const int MaxDigits = 18;

        public RenameConfigParseResult Parse(
            string prefix,
            string suffix,
            string digitsText,
            string startText)
        {
            try
            {
                RenameFileNameUtils.ValidateForbiddenCharacters(prefix, suffix);
                RenameConfig config = new RenameConfig(
                    prefix,
                    suffix,
                    ParseDigits(digitsText),
                    ParseStart(startText));
                return new RenameConfigParseResult(config);
            }
            catch (RenameValidationException ex)
            {
                return new RenameConfigParseResult(ex.Message);
            }
            catch (Exception ex)
            {
                return new RenameConfigParseResult("Непредвиденная ошибка парсера: " + ex.Message);
            }
        }

        public static int ParseDigits(string digitsText)
        {
            string trimmed = (digitsText ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                return DefaultDigits;

            int digits;
            if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out digits)
                || digits < MinDigits
                || digits > MaxDigits)
            {
                throw new RenameValidationException(
                    "Количество цифр в счётчике должно быть числом от " + MinDigits + " до " + MaxDigits + ".");
            }

            return digits;
        }

        public static long ParseStart(string startText)
        {
            string trimmed = (startText ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                return DefaultStart;

            long start;
            if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out start) || start < 0)
            {
                throw new RenameValidationException("Начальный номер должен быть целым неотрицательным числом.");
            }

            return start;
        }
    }
}
