using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ImageViewer.Win32
{
    [Flags]
    public enum SIIGBF
    {
        SIIGBF_RESIZETOFIT = 0x00000000,
        SIIGBF_BIGGERSIZEOK = 0x00000001,
        SIIGBF_MEMORYONLY = 0x00000002,
        SIIGBF_ICONONLY = 0x00000004,
        SIIGBF_THUMBNAILONLY = 0x00000008,
        SIIGBF_INCACHEONLY = 0x00000010,
        SIIGBF_CROPTOSQUARE = 0x00000020,
        SIIGBF_WIDETHUMBNAILS = 0x00000040,
        SIIGBF_ICONBACKGROUND = 0x00000080,
        SIIGBF_SCALEUP = 0x00000100,
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    public interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(System.Drawing.Size size, SIIGBF flags, out IntPtr phbm);
    }


    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("1AC3D9F0-175C-11d1-95BE-00609797EA4F")]
    public interface IPersistFolder2
    {
        void GetClassID(out Guid pClassID);
        void Initialize(IntPtr pidl);
        void GetCurFolder([MarshalAs(UnmanagedType.SysInt)] out IntPtr pidl);
    }

    public struct POINT
    {
        public long x;
        public long y;
    }


    [Guid("cde725b0-ccc9-4519-917e-325d72fab4ce"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFolderView
    {
        void GetCurrentViewMode(out uint pViewMode);
        void SetCurrentViewMode(uint ViewMode);
        void GetFolder([MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        void Item(int iItemIndex, out IntPtr ppidl);
        void ItemCount(uint uFlags, out int pcItems);

        [PreserveSig]
        int Items(SVGIO uFlags, Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object items);

        void GetSelectionMarkedItem(out int piItem);
        void GetFocusedItem(out int piItem);
        void GetItemPosition(IntPtr pidl, out POINT ppt);
        void GetSpacing(out POINT ppt);
        void GetDefaultSpacing(out POINT ppt);
        [PreserveSig]
        int GetAutoArrange();
        void SelectItem(int iItem, uint dwFlags);
        void SelectAndPositionItems(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref POINT apt, uint dwFlags);
    }



    public enum SortDirection
    {
        Default = 0,
        Descending = -1,
        Ascending = 1,
    }


    //https://github.com/shellscape/Shellscape.Common/blob/aa5465929e842e4bcc88c29c1cc369122c307bc6/Microsoft/Windows%20API/Shell/Interop/ExplorerBrowser/ExplorerBrowserCOMInterfaces.cs
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("1af3a467-214f-4298-908e-06b03e0b39f9")]
    public interface IFolderView2 : IFolderView
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetSortColumnCount(ref int pcColumns);
        //[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        void GetSortColumns([MarshalAs(UnmanagedType.IUnknown)] out object rgSortColumns, int cColumns);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SORTCOLUMN
    {
        public PROPERTYKEY propkey;
        public SortDirection direction;

        public SORTCOLUMN(PROPERTYKEY propkey, SortDirection direction)
        {
            this.propkey = propkey;
            this.direction = direction;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public Int32 pid;

        public PROPERTYKEY(Guid fmtid, Int32 pid)
        {
            this.fmtid = fmtid;
            this.pid = pid;
        }
    }

    // note: for the following interfaces, not all methods are defined as we don't use them here
    [Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellBrowser
    {
        void _VtblGap1_12(); // skip 12 methods https://stackoverflow.com/a/47567206/403671

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object QueryActiveShellView();
    }

    [Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object QueryService([MarshalAs(UnmanagedType.LPStruct)] Guid service, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);
    }

    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItem
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object BindToHandler(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        IShellItem GetParent();

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetDisplayName(SIGDN sigdnName);

        // 2 other methods to be defined
    }

    [Guid("b63ea76d-1f85-456f-a19c-48159efa858b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItemArray
    {
        void _VtblGap1_4(); // skip 4 methods

        int GetCount();
        IShellItem GetItemAt(int dwIndex);
    }

    public enum SIGDN
    {
        SIGDN_NORMALDISPLAY,
        SIGDN_PARENTRELATIVEPARSING,
        SIGDN_DESKTOPABSOLUTEPARSING,
        SIGDN_PARENTRELATIVEEDITING,
        SIGDN_DESKTOPABSOLUTEEDITING,
        SIGDN_FILESYSPATH,
        SIGDN_URL,
        SIGDN_PARENTRELATIVEFORADDRESSBAR,
        SIGDN_PARENTRELATIVE,
        SIGDN_PARENTRELATIVEFORUI
    }

    public enum SVGIO
    {
        SVGIO_BACKGROUND,
        SVGIO_SELECTION,
        SVGIO_ALLVIEW,
        SVGIO_CHECKED,
        SVGIO_TYPE_MASK,
        SVGIO_FLAG_VIEWORDER
    }


    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00020400-0000-0000-C000-000000000046")]
    public interface IDispatch
    {
        int GetTypeInfoCount();
        [return: MarshalAs(UnmanagedType.Interface)]
        System.Runtime.InteropServices.ComTypes.ITypeInfo GetTypeInfo([In, MarshalAs(UnmanagedType.U4)] int iTInfo, [In, MarshalAs(UnmanagedType.U4)] int lcid);
        void GetIDsOfNames([In] ref Guid riid, [In, MarshalAs(UnmanagedType.LPArray)] string[] rgszNames, [In, MarshalAs(UnmanagedType.U4)] int cNames, [In, MarshalAs(UnmanagedType.U4)] int lcid, [Out, MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);
    }


    internal static class Win32Util
    {

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr ILCreateFromPath([In, MarshalAs(UnmanagedType.LPWStr)] string pszPath);


        [DllImport("shell32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SHGetPathFromIDListW(IntPtr pidl, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder pszPath);


        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(string path, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItemImageFactory factory);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                DeleteObject(bitmap.GetHbitmap());


                return bitmapImage;
            }
        }


        public static Bitmap ExtractThumbnail(string filePath, System.Drawing.Size size, SIIGBF flags)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            // TODO: you might want to cache the factory for different types of files
            // as this simple call may trigger some heavy-load underground operations
            IShellItemImageFactory factory;
            int hr = SHCreateItemFromParsingName(filePath, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out factory);
            if (hr != 0)
                throw new Win32Exception(hr);

            IntPtr bmp;
            hr = factory.GetImage(size, flags, out bmp);
            if (hr != 0)
                throw new Win32Exception(hr);

            return Bitmap.FromHbitmap(bmp);
        }




        public static List<string> GetAllFilesFromExplorer(string baseFile)
        {
            Shell32.Shell shell = new Shell32.Shell();
            
            //Get all file explorer windows
            foreach (var window in shell.Windows())
            {
                var allFiles = new List<string>();

                var sp = (IServiceProvider)window;
                //Get "top" level browser (to traverse the explorer windows) 
                var SID_STopLevelBrowser = new Guid("4c96be40-915c-11cf-99d3-00aa004ae837");


                var browser = (IShellBrowser)sp.QueryService(SID_STopLevelBrowser, typeof(IShellBrowser).GUID);

                //If view is a valid IFolderView, then it is a file explorer instance
                var view = (IFolderView)browser.QueryActiveShellView();
                if (view != null)
                {
                    view.GetFolder(typeof(IPersistFolder2).GUID, out var pf);
                    IPersistFolder2 persistFolder = (IPersistFolder2)pf;

                    //Get pidl of folder
                    persistFolder.GetCurFolder(out IntPtr pidl);

                    //Convert pidl to string path
                    StringBuilder builder = new StringBuilder(260);
                    SHGetPathFromIDListW(pidl, builder);

                    string basePath = builder.ToString();
                    basePath = string.IsNullOrEmpty(basePath) ? Path.GetDirectoryName(baseFile) : basePath;

                    view.Items(SVGIO.SVGIO_FLAG_VIEWORDER, typeof(IShellItemArray).GUID, out var items);

                    //Gets the focused item index (index is in terms of the order of the folder)
                    view.GetFocusedItem(out int startInd);
                    var folderPaths = new List<string>();

                    if (items is IShellItemArray array)
                    {
                        for (var i = 0; i < array.GetCount(); i++)
                        {
                            //Item starts at the one that is currently selected or focused
                            var item = array.GetItemAt(i);
                            string fullItemPath = $"{basePath}\\{item.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}";


                            //Folders are excluded
                            if (File.Exists(fullItemPath)) allFiles.Add(fullItemPath);
                            else folderPaths.Add(fullItemPath);
                        }

                        //Correct shell explorer was found 
                        if (allFiles.Contains(baseFile))
                        {
                            startInd = (startInd - folderPaths.Count < 0) ? startInd : startInd - folderPaths.Count;

                            //Okay, so the startInd is actual index of the focused or selected item RELATIVE to the file explorer.
                            //Right now, array.getItemAt(0) always starts with the focused item first. So, we basically shift the 
                            //files so that they move to their correct locations.
                            var sortedFiles = new List<string>(allFiles);
                            for (int i = startInd, currInd = 0; currInd < sortedFiles.Count; i = (i + 1) % sortedFiles.Count, currInd++)
                            {
                                sortedFiles[i] = allFiles[currInd];
                            }

                            return sortedFiles;
                        }

                    }
                }
            }

            //If there are no shells(explorers) open or found, then try to use the original image without the directory
            return (File.Exists(baseFile)) ? new List<string>() { baseFile } : new List<string>();
        }

    }
}
