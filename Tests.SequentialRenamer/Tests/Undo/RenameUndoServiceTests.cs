using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.SequentialRenamer.Common;
using WPF.SequentialRenamer.Services.Rename;
using WPF.SequentialRenamer.Services.Undo;

namespace Tests.SequentialRenamer.Undo
{
    [TestClass]
    public class RenameUndoServiceTests
    {
        private readonly RenameUndoService _undo = new RenameUndoService();
        private readonly RenameBatchRunner _runner = new RenameBatchRunner();
        private readonly RenamePlanner _planner = new RenamePlanner();

        [TestMethod]
        public void Execute_AfterRename_RestoresOriginalNameAndContent()
        {
            using (var dir = new TempDirectory())
            {
                string original = dir.CreateFile("a.txt", "A");
                string renamed = Path.Combine(dir.Path, "File001.txt");
                RenameBatchResult rename = _runner.Run(
                    new List<RenameItem> { new RenameItem { OldPath = original, NewPath = renamed } },
                    isFolders: false);

                var entry = new UndoEntry { IsFolders = false };
                entry.Add(_planner.BuildUndoItems(rename.Done));
                RenameUndoResult undo = _undo.Execute(entry);

                Assert.IsFalse(undo.HasErrors);
                Assert.AreEqual(1, undo.Done.Count);
                CollectionAssert.AreEqual(new[] { "a.txt" }, dir.GetNames());
                Assert.AreEqual("A", File.ReadAllText(original));
            }
        }

        [TestMethod]
        public void Execute_TwiceThroughLog_RestoresOriginalAfterTwoRenames()
        {
            using (var dir = new TempDirectory())
            {
                string original = dir.CreateFile("a.txt", "A");
                string middle = Path.Combine(dir.Path, "b.txt");
                string last = Path.Combine(dir.Path, "c.txt");

                var log = new RenameUndoLog();

                RenameBatchResult firstRename = _runner.Run(
                    new List<RenameItem> { new RenameItem { OldPath = original, NewPath = middle } },
                    isFolders: false);
                var firstEntry = new UndoEntry { IsFolders = false };
                firstEntry.Add(_planner.BuildUndoItems(firstRename.Done));
                log.Push(firstEntry);

                RenameBatchResult secondRename = _runner.Run(
                    new List<RenameItem> { new RenameItem { OldPath = middle, NewPath = last } },
                    isFolders: false);
                var secondEntry = new UndoEntry { IsFolders = false };
                secondEntry.Add(_planner.BuildUndoItems(secondRename.Done));
                log.Push(secondEntry);

                Assert.IsFalse(_undo.Execute(log.Pop()).HasErrors);
                CollectionAssert.AreEqual(new[] { "b.txt" }, dir.GetNames());

                Assert.IsFalse(_undo.Execute(log.Pop()).HasErrors);
                CollectionAssert.AreEqual(new[] { "a.txt" }, dir.GetNames());
                Assert.AreEqual("A", File.ReadAllText(original));
                Assert.IsFalse(log.HasEntries);
            }
        }

        [TestMethod]
        public void Execute_CaseOnlyRestore_RestoresOriginalLetterCase()
        {
            using (var dir = new TempDirectory())
            {
                string upper = dir.CreateFile("ABC.txt", "A");
                var entry = new UndoEntry { IsFolders = false };
                entry.Add(new List<UndoItem>
                {
                    new UndoItem { CurrentPath = upper, OriginalPath = Path.Combine(dir.Path, "abc.txt") }
                });

                RenameUndoResult result = _undo.Execute(entry);

                Assert.IsFalse(result.HasErrors);
                CollectionAssert.AreEqual(new[] { "abc.txt" }, dir.GetNames());
                Assert.AreEqual("A", File.ReadAllText(Path.Combine(dir.Path, "abc.txt")));
            }
        }

        [TestMethod]
        public void Execute_TargetAlreadyExists_ReportsErrorWithoutThrowing()
        {
            using (var dir = new TempDirectory())
            {
                string current = dir.CreateFile("new.txt", "new");
                string original = dir.CreateFile("old.txt", "old");
                var entry = new UndoEntry { IsFolders = false };
                entry.Add(new List<UndoItem>
                {
                    new UndoItem { CurrentPath = current, OriginalPath = original }
                });

                RenameUndoResult result = _undo.Execute(entry);

                Assert.IsTrue(result.HasErrors);
                Assert.AreEqual(0, result.Done.Count);
                CollectionAssert.AreEqual(new[] { "new.txt", "old.txt" }, dir.GetNames());
                Assert.AreEqual("old", File.ReadAllText(original));
            }
        }

        [TestMethod]
        public void Execute_Folder_RestoresFolderName()
        {
            using (var dir = new TempDirectory())
            {
                string renamed = dir.CreateFolder("Folder001");
                string original = Path.Combine(dir.Path, "one");
                var entry = new UndoEntry { IsFolders = true };
                entry.Add(new List<UndoItem>
                {
                    new UndoItem { CurrentPath = renamed, OriginalPath = original }
                });

                RenameUndoResult result = _undo.Execute(entry);

                Assert.IsFalse(result.HasErrors);
                Assert.IsTrue(Directory.Exists(original));
                CollectionAssert.AreEqual(new[] { "one" }, dir.GetNames());
            }
        }

        [TestMethod]
        public void Execute_MissingCurrentPath_ReportsErrorAndContinues()
        {
            using (var dir = new TempDirectory())
            {
                string existing = dir.CreateFile("new.txt", "new");
                var entry = new UndoEntry { IsFolders = false };
                entry.Add(new List<UndoItem>
                {
                    new UndoItem { CurrentPath = Path.Combine(dir.Path, "gone.txt"), OriginalPath = Path.Combine(dir.Path, "x.txt") },
                    new UndoItem { CurrentPath = existing, OriginalPath = Path.Combine(dir.Path, "old.txt") }
                });

                RenameUndoResult result = _undo.Execute(entry);

                Assert.AreEqual(1, result.Errors.Count);
                Assert.AreEqual(1, result.Done.Count);
                Assert.IsTrue(File.Exists(Path.Combine(dir.Path, "old.txt")));
            }
        }

        [TestMethod]
        public void Execute_NullEntry_ReturnsEmptyResult()
        {
            RenameUndoResult result = _undo.Execute(null);

            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual(0, result.Done.Count);
        }
    }
}
