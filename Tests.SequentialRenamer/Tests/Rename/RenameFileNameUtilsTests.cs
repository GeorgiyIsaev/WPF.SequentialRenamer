using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPF.SequentialRenamer.Services.Rename;

namespace Tests.SequentialRenamer.Rename
{
    [TestClass]
    public class RenameFileNameUtilsTests
    {
        [DataTestMethod]
        [DataRow("", "", 3, 1L, "", "001")]
        [DataRow("File", "", 3, 1L, "", "File001")]
        [DataRow("", "_end", 2, 5L, "", "05_end")]
        [DataRow("P", "S", 1, 1L, "", "P1S")]
        [DataRow("P", "", 2, 100L, "", "P100")]
        [DataRow("P", "", 3, 0L, "", "P000")]
        public void BuildFileName_WithoutExtension_FormatsCounter(
            string prefix, string suffix, int digits, long counter, string extension, string expected)
        {
            Assert.AreEqual(expected, RenameFileNameUtils.BuildFileName(prefix, suffix, digits, counter, extension));
        }

        [TestMethod]
        public void BuildFileName_ImplicitExtension_ShouldNotDoubleDot()
        {
            Assert.AreEqual("File001.jpg", RenameFileNameUtils.BuildFileName("File", "", 3, 1, ".jpg"));
        }

        [TestMethod]
        public void BuildFileName_NoDotExtension_ShouldAddDot()
        {
            Assert.AreEqual("File001.jpg", RenameFileNameUtils.BuildFileName("File", "", 3, 1, "jpg"));
        }

        [TestMethod]
        public void BuildFileName_NullPrefixAndSuffix_AreTreatedAsEmpty()
        {
            Assert.AreEqual("001", RenameFileNameUtils.BuildFileName(null, null, 3, 1, null));
        }

        [TestMethod]
        public void BuildFileName_PreservesCaseOfPrefixAndSuffix()
        {
            Assert.AreEqual("AbC001dEf.txt", RenameFileNameUtils.BuildFileName("AbC", "dEf", 3, 1, ".txt"));
        }

        [DataTestMethod]
        [DataRow(".jpg", ".jpg")]
        [DataRow("jpg", ".jpg")]
        [DataRow("", "")]
        [DataRow(".tar.gz", ".tar.gz")]
        public void NormalizeExtension_ProducesLeadingDot(string extension, string expected)
        {
            Assert.AreEqual(expected, RenameFileNameUtils.NormalizeExtension(extension));
        }

        [TestMethod]
        public void ValidateForbiddenCharacters_NullValues_DoNotThrow()
        {
            RenameFileNameUtils.ValidateForbiddenCharacters(null, null);
        }

        [TestMethod]
        public void ValidateForbiddenCharacters_DotAndSpace_AreAllowed()
        {
            RenameFileNameUtils.ValidateForbiddenCharacters("v1.0 final", " (copy)");
        }

        [TestMethod]
        public void FindFinalNameProblem_ValidName_ReturnsNull()
        {
            Assert.IsNull(RenameFileNameUtils.FindFinalNameProblem("01_001.jpg"));
            Assert.IsNull(RenameFileNameUtils.FindFinalNameProblem("File001"));
            Assert.IsNull(RenameFileNameUtils.FindFinalNameProblem(".gitignore"));
            Assert.IsNull(RenameFileNameUtils.FindFinalNameProblem("CON_001.txt"));
            Assert.IsNull(RenameFileNameUtils.FindFinalNameProblem("COM10.txt"));
            Assert.IsNull(RenameFileNameUtils.FindFinalNameProblem("v1.0 final (copy).txt"));
        }

        [TestMethod]
        public void FindFinalNameProblem_TrailingDotOrSpace_ReportsStripping()
        {
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("001.jpg."));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("001 "));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("001.\t"));
        }

        [TestMethod]
        public void FindFinalNameProblem_OnlyDotsAndSpaces_ReportsFullStrip()
        {
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("."));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem(" . "));
        }

        [TestMethod]
        public void FindFinalNameProblem_ReservedDeviceNames_ReportsProblem()
        {
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("COM1"));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("com1"));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("CON.txt"));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("LPT9.tar.gz"));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("NUL "));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("AUX."));
        }

        [TestMethod]
        public void FindFinalNameProblem_EmptyOrInvalid_ReturnsProblem()
        {
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem(""));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem(null));
            Assert.IsNotNull(RenameFileNameUtils.FindFinalNameProblem("bad<name"));
        }
    }
}
