using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.SequentialRenamer.Common;
using WPF.SequentialRenamer.Services.Rename;

namespace Tests.SequentialRenamer.Rename
{
    [TestClass]
    public class RenameBatchRunnerTests
    {
        private readonly RenameBatchRunner _runner = new RenameBatchRunner();

        [TestMethod]
        public void Run_RenamesFiles_AndKeepsContent()
        {
            using (var dir = new TempDirectory())
            {
                string first = dir.CreateFile("a.txt", "A");
                string second = dir.CreateFile("b.txt", "B");

                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = first, NewPath = Path.Combine(dir.Path, "File001.txt") },
                    new RenameItem { OldPath = second, NewPath = Path.Combine(dir.Path, "File002.txt") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: false);

                Assert.IsFalse(result.HasErrors);
                Assert.AreEqual(2, result.Done.Count);
                CollectionAssert.AreEqual(new[] { "File001.txt", "File002.txt" }, dir.GetNames());
                Assert.AreEqual("A", File.ReadAllText(Path.Combine(dir.Path, "File001.txt")));
                Assert.AreEqual("B", File.ReadAllText(Path.Combine(dir.Path, "File002.txt")));
            }
        }

        [TestMethod]
        public void Run_CaseOnlyRename_ChangesNameCase()
        {
            using (var dir = new TempDirectory())
            {
                string source = dir.CreateFile("abc.txt", "A");
                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = source, NewPath = Path.Combine(dir.Path, "ABC.txt") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: false);

                Assert.IsFalse(result.HasErrors);
                CollectionAssert.AreEqual(new[] { "ABC.txt" }, dir.GetNames());
                Assert.AreEqual("A", File.ReadAllText(Path.Combine(dir.Path, "ABC.txt")));
            }
        }

        [TestMethod]
        public void Run_SwappedNames_DoesNotLoseFiles()
        {
            using (var dir = new TempDirectory())
            {
                string a = dir.CreateFile("a.txt", "A");
                string b = dir.CreateFile("b.txt", "B");
                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = a, NewPath = Path.Combine(dir.Path, "b.txt") },
                    new RenameItem { OldPath = b, NewPath = Path.Combine(dir.Path, "a.txt") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: false);

                Assert.IsFalse(result.HasErrors);
                Assert.AreEqual("B", File.ReadAllText(Path.Combine(dir.Path, "a.txt")));
                Assert.AreEqual("A", File.ReadAllText(Path.Combine(dir.Path, "b.txt")));
            }
        }

        [TestMethod]
        public void Run_MissingSource_ReportsErrorButRenamesOtherFiles()
        {
            using (var dir = new TempDirectory())
            {
                string real = dir.CreateFile("a.txt", "A");
                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = Path.Combine(dir.Path, "missing.txt"), NewPath = Path.Combine(dir.Path, "File001.txt") },
                    new RenameItem { OldPath = real, NewPath = Path.Combine(dir.Path, "File002.txt") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: false);

                Assert.IsTrue(result.HasErrors);
                Assert.AreEqual(1, result.Errors.Count);
                Assert.AreEqual(1, result.Done.Count);
                CollectionAssert.AreEqual(new[] { "File002.txt" }, dir.GetNames());
            }
        }

        [TestMethod]
        public void Run_DuplicateTargetNames_ReportsErrorInsteadOfOverwriting()
        {
            using (var dir = new TempDirectory())
            {
                string first = dir.CreateFile("a.txt", "A");
                string second = dir.CreateFile("b.txt", "B");
                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = first, NewPath = Path.Combine(dir.Path, "Same.txt") },
                    new RenameItem { OldPath = second, NewPath = Path.Combine(dir.Path, "Same.txt") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: false);

                Assert.IsTrue(result.HasErrors);
                Assert.AreEqual(1, result.Errors.Count);
                Assert.AreEqual(1, result.Done.Count);

                // Второй элемент не смог занять имя и вернулся на исходное место.
                CollectionAssert.AreEqual(new[] { "b.txt", "Same.txt" }, dir.GetNames());
                Assert.AreEqual("A", File.ReadAllText(Path.Combine(dir.Path, "Same.txt")));
                Assert.AreEqual("B", File.ReadAllText(Path.Combine(dir.Path, "b.txt")));
                Assert.AreEqual(0, CountTempEntries(dir), "Временные имена не должны оставаться в папке.");
            }
        }

        [TestMethod]
        public void Run_TargetAlreadyExists_RollsBackToOriginalName()
        {
            using (var dir = new TempDirectory())
            {
                string source = dir.CreateFile("a.txt", "A");
                dir.CreateFile("taken.txt", "T");
                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = source, NewPath = Path.Combine(dir.Path, "taken.txt") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: false);

                Assert.IsTrue(result.HasErrors);
                Assert.AreEqual(1, result.Errors.Count);
                Assert.AreEqual(0, result.Done.Count);
                CollectionAssert.AreEqual(new[] { "a.txt", "taken.txt" }, dir.GetNames());
                Assert.AreEqual("A", File.ReadAllText(source));
                Assert.AreEqual("T", File.ReadAllText(Path.Combine(dir.Path, "taken.txt")));
                Assert.AreEqual(0, CountTempEntries(dir), "После отката временное имя должно исчезнуть.");
            }
        }

        [TestMethod]
        public void Run_OneItemFails_OtherItemsKeepTheirNewNames()
        {
            using (var dir = new TempDirectory())
            {
                string first = dir.CreateFile("a.txt", "A");
                string second = dir.CreateFile("b.txt", "B");
                dir.CreateFile("taken.txt", "T");
                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = first, NewPath = Path.Combine(dir.Path, "File001.txt") },
                    new RenameItem { OldPath = second, NewPath = Path.Combine(dir.Path, "taken.txt") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: false);

                Assert.AreEqual(1, result.Errors.Count);
                Assert.AreEqual(1, result.Done.Count);
                CollectionAssert.AreEqual(new[] { "b.txt", "File001.txt", "taken.txt" }, dir.GetNames());
                Assert.AreEqual("A", File.ReadAllText(Path.Combine(dir.Path, "File001.txt")));
                Assert.AreEqual("B", File.ReadAllText(Path.Combine(dir.Path, "b.txt")));
                Assert.AreEqual("T", File.ReadAllText(Path.Combine(dir.Path, "taken.txt")));
                Assert.AreEqual(0, CountTempEntries(dir));
            }
        }

        [TestMethod]
        public void Run_FolderTargetAlreadyExists_RollsBackOriginalFolder()
        {
            using (var dir = new TempDirectory())
            {
                string folder = dir.CreateFolder("one");
                File.WriteAllText(Path.Combine(folder, "inner.txt"), "inner");
                dir.CreateFolder("taken");
                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = folder, NewPath = Path.Combine(dir.Path, "taken") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: true);

                Assert.IsTrue(result.HasErrors);
                Assert.AreEqual(0, result.Done.Count);
                Assert.IsTrue(Directory.Exists(folder));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir.Path, "taken")));
                Assert.AreEqual("inner", File.ReadAllText(Path.Combine(folder, "inner.txt")));
                Assert.AreEqual(0, CountTempEntries(dir), "Временная папка не должна оставаться на диске.");
            }
        }

        private static int CountTempEntries(TempDirectory dir)
        {
            return Directory.GetFileSystemEntries(dir.Path, "sqr_tmp_*").Length;
        }

        [TestMethod]
        public void Run_RenamesFolders_AndKeepsContents()
        {
            using (var dir = new TempDirectory())
            {
                string folder = dir.CreateFolder("one");
                File.WriteAllText(Path.Combine(folder, "inner.txt"), "inner");

                var items = new List<RenameItem>
                {
                    new RenameItem { OldPath = folder, NewPath = Path.Combine(dir.Path, "Folder001") }
                };

                RenameBatchResult result = _runner.Run(items, isFolders: true);

                Assert.IsFalse(result.HasErrors);
                string renamed = Path.Combine(dir.Path, "Folder001");
                Assert.IsTrue(Directory.Exists(renamed));
                Assert.AreEqual("inner", File.ReadAllText(Path.Combine(renamed, "inner.txt")));
            }
        }

        [TestMethod]
        public void Run_NullItems_ReturnsEmptyResult()
        {
            RenameBatchResult result = _runner.Run(null, isFolders: false);

            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual(0, result.Done.Count);
            Assert.IsTrue(result.IsEmpty);
        }
    }
}
