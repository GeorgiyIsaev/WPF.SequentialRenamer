using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPF.SequentialRenamer.Models;
using WPF.SequentialRenamer.Utils;

namespace Tests.SequentialRenamer.Models
{
    [TestClass]
    public class ModelEqualityTests
    {
        [TestMethod]
        public void FolderRow_EqualPathsIgnoreCase_TreatedAsEqual()
        {
            var a = new FolderRow { Path = @"C:\Folder\X", Name = "x" };
            var b = new FolderRow { Path = @"C:\folder\x", Name = "X" };

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(b.Equals(a));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void FolderRow_DifferentPaths_ReturnsFalse()
        {
            var a = new FolderRow { Path = @"C:\A\B", Name = "B" };
            var b = new FolderRow { Path = @"C:\A\C", Name = "B" };

            Assert.IsFalse(a.Equals(b));
        }

        [TestMethod]
        public void FolderRow_Null_ReturnsFalse()
        {
            Assert.IsFalse(new FolderRow { Path = @"C:\A" }.Equals(null));
        }

        [TestMethod]
        public void FileRow_EqualPathsIgnoreCase_TreatedAsEqual()
        {
            var a = new FileRow { Path = @"C:\dir\file.txt" };
            var b = new FileRow { Path = @"C:\DIR\FILE.TXT" };

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(b.Equals(a));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void FileRow_DifferentPaths_ReturnsFalse()
        {
            Assert.IsFalse(new FileRow { Path = @"C:\dir\a.txt" }.Equals(new FileRow { Path = @"C:\dir\b.txt" }));
        }
    }

    [TestClass]
    public class NameComparerTests
    {
        [TestMethod]
        public void CompareWithoutExtension_IgnoresCase()
        {
            Assert.AreEqual(0, NameComparer.CompareWithoutExtension("File001", "file001"));
            Assert.AreEqual(0, NameComparer.CompareWithoutExtension("PHOTO", "photo"));
        }

        [TestMethod]
        public void CompareWithoutExtension_ComparesNumbersByValue()
        {
            Assert.AreEqual(-1, NameComparer.CompareWithoutExtension("File1", "File10"));
            Assert.AreEqual(-1, NameComparer.CompareWithoutExtension("2", "10"));
            Assert.AreEqual(0, NameComparer.CompareWithoutExtension("100", "100"));
        }

        [TestMethod]
        public void CompareWithoutExtension_NullsSortFirst()
        {
            Assert.AreEqual(0, NameComparer.CompareWithoutExtension(null, null));
            Assert.AreEqual(-1, NameComparer.CompareWithoutExtension(null, "a"));
            Assert.AreEqual(1, NameComparer.CompareWithoutExtension("a", null));
        }

        [TestMethod]
        public void CompareWithoutExtension_OverlongNumbers_FallsBackToStringCompare()
        {
            // 20 цифр не помещается в long: раньше long.Parse бросал OverflowException.
            int result = NameComparer.CompareWithoutExtension("99999999999999999999", "10000000000000000000");

            // Оба числа не разобрались — сравнение строковое (Ordinal): "1..." < "9...".
            Assert.IsTrue(result > 0, "Без числового сравнения имена сортируются как строки (Ordinal).");
        }

        [TestMethod]
        public void CompareWithoutExtension_NumbersNearLongLimit_DoNotThrow()
        {
            // long.MaxValue и long.MaxValue + 1: одно число парсится, другое нет — сравнение строковое.
            int result = NameComparer.CompareWithoutExtension("9223372036854775807", "9223372036854775808");

            Assert.AreEqual(-1, result);
        }
    }
}
