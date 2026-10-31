using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPF.SequentialRenamer.Services.Rename;

namespace Tests.SequentialRenamer.Rename
{
    [TestClass]
    public class RenameConflictCheckerTests
    {
        private static RenameItem Item(string directory, string oldName, string newName)
        {
            return new RenameItem
            {
                OldPath = Path.Combine(directory, oldName),
                NewPath = Path.Combine(directory, newName)
            };
        }

        private static Func<string, IReadOnlyList<string>> Entries(params string[] names)
        {
            return _ => names;
        }

        [TestMethod]
        public void FindExistingConflicts_NewNameNotOnDisk_ReturnsEmpty()
        {
            var items = new[] { Item(@"C:\work", "a.jpg", "File001.jpg") };

            IReadOnlyList<string> conflicts = RenameConflictChecker.FindExistingConflicts(
                items,
                Entries("a.jpg", "b.jpg"));

            Assert.AreEqual(0, conflicts.Count);
        }

        [TestMethod]
        public void FindExistingConflicts_NewNameAlreadyOnDisk_ReportsConflict()
        {
            var items = new[] { Item(@"C:\work", "a.jpg", "File001.jpg") };

            IReadOnlyList<string> conflicts = RenameConflictChecker.FindExistingConflicts(
                items,
                Entries("a.jpg", "File001.jpg"));

            Assert.AreEqual(1, conflicts.Count);
            Assert.AreEqual("File001.jpg", conflicts[0]);
        }

        [TestMethod]
        public void FindExistingConflicts_ExistingNameDiffersOnlyByCase_ReportsConflict()
        {
            var items = new[] { Item(@"C:\work", "a.jpg", "File001.JPG") };

            IReadOnlyList<string> conflicts = RenameConflictChecker.FindExistingConflicts(
                items,
                Entries("file001.jpg", "a.jpg"));

            Assert.AreEqual(1, conflicts.Count);
            Assert.AreEqual("File001.JPG", conflicts[0]);
        }

        [TestMethod]
        public void FindExistingConflicts_NameOccupiedOnlyByItemFromBatch_IsNotConflict()
        {
            // Обмен именами: a.jpg -> b.jpg и b.jpg -> a.jpg.
            var items = new[]
            {
                Item(@"C:\work", "a.jpg", "b.jpg"),
                Item(@"C:\work", "b.jpg", "a.jpg")
            };

            IReadOnlyList<string> conflicts = RenameConflictChecker.FindExistingConflicts(
                items,
                Entries("a.jpg", "b.jpg"));

            Assert.AreEqual(0, conflicts.Count);
        }

        [TestMethod]
        public void FindExistingConflicts_SameNameInAnotherFolder_IsNotReportedOnceGlobally()
        {
            var items = new[]
            {
                Item(@"C:\one", "a.jpg", "File001.jpg"),
                Item(@"C:\two", "a.jpg", "File001.jpg")
            };

            IReadOnlyList<string> conflicts = RenameConflictChecker.FindExistingConflicts(
                items,
                directory => directory == @"C:\one"
                    ? new[] { "File001.jpg", "a.jpg" }
                    : new List<string> { "a.jpg" });

            Assert.AreEqual(1, conflicts.Count);
        }

        [TestMethod]
        public void FindExistingConflicts_NullItems_ReturnsEmpty()
        {
            Assert.AreEqual(0, RenameConflictChecker.FindExistingConflicts(null, Entries()).Count);
        }
    }
}
