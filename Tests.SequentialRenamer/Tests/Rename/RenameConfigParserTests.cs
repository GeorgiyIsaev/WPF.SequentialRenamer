using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPF.SequentialRenamer.Services.Rename;

namespace Tests.SequentialRenamer.Rename
{
    [TestClass]
    public class RenameConfigParserTests
    {
        private readonly RenameConfigParser _parser = new RenameConfigParser();

        [TestMethod]
        public void Parse_EmptyPrefixAndSuffix_ReturnsDefaults()
        {
            RenameConfigParseResult result = _parser.Parse(null, null, "3", "1");

            Assert.IsFalse(result.HasError);
            Assert.IsNotNull(result.Config);
            Assert.AreEqual(string.Empty, result.Config.Prefix);
            Assert.AreEqual(string.Empty, result.Config.Suffix);
            Assert.AreEqual(3, result.Config.Digits);
            Assert.AreEqual(1L, result.Config.Start);
        }

        [TestMethod]
        public void Parse_AllFieldsNull_DoesNotThrowAndUsesDefaults()
        {
            RenameConfigParseResult result = _parser.Parse(null, null, null, null);

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(string.Empty, result.Config.Prefix);
            Assert.AreEqual(string.Empty, result.Config.Suffix);
            Assert.AreEqual(3, result.Config.Digits);
            Assert.AreEqual(1L, result.Config.Start);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void Parse_EmptyDigitCount_DefaultsToThree(string digitsText)
        {
            RenameConfigParseResult result = _parser.Parse("P", "S", digitsText, "1");

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(3, result.Config.Digits);
        }

        [DataTestMethod]
        [DataRow("abc")]
        [DataRow("0")]
        [DataRow("-1")]
        [DataRow("19")]
        [DataRow("100")]
        public void Parse_InvalidDigitCount_ReturnsErrorWithoutConfig(string digitsText)
        {
            RenameConfigParseResult result = _parser.Parse("P", "S", digitsText, "1");

            Assert.IsTrue(result.HasError);
            Assert.IsNull(result.Config);
            StringAssert.Contains(result.Error, "от 1 до 18");
        }

        [DataTestMethod]
        [DataRow("1")]
        [DataRow("18")]
        [DataRow(" 4 ")]
        public void Parse_ValidDigitCount_IsKept(string digitsText)
        {
            RenameConfigParseResult result = _parser.Parse("P", "S", digitsText, "1");

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(int.Parse(digitsText.Trim()), result.Config.Digits);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void Parse_EmptyStart_DefaultsToOne(string startText)
        {
            RenameConfigParseResult result = _parser.Parse("P", "S", "3", startText);

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(1L, result.Config.Start);
        }

        [DataTestMethod]
        [DataRow("-5")]
        [DataRow("abc")]
        [DataRow("999999999999999999999999")]
        public void Parse_InvalidStart_ReturnsErrorWithoutConfig(string startText)
        {
            RenameConfigParseResult result = _parser.Parse("P", "S", "3", startText);

            Assert.IsTrue(result.HasError);
            Assert.IsNull(result.Config);
            StringAssert.Contains(result.Error, "Начальный номер");
        }

        [DataTestMethod]
        [DataRow("0")]
        [DataRow("7")]
        [DataRow(" 12 ")]
        public void Parse_ValidStart_IsKept(string startText)
        {
            RenameConfigParseResult result = _parser.Parse("P", "S", "3", startText);

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(long.Parse(startText.Trim()), result.Config.Start);
        }

        [DataTestMethod]
        [DataRow("*")]
        [DataRow("?")]
        [DataRow("\\")]
        [DataRow("/")]
        [DataRow(":")]
        [DataRow("<")]
        [DataRow(">")]
        [DataRow("|")]
        [DataRow("\"")]
        [DataRow("a*b")]
        public void Parse_ForbiddenCharInPrefix_ReturnsError(string prefix)
        {
            RenameConfigParseResult result = _parser.Parse(prefix, null, "3", "1");

            Assert.IsTrue(result.HasError);
            StringAssert.Contains(result.Error, "Префикс");
        }

        [DataTestMethod]
        [DataRow("*")]
        [DataRow("?")]
        [DataRow("\\")]
        [DataRow("/")]
        [DataRow(":")]
        [DataRow("bad*name")]
        public void Parse_ForbiddenCharInSuffix_ReturnsError(string suffix)
        {
            RenameConfigParseResult result = _parser.Parse(null, suffix, "3", "1");

            Assert.IsTrue(result.HasError);
            StringAssert.Contains(result.Error, "Суффикс");
        }

        [TestMethod]
        public void Parse_ForbiddenCharsFromRuntimeList_AreRejected()
        {
            var rejected = new List<char>();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                RenameConfigParseResult result = _parser.Parse(c.ToString(), null, "3", "1");
                if (!result.HasError)
                    rejected.Add(c);
            }

            Assert.AreEqual(0, rejected.Count, "Все запрещённые символы должны отклоняться: " + string.Join(", ", rejected));
        }

        [TestMethod]
        public void Parse_DotInPrefix_IsAllowed()
        {
            RenameConfigParseResult result = _parser.Parse("v1.0", null, "3", "1");

            Assert.IsFalse(result.HasError);
            Assert.AreEqual("v1.0", result.Config.Prefix);
        }

        [TestMethod]
        public void Parse_SpacesInPrefixAndSuffix_AreAllowed()
        {
            RenameConfigParseResult result = _parser.Parse("my ", " photos", "2", "1");

            Assert.IsFalse(result.HasError);
            Assert.AreEqual("my ", result.Config.Prefix);
            Assert.AreEqual(" photos", result.Config.Suffix);
        }
    }
}
