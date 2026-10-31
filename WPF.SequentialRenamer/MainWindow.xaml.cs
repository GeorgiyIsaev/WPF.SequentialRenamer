using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Input;
using WPF.SequentialRenamer.Models;
using WPF.SequentialRenamer.Services.Files;
using WPF.SequentialRenamer.Services.Folders;
using WPF.SequentialRenamer.Services.Rename;
using WPF.SequentialRenamer.Services.Undo;
using WPF.SequentialRenamer.Utils;
using VTH = System.Windows.Media.VisualTreeHelper;

namespace WPF.SequentialRenamer
{
    public partial class MainWindow : Window
    {
        private const string NoFolderSelectedHeader = "Файлы (выберите папку выше)";

        private readonly FolderNavigation _navigator;
        private readonly RenamePlanner _planner = new RenamePlanner();
        private readonly RenameConfigParser _parser = new RenameConfigParser();
        private readonly RenameBatchRunner _renameRunner = new RenameBatchRunner();
        private readonly RenameConflictChecker _conflictChecker = new RenameConflictChecker();
        private readonly RenameUndoLog _undoLog = new RenameUndoLog();
        private readonly RenameUndoService _undoService = new RenameUndoService();
        private readonly MoveUpPlanner _moveUpPlanner = new MoveUpPlanner();
        private readonly MoveUpExecutor _moveUpExecutor = new MoveUpExecutor();

        private string _currentPath;
        private FolderRow _selectedFolder;
        private FolderRow _contextMenuFolder;
        private bool _isFolderMode;

        private List<FolderRow> _cachedFolders;
        private List<FileRow> _cachedFiles;

        public MainWindow()
        {
            InitializeComponent();
            _navigator = new FolderNavigation();

            InitDigitsComboBox();
            Button_Undo.IsEnabled = _undoLog.HasEntries;
            SetRenameMode(isFolderMode: false);
            BuildFoldersContextMenu();
        }

        private void InitDigitsComboBox()
        {
            for (int digits = 1; digits <= 18; digits++)
                ComboBox_Digits.Items.Add(digits);

            ComboBox_Digits.SelectedItem = 3;
        }

        private void ComboBox_Digits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRenamePreview();
        }

        private void MenuItem_FilesMode_Click(object sender, RoutedEventArgs e)
        {
            SetRenameMode(isFolderMode: false);
        }

        private void MenuItem_FoldersMode_Click(object sender, RoutedEventArgs e)
        {
            SetRenameMode(isFolderMode: true);
        }

        private void SetRenameMode(bool isFolderMode)
        {
            _isFolderMode = isFolderMode;
            MenuItem_FilesMode.IsChecked = !isFolderMode;
            MenuItem_FoldersMode.IsChecked = isFolderMode;
            Button_Rename.Content = isFolderMode ? "Переименовать папки" : "Переименовать файлы";
            UpdateRenamePreview();
        }

        private void BuildFoldersContextMenu()
        {
            var menu = new ContextMenu();
            var openItem = new MenuItem { Header = "Открыть папку" };
            openItem.Click += MenuItem_OpenSubFolder_Click;
            menu.Items.Add(openItem);
            ListView_Folders.ContextMenu = menu;
        }

