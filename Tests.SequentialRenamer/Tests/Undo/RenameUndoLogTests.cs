using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPF.SequentialRenamer.Services.Undo;

namespace Tests.SequentialRenamer.Undo
{
    [TestClass]
    public class RenameUndoLogTests
    {
        private static UndoEntry Entry(bool isFolders, params string[] names)
        {
            var entry = new UndoEntry { IsFolders = isFolders };
            foreach (string name in names)
            {
                entry.Add(new List<UndoItem>
                {
                    new UndoItem { CurrentPath = @"C:\new\" + name, OriginalPath = @"C:\old\" + name }
                });
            }

            return entry;
        }

        [TestMethod]
        public void Push_EmptyEntry_DoesNothing()
        {
            var log = new RenameUndoLog();

            log.Push(new UndoEntry { IsFolders = false });

            Assert.IsFalse(log.HasEntries);
        }

        [TestMethod]
        public void Push_NullEntry_Throws()
        {
            var log = new RenameUndoLog();

            Assert.ThrowsException<ArgumentNullException>(() => log.Push(null));
        }

        [TestMethod]
        public void Pop_EmptyLog_Throws()
        {
            var log = new RenameUndoLog();

            Assert.ThrowsException<InvalidOperationException>(() => log.Pop());
        }

        [TestMethod]
        public void Pop_ReturnsEntriesInReverseOrderOfPushing()
        {
            var log = new RenameUndoLog();
            UndoEntry first = Entry(isFolders: true, "1");
            UndoEntry second = Entry(isFolders: false, "2");

            log.Push(first);
            log.Push(second);

            Assert.AreSame(second, log.Pop());
            Assert.IsTrue(log.HasEntries);

            Assert.AreSame(first, log.Pop());
            Assert.IsFalse(log.HasEntries);
        }

        [TestMethod]
        public void Pop_AfterLastEntry_ThrowsAgain()
        {
            var log = new RenameUndoLog();
            log.Push(Entry(isFolders: true, "1"));

            log.Pop();

            Assert.ThrowsException<InvalidOperationException>(() => log.Pop());
        }
    }
}
