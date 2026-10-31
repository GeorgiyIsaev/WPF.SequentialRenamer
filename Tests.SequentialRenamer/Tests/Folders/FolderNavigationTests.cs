using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.SequentialRenamer.Common;
using WPF.SequentialRenamer.Models;
using WPF.SequentialRenamer.Services.Folders;

namespace Tests.SequentialRenamer.Folders
{
    [TestClass]
    public class FolderNavigationTests
    {
        private static FolderNavigation CreateNavigator()
        {
            return new FolderNavigation();
        }

        [TestMethod]
        public void NavigateTo_FirstVisit_ReturnsFolders()
        {
            using (var dir = new TempDirectory())
            {
                dir.CreateFile("a.txt");
                dir.CreateFolder("sub");
                FolderNavigation navigator = CreateNavigator();

                FolderNavigateResult result = navigator.NavigateTo(dir.Path);

                Assert.IsTrue(result.Success);
                Assert.AreEqual(Path.GetFullPath(dir.Path), result.State.Path);
                Assert.AreEqual(1, result.State.Folders.Count, "Подпапки должны читаться при первом переходе.");
                Assert.AreEqual("sub", result.State.Folders[0].Name);
            }
        }

        [TestMethod]
        public void NavigateTo_MissingPath_Fails()
        {
            FolderNavigateResult result = CreateNavigator().NavigateTo(Path.Combine(Path.GetTempPath(), "sqr_missing_" + System.Guid.NewGuid().ToString("N")));

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.State);
            StringAssert.Contains(result.Error, "не существует");
        }