        private void ListView_Folders_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _contextMenuFolder = FindFolderRowFromSource(e.OriginalSource as DependencyObject);
            if (_contextMenuFolder == null)
                e.Handled = true;
        }

        private void ListView_Folders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FolderRow folder = FindFolderRowFromSource(e.OriginalSource as DependencyObject);
            if (folder != null)
                NavigateTo(folder.Path);
        }

        /// <summary>Поднимается по визуальному дереву от точки клика до строки списка и возвращает её папку.</summary>
        private static FolderRow FindFolderRowFromSource(DependencyObject source)
        {
            while (source != null && !(source is ListViewItem))
                source = VTH.GetParent(source);

            return (source as ListViewItem)?.Content as FolderRow;
        }

        private void MenuItem_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderPicker
            {
                Title = "Открыть папку",
                InitialFolder = string.IsNullOrEmpty(_currentPath) ? null : _currentPath
            };

            if (dialog.ShowDialog(new WindowInteropHelper(this).Handle))
                NavigateTo(dialog.ResultPath);
        }

        private void Button_GoUp_Click(object sender, RoutedEventArgs e)
        {
            string parent;
            if (!_navigator.TryGetParentPath(_currentPath, out parent))
                return;

            NavigateTo(parent);
        }

        private void MenuItem_OpenSubFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_contextMenuFolder != null)
                NavigateTo(_contextMenuFolder.Path);
        }

        private void NavigateTo(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            FolderNavigateResult result = _navigator.NavigateTo(path);
            if (!result.Success)
            {
                RecoverToAccessibleFolder(path);
                return;
            }

            ShowFolder(result.State, showCurrentFolderFiles: false);
        }

        /// <summary>
        /// Папка недоступна (удалена или переименована извне): поднимаемся вверх по иерархии,
        /// пока не найдём существующую папку, и показываем её содержимое.
        /// </summary>
        private void RecoverToAccessibleFolder(string path)
        {
            string accessible;
            if (!_navigator.TryFindAccessibleFolder(path, out accessible))
            {
                ShowWarning("Навигация", "Папка «" + path + "» недоступна, как и все её родительские папки.");
                return;
            }

            App.Log.Warn("Навигация: папка «" + path + "» недоступна, открыта ближайшая существующая «" + accessible + "».");

            FolderNavigateResult recovered = _navigator.NavigateTo(accessible);
            if (!recovered.Success)
            {
                ShowWarning("Навигация", recovered.Error ?? "Не удалось перейти к папке.");
                return;
            }

            ShowFolder(recovered.State, showCurrentFolderFiles: true);
        }

        private void ShowFolder(FolderNavigateState state, bool showCurrentFolderFiles)
        {
            _currentPath = state.Path;
            Title = "SequentialRenamer – " + _currentPath;
            Label_RootPath.Text = _currentPath;
            Button_GoUp.IsEnabled = state.ParentPath != null;

            _selectedFolder = null;
            _contextMenuFolder = null;
            ShowNoFilesSelected();

            ApplyFolders(state.Folders);

            if (showCurrentFolderFiles)
            {
                GroupBox_Files.Header = "Файлы — " + Path.GetFileName(_currentPath);
                ApplyFiles(_navigator.LoadFiles(_currentPath));
            }
        }

        private void ListView_Folders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedFolder = ListView_Folders.SelectedItem as FolderRow;
            if (_selectedFolder == null)
            {
                ShowNoFilesSelected();
                return;
            }

            if (!Directory.Exists(_selectedFolder.Path))
            {
                RefreshCurrentFolder();
                return;
            }

            ApplyFiles(_navigator.LoadFiles(_selectedFolder.Path));
        }

        private void ApplyFolders(IReadOnlyList<FolderRow> folders)
        {
            _cachedFolders = folders == null ? new List<FolderRow>() : folders.ToList();
            Sort(_cachedFolders, folder => folder.Name);
            ListView_Folders.ItemsSource = _cachedFolders;
        }

        private void ApplyFiles(IReadOnlyList<FileRow> files)
        {
            _cachedFiles = files == null ? new List<FileRow>() : files.ToList();
            Sort(_cachedFiles, file => file.NameWithoutExt);
            GroupBox_Files.Header = "Файлы — " + (_selectedFolder?.Name ?? Path.GetFileName(_currentPath));
            ListView_Files.ItemsSource = _cachedFiles;
        }

        private void ShowNoFilesSelected()
        {
            GroupBox_Files.Header = NoFolderSelectedHeader;
            ListView_Files.ItemsSource = null;
        }

        private void RefreshCurrentFolder()
        {
            if (string.IsNullOrEmpty(_currentPath))
                return;

            FolderNavigateResult result = _navigator.NavigateTo(_currentPath);
            if (result.Success)
            {
                ApplyFolders(result.State.Folders);
                return;
            }

            RecoverToAccessibleFolder(_currentPath);
        }

        private static void Sort<T>(List<T> list, Func<T, string> nameSelector)
        {
            list.Sort((a, b) => NameComparer.CompareWithoutExtension(nameSelector(a), nameSelector(b)));
        }

        private void Config_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRenamePreview();
        }

        private void UpdateRenamePreview()
        {
            if (Label_RenameMode == null)
                return;

            RenameConfigParseResult parseResult = ParseConfig();

            string example;
            if (parseResult.HasError)
            {
                example = parseResult.Error;
            }
            else
            {
                string baseName = RenameFileNameUtils.BuildFileName(
                    parseResult.Config.Prefix,
                    parseResult.Config.Suffix,
                    parseResult.Config.Digits,
                    parseResult.Config.Start,
                    string.Empty);
                string previewName = _isFolderMode ? baseName : baseName + ".jpg";
                string nameProblem = RenameFileNameUtils.FindFinalNameProblem(previewName);
                example = nameProblem ?? previewName;
            }

            string mode = _isFolderMode ? "Переименование папок" : "Переименование файлов";
            if (parseResult.HasError)
                Label_RenameMode.Text = "Режим: " + mode + "\nОшибка: " + parseResult.Error;
            else
                Label_RenameMode.Text = "Режим: " + mode + "\nПример имени: " + example;
        }

        private RenameConfigParseResult ParseConfig()
        {
            return _parser.Parse(
                TextBox_Prefix.Text,
                TextBox_Suffix.Text,
                GetDigitsText(),
                TextBox_StartNumber.Text);
        }

        private string GetDigitsText()
        {
            if (ComboBox_Digits.SelectedItem is int digits)
                return digits.ToString(CultureInfo.InvariantCulture);

            return "3";
        }

        private RenameConfig GetConfigOrShowError()
        {
            RenameConfigParseResult parseResult = ParseConfig();
            if (parseResult.HasError)
            {
                ShowWarning("Переименование", parseResult.Error);
                return null;
            }

            return parseResult.Config;
        }

        private void Button_Rename_Click(object sender, RoutedEventArgs e)
        {
            if (_isFolderMode)
                RenameFolders();
            else
                RenameFiles();
        }

        private void RenameFolders()
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                ShowInfo("Переименование", "Сначала откройте папку.");
                return;
            }

            if (_cachedFolders == null || _cachedFolders.Count == 0)
            {
                ShowInfo("Переименование", "Сначала откройте папку, содержащую подпапки.");
                return;
            }

            RenameConfig config = GetConfigOrShowError();
            if (config == null)
                return;

            if (!_navigator.IsFolderListActual(_currentPath, _cachedFolders))
            {
                ShowWarning(
                    "Переименование",
                    "Содержимое папки изменилось извне — папки были переименованы, добавлены или удалены. " +
                    "Список обновлён, повторите переименование.");
                RefreshCurrentFolder();
                return;
            }

            List<RenameItem> items = _planner.BuildRenameItemsForFolder(_cachedFolders, config, _currentPath);
            if (!ConfirmRenameTargets(items))
                return;

            RunRename(items, isFolders: true);
            RefreshCurrentFolder();
        }

        private void RenameFiles()
        {
            if (_selectedFolder == null)
            {
                ShowInfo("Переименование", "Сначала выберите папку в левом блоке.");
                return;
            }

            if (!Directory.Exists(_selectedFolder.Path))
            {
                ShowWarning(
                    "Переименование",
                    "Выбранная папка «" + _selectedFolder.Name + "» больше не существует. Список обновлён.");
                _selectedFolder = null;
                RefreshCurrentFolder();
                return;
            }

            RenameConfig config = GetConfigOrShowError();
            if (config == null)
                return;

            ApplyFiles(_navigator.LoadFiles(_selectedFolder.Path));
            if (_cachedFiles == null || _cachedFiles.Count == 0)
            {
                ShowInfo("Переименование", "В выбранной папке нет файлов.");
                return;
            }

            List<RenameItem> items = _planner.BuildRenameItemsForFiles(_cachedFiles, config, _selectedFolder.Path);
            if (!ConfirmRenameTargets(items))
                return;

            RunRename(items, isFolders: false);
            ApplyFiles(_navigator.LoadFiles(_selectedFolder.Path));
        }

        private bool ConfirmRenameTargets(IReadOnlyList<RenameItem> items)
        {
            List<string> nameProblems = RenamePlanner.FindFinalNameProblems(items);
            if (nameProblems.Count > 0)
            {
                ShowWarning(
                    "Переименование",
                    "Переименование отменено: итоговые имена недопустимы.\n\n" + JoinLimited(nameProblems, 5));
                return false;
            }

            IReadOnlyList<string> conflicts = _conflictChecker.FindExistingConflicts(items);
            if (conflicts.Count > 0)
            {
                ShowWarning(
                    "Переименование",
                    "В папке уже есть файлы или папки с такими именами: «" + string.Join("», «", conflicts) +
                    "». Переименование отменено, чтобы не перезаписать их.");
                return false;
            }

            return true;
        }

        private void RunRename(IEnumerable<RenameItem> items, bool isFolders)
        {
            RenameBatchResult result = _renameRunner.Run(items, isFolders);
            if (result.HasErrors)
                ShowWarning("Переименование", "Не переименовано:\n" + Join(result.Errors));

            if (result.Done.Count == 0)
                return;

            PushUndo(isFolders, _planner.BuildUndoItems(result.Done));
        }

        private void Button_MoveFilesUp_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                ShowInfo("Поднять файлы", "Сначала откройте папку.");
                return;
            }

            List<MoveUpItem> moves = _moveUpPlanner.CollectFilesFromSubfolders(_currentPath);
            if (moves.Count == 0)
            {
                ShowInfo("Поднять файлы", "В подпапках нет файлов — поднимать нечего.");
                return;
            }

            List<MoveUpItem> collisions = _moveUpPlanner.FindCollisions(moves, _currentPath);
            if (collisions.Count > 0)
            {
                string names = string.Join("\n", collisions.Select(c => "\"" + Path.GetFileName(c.SourcePath) + "\""));
                ShowWarning("Поднять файлы", "Перемещение отменено — одинаковые имена:\n\n" + names);
                return;
            }

            MoveUpResult result = _moveUpExecutor.Execute(moves, _currentPath);
            if (result.HasErrors)
                ShowWarning("Поднять файлы", "Не перемещено:\n" + Join(result.Errors));

            if (result.Done.Count > 0)
            {
                IEnumerable<UndoItem> undoItems = result.Done.Select(move => new UndoItem
                {
                    CurrentPath = move.TargetPath,
                    OriginalPath = move.SourcePath
                });
                PushUndo(isFolders: false, items: undoItems);
            }

            RefreshCurrentFolder();
            ListView_Folders.SelectedItem = null;
            _selectedFolder = null;
            GroupBox_Files.Header = "Файлы — " + Path.GetFileName(_currentPath);
            ApplyFiles(_navigator.LoadFiles(_currentPath));
        }

        private void Button_Undo_Click(object sender, RoutedEventArgs e)
        {
            if (!_undoLog.HasEntries)
                return;

            UndoEntry entry;
            try
            {
                entry = _undoLog.Pop();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Button_Undo.IsEnabled = _undoLog.HasEntries;

            RenameUndoResult result = _undoService.Execute(entry);
            if (result.HasErrors)
                ShowWarning("Отмена", "Не отменено:\n" + Join(result.Errors));

            RefreshCurrentFolder();
            if (_selectedFolder != null && Directory.Exists(_selectedFolder.Path))
                ApplyFiles(_navigator.LoadFiles(_selectedFolder.Path));
            else
            {
                _selectedFolder = null;
                ShowNoFilesSelected();
            }
        }

        private void PushUndo(bool isFolders, IEnumerable<UndoItem> items)
        {
            var entry = new UndoEntry { IsFolders = isFolders };
            entry.Add(items);
            _undoLog.Push(entry);
            Button_Undo.IsEnabled = _undoLog.HasEntries;
        }

        private static string Join<T>(IEnumerable<T> values)
        {
            return string.Join("\n", values);
        }

        /// <summary>Склеивает строки, ограничивая список первыми limit строками.</summary>
        private static string JoinLimited(List<string> lines, int limit)
        {
            if (lines.Count <= limit)
                return string.Join("\n", lines);

            return string.Join("\n", lines.Take(limit)) + "\n…и ещё проблем: " + (lines.Count - limit);
        }

        private static void ShowInfo(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void ShowWarning(string title, string message)
        {
            App.Log.Warn(title + ": " + message);
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
