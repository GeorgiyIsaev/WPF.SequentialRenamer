using System;
using System.Runtime.InteropServices;

namespace WPF.SequentialRenamer
{
    /// <summary>
    /// Выбор папки через системный диалог Windows (IFileOpenDialog + FOS_PICKFOLDERS).
    /// Современный Vista-диалог без внешних библиотек.
    /// </summary>
    public class FolderPicker
    {
        /// <summary>Заголовок окна диалога.</summary>
        public string Title { get; set; }

        /// <summary>Папка, открытая при запуске диалога.</summary>
        public string InitialFolder { get; set; }

        /// <summary>Полный путь выбранной папки (null, если нажали "Отмена").</summary>
        public string ResultPath { get; private set; }

        /// <summary>
        /// Показать диалог. owner — дескриптор родительского окна
        /// (new WindowInteropHelper(this).Handle), можно IntPtr.Zero.
        /// Возвращает true, если папка выбрана.
        /// </summary>
        public bool ShowDialog(IntPtr owner)
        {
            var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
            try
            {
                dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_PATHMUSTEXIST);
                if (!string.IsNullOrEmpty(Title))
                    dialog.SetTitle(Title);
                if (!string.IsNullOrEmpty(InitialFolder))
                {
                    IShellItem folder = null;
                    try
                    {
                        Guid iid = typeof(IShellItem).GUID;
                        if (SHCreateItemFromParsingName(InitialFolder, IntPtr.Zero, ref iid, out folder) == 0)
                            dialog.SetFolder(folder);
                    }
                    finally
                    {
                        if (folder != null)
                            Marshal.ReleaseComObject(folder);
                    }
                }

                int hr = dialog.Show(owner);
                if (hr != 0)
                    return false; // отмена или ошибка

                IShellItem result = null;
                try
                {
                    dialog.GetResult(out result);
                    string path;
                    result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out path);
                    ResultPath = path;
                    return true;
                }
                finally
                {
                    if (result != null)
                        Marshal.ReleaseComObject(result);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW { }

        [ComImport, Guid("d57c7288-d4ad-4768-be02-9d969532d960"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr phwndOwner);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
            void GetResults(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc,
            ref Guid riid, out IShellItem ppv);

        [Flags]
        private enum FOS : uint
        {
            FOS_PICKFOLDERS = 0x20,
            FOS_FORCEFILESYSTEM = 0x40,
            FOS_PATHMUSTEXIST = 0x800,
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000,
        }
    }
}