        [TestMethod]
        public void NavigateTo_EmptyPath_Fails()
        {
            FolderNavigateResult result = CreateNavigator().NavigateTo("");

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Error, "не указан");
        }

        [TestMethod]
        public void NavigateTo_SecondVisit_ReadsFoldersFromDiskAgain()
        {
            using (var dir = new TempDirectory())
            {
                dir.CreateFolder("sub");
                FolderNavigation navigator = CreateNavigator();
                navigator.NavigateTo(dir.Path);

                FolderNavigateResult second = navigator.NavigateTo(dir.Path);

                Assert.IsTrue(second.Success);
                Assert.AreEqual(1, second.State.Folders.Count);
            }
        }

        [TestMethod]
        public void LoadFiles_AlwaysFreshAfterExternalChange()
        {
            using (var dir = new TempDirectory())
            {
                dir.CreateFile("a.txt");
                FolderNavigation navigator = CreateNavigator();

                Assert.AreEqual(1, navigator.LoadFiles(dir.Path).Count);

                dir.CreateFile("b.txt");
                Assert.AreEqual(2, navigator.LoadFiles(dir.Path).Count, "Файлы должны перечитываться с диска, а не браться из кэша.");
            }
        }

        [TestMethod]
        public void LoadFiles_SubfolderNotVisited_ReturnsFilesWithoutThrowing()
        {
            using (var dir = new TempDirectory())
            {
                string sub = dir.CreateFolder("sub");
                File.WriteAllText(Path.Combine(sub, "a.txt"), "A");
                File.WriteAllText(Path.Combine(sub, "b.jpg"), "B");
                FolderNavigation navigator = CreateNavigator();

                var files = navigator.LoadFiles(sub);

                Assert.AreEqual(2, files.Count);
                Assert.AreEqual(1, files.Count(file => file.Extension == ".jpg"));
            }
        }

        [TestMethod]
        public void TryFindAccessibleFolder_PathExists_ReturnsSamePath()
        {
            using (var dir = new TempDirectory())
            {
                string sub = dir.CreateFolder("sub");

                Assert.IsTrue(CreateNavigator().TryFindAccessibleFolder(sub, out string accessible));
                Assert.AreEqual(Path.GetFullPath(sub), accessible);
            }
        }

        [TestMethod]
        public void TryFindAccessibleFolder_MissingChild_ReturnsNearestExistingParent()
        {
            using (var dir = new TempDirectory())
            {
                string sub = dir.CreateFolder("sub");
                string missing = Path.Combine(sub, "gone", "deeper");

                Assert.IsTrue(CreateNavigator().TryFindAccessibleFolder(missing, out string accessible));
                Assert.AreEqual(Path.GetFullPath(sub), accessible);
            }
        }

        [TestMethod]
        public void TryFindAccessibleFolder_AllLevelsMissing_ClimbsToFirstExisting()
        {
            using (var dir = new TempDirectory())
            {
                string missing = Path.Combine(dir.Path, "a", "b", "c");

                Assert.IsTrue(CreateNavigator().TryFindAccessibleFolder(missing, out string accessible));
                Assert.AreEqual(Path.GetFullPath(dir.Path), accessible);
            }
        }

        [TestMethod]
        public void TryFindAccessibleFolder_EmptyPath_ReturnsFalse()
        {
            Assert.IsFalse(CreateNavigator().TryFindAccessibleFolder("", out _));
        }

        [TestMethod]
        public void TryGetParentPath_Subfolder_ReturnsParent()
        {
            using (var dir = new TempDirectory())
            {
                string sub = dir.CreateFolder("sub");
                FolderNavigation navigator = CreateNavigator();

                Assert.IsTrue(navigator.TryGetParentPath(sub, out string parent));
                Assert.AreEqual(Path.GetFullPath(dir.Path), parent);
            }
        }

        [TestMethod]
        public void TryGetParentPath_EmptyPath_ReturnsFalse()
        {
            Assert.IsFalse(CreateNavigator().TryGetParentPath("", out _));
        }

        [TestMethod]
        public void IsFolderListActual_SameNames_ReturnsTrue()
        {
            using (var dir = new TempDirectory())
            {
                dir.CreateFolder("a");
                dir.CreateFolder("b");
                var displayed = new List<FolderRow>
                {
                    new FolderRow { Name = "a", Path = Path.Combine(dir.Path, "a") },
                    new FolderRow { Name = "b", Path = Path.Combine(dir.Path, "b") }
                };

                Assert.IsTrue(CreateNavigator().IsFolderListActual(dir.Path, displayed));
            }
        }

        [TestMethod]
        public void IsFolderListActual_FolderDeletedExternally_ReturnsFalse()
        {
            using (var dir = new TempDirectory())
            {
                string sub = dir.CreateFolder("sub");
                var displayed = new List<FolderRow>
                {
                    new FolderRow { Name = "sub", Path = sub }
                };

                Directory.Delete(sub);

                Assert.IsFalse(CreateNavigator().IsFolderListActual(dir.Path, displayed));
            }
        }

        [TestMethod]
        public void IsFolderListActual_FolderRenamedExternally_ReturnsFalse()
        {
            using (var dir = new TempDirectory())
            {
                string sub = dir.CreateFolder("sub");
                var displayed = new List<FolderRow>
                {
                    new FolderRow { Name = "sub", Path = sub }
                };

                Directory.Move(sub, Path.Combine(dir.Path, "renamed"));

                Assert.IsFalse(CreateNavigator().IsFolderListActual(dir.Path, displayed));
            }
        }

        [TestMethod]
        public void IsFolderListActual_FolderAddedExternally_ReturnsFalse()
        {
            using (var dir = new TempDirectory())
            {
                dir.CreateFolder("sub");
                var displayed = new List<FolderRow>
                {
                    new FolderRow { Name = "sub", Path = Path.Combine(dir.Path, "sub") }
                };

                dir.CreateFolder("extra");

                Assert.IsFalse(CreateNavigator().IsFolderListActual(dir.Path, displayed));
            }
        }

        [TestMethod]
        public void FolderListsMatch_DifferentOrder_ReturnsTrue()
        {
            var first = new List<FolderRow> { new FolderRow { Name = "a" }, new FolderRow { Name = "b" } };
            var second = new List<FolderRow> { new FolderRow { Name = "b" }, new FolderRow { Name = "a" } };

            Assert.IsTrue(FolderNavigation.FolderListsMatch(first, second));
        }

        [TestMethod]
        public void FolderListsMatch_DifferentCount_ReturnsFalse()
        {
            var first = new List<FolderRow> { new FolderRow { Name = "a" } };
            var second = new List<FolderRow>();

            Assert.IsFalse(FolderNavigation.FolderListsMatch(first, second));
        }

        [TestMethod]
        public void FolderListsMatch_CaseOnlyRename_ReturnsFalse()
        {
            var first = new List<FolderRow> { new FolderRow { Name = "Sub" } };
            var second = new List<FolderRow> { new FolderRow { Name = "sub" } };

            Assert.IsFalse(FolderNavigation.FolderListsMatch(first, second));
        }

        [TestMethod]
        public void LoadFoldersViaEnumerator_ReportsCounts()
        {
            using (var dir = new TempDirectory())
            {
                string sub = dir.CreateFolder("sub");
                File.WriteAllText(Path.Combine(sub, "a.txt"), "A");
                File.WriteAllText(Path.Combine(sub, "b.txt"), "B");
                Directory.CreateDirectory(Path.Combine(sub, "inner"));
                FolderNavigation navigator = CreateNavigator();

                FolderRow row = navigator.LoadFoldersViaEnumerator(dir.Path).Single();

                Assert.AreEqual("sub", row.Name);
                Assert.AreEqual(2, row.FileCount);
                Assert.AreEqual(1, row.SubfolderCount);
            }
        }

        [TestMethod]
        public void LoadFilesViaEnumerator_MissingFolder_ReturnsEmpty()
        {
            FolderNavigation navigator = CreateNavigator();

            Assert.AreEqual(0, navigator.LoadFilesViaEnumerator(Path.Combine(Path.GetTempPath(), "sqr_missing_" + System.Guid.NewGuid().ToString("N"))).Count);
        }
    }
}
