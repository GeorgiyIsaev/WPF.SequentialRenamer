using System;
using System.Collections.Generic;
using System.IO;

namespace WPF.SequentialRenamer.Services.Files
{
    /// <summary>
    /// Собирает файлы из подпапок открытой папки и проверяет,
    /// не перекрываются ли имена файлов (без учёта регистра) с уже существующими.
    /// </summary>
    public class MoveUpPlanner
    {
        public List<MoveUpItem> CollectFilesFromSubfolders(string rootPath)
        {
            var moves = new List<MoveUpItem>();
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                return moves;

            try
            {
                foreach (string dir in Directory.EnumerateDirectories(rootPath))
                {
                    try
                    {
                        foreach (string file in Directory.EnumerateFiles(dir))
                        {
                            moves.Add(new MoveUpItem
                            {
                                SourcePath = file,
                                FolderName = Path.GetFileName(dir)
                            });
                        }
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return moves;
        }

        public List<MoveUpItem> FindCollisions(IEnumerable<MoveUpItem> moves, string rootPath)
        {
            var collisions = new List<MoveUpItem>();
            if (moves == null)
                return collisions;

            var occupied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
            {
                try
                {
                    foreach (string file in Directory.EnumerateFiles(rootPath))
                        occupied[Path.GetFileName(file)] = "файл в открытой папке";

                    foreach (string dir in Directory.EnumerateDirectories(rootPath))
                        occupied[Path.GetFileName(dir)] = "папка в открытой папке";
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            foreach (MoveUpItem move in moves)
            {
                string name = Path.GetFileName(move.SourcePath);
                if (occupied.ContainsKey(name))
                {
                    collisions.Add(move);
                    continue;
                }

                occupied[name] = "файл из папки \"" + move.FolderName + "\"";
            }

            return collisions;
        }
    }
}
