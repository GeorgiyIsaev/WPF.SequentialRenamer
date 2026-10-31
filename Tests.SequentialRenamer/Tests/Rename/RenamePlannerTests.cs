using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPF.SequentialRenamer.Models;
using WPF.SequentialRenamer.Services.Rename;
using WPF.SequentialRenamer.Services.Undo;

namespace Tests.SequentialRenamer.Rename
{
    [TestClass]
    public class RenamePlannerTests
    {
        private readonly RenamePlanner _planner = new RenamePlanner();

        [TestMethod]
        public void BuildRenameItemsForFolder_UsesSequentialNumbers()
        {
            string root = @"C:\root";
            var folders = new List<FolderRow>
            {
                new FolderRow { Name = "b", Path = Path.Combine(root, "b") },
                new FolderRow { Name = "a", Path = Path.Combine(root, "a") }
            };

            var items = _planner.BuildRenameItemsForFolder(
                folders,
                new RenameConfig("F", "", 2, 3),
                root);

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(Path.Combine(root, "b"), items[0].OldPath);
            Assert.AreEqual(Path.Combine(root, "F03"), items[0].NewPath);
            Assert.AreEqual(Path.Combine(root, "a"), items[1].OldPath);
            Assert.AreEqual(Path.Combine(root, "F04"), items[1].NewPath);
        }

        [TestMethod]
        public void BuildRenameItemsForFiles_PreservesEachExtension()
        {
            string dir = @"C:\photos";
            var files = new List<FileRow>
            {
                new FileRow { Path = Path.Combine(dir, "a.jpg"), Extension = ".jpg" },
                new FileRow { Path = Path.Combine(dir, "b.PNG"), Extension = "PNG" },
                new FileRow { Path = Path.Combine(dir, "c"), Extension = "" }
            };

            var items = _planner.BuildRenameItemsForFiles(
                files,
                new RenameConfig("P", "S", 3, 0),
                dir);

            Assert.AreEqual(Path.Combine(dir, "P000S.jpg"), items[0].NewPath);
            Assert.AreEqual(Path.Combine(dir, "P001S.PNG"), items[1].NewPath);
            Assert.AreEqual(Path.Combine(dir, "P002S"), items[2].NewPath);
        }

        [TestMethod]
        public void BuildRenameItemsForFolder_NullList_ReturnsEmpty()
        {
            Assert.AreEqual(0, _planner.BuildRenameItemsForFolder(null, new RenameConfig("P", "", 3, 1), @"C:\root").Count);
        }

        [TestMethod]
        public void BuildRenameItemsForFiles_NullList_ReturnsEmpty()
        {
            Assert.AreEqual(0, _planner.BuildRenameItemsForFiles(null, new RenameConfig("P", "", 3, 1), @"C:\root").Count);
        }

        [TestMethod]
        public void BuildUndoItems_MapsNewPathBackToOldPath()
        {
            var renamed = new List<RenameItem>
            {
                new RenameItem { OldPath = @"C:\a\one", NewPath = @"C:\a\File001" },
                new RenameItem { OldPath = @"C:\a\two", NewPath = @"C:\a\File002" }
            };

            List<UndoItem> undoItems = _planner.BuildUndoItems(renamed);

            Assert.AreEqual(2, undoItems.Count);
            Assert.AreEqual(@"C:\a\File001", undoItems[0].CurrentPath);
            Assert.AreEqual(@"C:\a\one", undoItems[0].OriginalPath);
            Assert.AreEqual(@"C:\a\File002", undoItems[1].CurrentPath);
            Assert.AreEqual(@"C:\a\two", undoItems[1].OriginalPath);
        }

        [TestMethod]
        public void FindFinalNameProblems_ValidPlan_ReturnsEmpty()
        {
            var items = new List<RenameItem>
            {
                new RenameItem { OldPath = @"C:\a\one", NewPath = @"C:\a\01_001" },
                new RenameItem { OldPath = @"C:\a\two", NewPath = @"C:\a\01_002" }
            };

            Assert.AreEqual(0, RenamePlanner.FindFinalNameProblems(items).Count);
        }

        [TestMethod]
        public void FindFinalNameProblems_TrailingDotSuffix_ReportsEachItem()
        {
            // Суффикс «.» и пустое расширение: имя папки заканчивается точкой, Windows её отбросит.
            var items = new List<RenameItem>
            {
                new RenameItem { OldPath = @"C:\a\one", NewPath = @"C:\a\001." },
                new RenameItem { OldPath = @"C:\a\two", NewPath = @"C:\a\002." }
            };

            List<string> problems = RenamePlanner.FindFinalNameProblems(items);

            Assert.AreEqual(2, problems.Count);
        }

        [TestMethod]
        public void FindFinalNameProblems_DeviceNameFromPrefixAndCounter_ReportsProblem()
        {
            // Префикс COM, счётчик 1, одна цифра: итоговое имя — COM1, зарезервированное устройство.
            var items = new List<RenameItem>
            {
                new RenameItem { OldPath = @"C:\a\one", NewPath = @"C:\a\COM1" },
                new RenameItem { OldPath = @"C:\a\two", NewPath = @"C:\a\COM2" }
            };

            List<string> problems = RenamePlanner.FindFinalNameProblems(items);

            Assert.AreEqual(2, problems.Count);
            StringAssert.Contains(problems[0], "зарезервированное имя устройства");
        }

        [TestMethod]
        public void FindFinalNameProblems_NullAndSkippedItems_DoNotThrow()
        {
            Assert.AreEqual(0, RenamePlanner.FindFinalNameProblems(null).Count);
            Assert.AreEqual(0, RenamePlanner.FindFinalNameProblems(
                new List<RenameItem> { null, new RenameItem { OldPath = @"C:\a\x" } }).Count);
        }
    }
}
