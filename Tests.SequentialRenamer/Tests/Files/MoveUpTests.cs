using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.SequentialRenamer.Common;
using WPF.SequentialRenamer.Services.Files;

namespace Tests.SequentialRenamer.Files
{
    [TestClass]
    public class MoveUpTests
    {
        private readonly MoveUpPlanner _planner = new MoveUpPlanner();
        private readonly MoveUpExecutor _executor = new MoveUpExecutor();

        [TestMethod]
        public void CollectFilesFromSubfolders_ReturnsFilesWithFolderNames()
        {
            using (var dir = new TempDirectory())
            {
                dir.CreateFile("root.txt");
                string one = dir.CreateFolder("one");
                string two = dir.CreateFolder("two");
                File.WriteAllText(Path.Combine(one, "a.txt"), "A");
                File.WriteAllText(Path.Combine(two, "b.txt"), "B");

                List<MoveUpItem> moves = _planner.CollectFilesFromSubfolders(dir.Path);

                Assert.AreEqual(2, moves.Count);
                Assert.AreEqual("a.txt", Path.GetFileName(moves[0].SourcePath));
                Assert.AreEqual("one", moves[0].FolderName);
                Assert.AreEqual("b.txt", Path.GetFileName(moves[1].SourcePath));
                Assert.AreEqual("two", moves[1].FolderName);
            }
        }

        [TestMethod]
        public void CollectFilesFromSubfolders_MissingFolder_ReturnsEmpty()
        {
            Assert.AreEqual(0, _planner.CollectFilesFromSubfolders(Path.Combine(Path.GetTempPath(), "sqr_missing_" + System.Guid.NewGuid().ToString("N"))).Count);
        }

        [TestMethod]
        public void FindCollisions_ExistingFileInRoot_IsCollisionIgnoringCase()
        {
            using (var dir = new TempDirectory())
            {
                dir.CreateFile("Photo.JPG");
                string sub = dir.CreateFolder("sub");
                File.WriteAllText(Path.Combine(sub, "photo.jpg"), "A");
                List<MoveUpItem> moves = _planner.CollectFilesFromSubfolders(dir.Path);

                List<MoveUpItem> collisions = _planner.FindCollisions(moves, dir.Path);

                Assert.AreEqual(1, collisions.Count);
            }
        }

        [TestMethod]
        public void FindCollisions_SameNameInTwoSubfolders_ReportsSecond()
        {
            using (var dir = new TempDirectory())
            {
                string one = dir.CreateFolder("one");
                string two = dir.CreateFolder("two");
                File.WriteAllText(Path.Combine(one, "same.txt"), "1");
                File.WriteAllText(Path.Combine(two, "same.txt"), "2");
                List<MoveUpItem> moves = _planner.CollectFilesFromSubfolders(dir.Path);

                List<MoveUpItem> collisions = _planner.FindCollisions(moves, dir.Path);

                Assert.AreEqual(1, collisions.Count);
                Assert.AreEqual("two", collisions[0].FolderName);
            }
        }

        [TestMethod]
        public void FindCollisions_UniqueNames_ReturnsEmpty()
        {
            using (var dir = new TempDirectory())
            {
                string one = dir.CreateFolder("one");
                File.WriteAllText(Path.Combine(one, "a.txt"), "A");
                List<MoveUpItem> moves = _planner.CollectFilesFromSubfolders(dir.Path);

                Assert.AreEqual(0, _planner.FindCollisions(moves, dir.Path).Count);
            }
        }

        [TestMethod]
        public void Execute_MovesFilesUpAndSetsTargetPath()
        {
            using (var dir = new TempDirectory())
            {
                string one = dir.CreateFolder("one");
                File.WriteAllText(Path.Combine(one, "a.txt"), "A");
                List<MoveUpItem> moves = _planner.CollectFilesFromSubfolders(dir.Path);

                MoveUpResult result = _executor.Execute(moves, dir.Path);

                Assert.IsFalse(result.HasErrors);
                Assert.AreEqual(1, result.Done.Count);
                Assert.AreEqual(Path.Combine(dir.Path, "a.txt"), result.Done[0].TargetPath);
                Assert.AreEqual("A", File.ReadAllText(Path.Combine(dir.Path, "a.txt")));
                Assert.AreEqual(0, Directory.GetFiles(one).Length);
            }
        }

        [TestMethod]
        public void Execute_MissingSource_ReportsError()
        {
            using (var dir = new TempDirectory())
            {
                var moves = new List<MoveUpItem>
                {
                    new MoveUpItem { SourcePath = Path.Combine(dir.Path, "missing.txt"), FolderName = "one" }
                };

                MoveUpResult result = _executor.Execute(moves, dir.Path);

                Assert.IsTrue(result.HasErrors);
                Assert.AreEqual(0, result.Done.Count);
            }
        }
    }
}
