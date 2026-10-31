using System;
using System.Globalization;

namespace WPF.SequentialRenamer.Utils
{
    /// <summary>
    /// Сравнение имён для сортировки и поиска.
    /// Игнорирует регистр (OrdinalIgnoreCase) и не зависит от текущей культуры,
    /// чтобы сортировка и обнаружение дублей велись одинаково независимо от настроек системы.
    /// </summary>
    public static class NameComparer
    {
        private static bool IsInteger(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            foreach (char c in name)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        /// <summary>Сравнение имён без расширения (с использованием целых чисел, если возможно).</summary>
        public static int CompareWithoutExtension(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (IsInteger(x) && IsInteger(y))
            {
                // Число длиннее long (20+ цифр) не парсится — тогда сравниваем как строки.
                long valueX, valueY;
                if (long.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out valueX)
                    && long.TryParse(y, NumberStyles.Integer, CultureInfo.InvariantCulture, out valueY))
                {
                    int byValue = valueX.CompareTo(valueY);
                    if (byValue != 0) return byValue;
                }
            }

            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }
    }
}
