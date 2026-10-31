using System;
using System.Collections.Generic;
using System.IO;
using WPF.SequentialRenamer.Services.Common;

namespace WPF.SequentialRenamer.Services.Files
{
    public class MoveUpResult
    {
        public List<MoveUpItem> Done { get; } = new List<MoveUpItem>();
        public List<FileSystemError> Errors { get; } = new List<FileSystemError>();

        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>Поднимает файлы из подпапок в целевую папку.</summary>
    public class MoveUpExecutor
    {
        public MoveUpResult Execute(IEnumerable<MoveUpItem> moves, string targetFolder)
        {
            var result = new MoveUpResult();
            if (moves == null || string.IsNullOrEmpty(targetFolder))
                return result;

            foreach (MoveUpItem move in moves)
            {
                if (move?.SourcePath == null)
                    continue;

                string targetPath = Path.Combine(targetFolder, Path.GetFileName(move.SourcePath));
                try
                {
                    File.Move(move.SourcePath, targetPath);
                    move.TargetPath = targetPath;
                    result.Done.Add(move);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new FileSystemError(move.SourcePath, ex.Message));
                }
            }

            return result;
        }
    }
}
