using ImageViewer.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinProps;
using System.Security.Policy;

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

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010c-0000-0000-c000-000000000046")]
    public interface IPersist
    {
        /// <summary>
        /// Retrieves the class identifier (CLSID) of the object.
        /// </summary>
        /// <param name="pClassID">A pointer to the location that receives the CLSID on return. 
        /// The CLSID is a globally unique identifier (GUID) that uniquely represents an object class that defines the code that can manipulate the object's data.</param>
        /// <returns>If the method succeeds, the return value is S_OK. Otherwise, it is E_FAIL.</returns>
        [PreserveSig]
        int GetClassID(out Guid pClassID);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1079acfc-29bd-11d3-8e0d-00c04f6837d5")]
    public interface IPersistIDList : IPersist
    {
        #region Overriden IPersist Methods

        /// <summary>
        /// Retrieves the class identifier (CLSID) of the object.
        /// </summary>
        /// <param name="pClassID">A pointer to the location that receives the CLSID on return.
        /// The CLSID is a globally unique identifier (GUID) that uniquely represents an object class that defines the code that can manipulate the object's data.</param>
        /// <returns>
        /// If the method succeeds, the return value is S_OK. Otherwise, it is E_FAIL.
        /// </returns>
        [PreserveSig]
        new int GetClassID(out Guid pClassID);

        #endregion

        /// <summary>
        /// Sets a persisted item identifier list.
        /// </summary>
        /// <param name="pidl">A pointer to the item identifier list to set.</param>
        /// <returns>If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        [PreserveSig]
        int SetIDList(IntPtr pidl);

        /// <summary>
        /// Gets an item identifier list.
        /// </summary>
        /// <param name="pidl">The address of a pointer to the item identifier list to get.</param>
        /// <returns>If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        [PreserveSig]
        int GetIDList(out IntPtr pidl);
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
        [PreserveSig]
        int GetWindow(out IntPtr phwnd);
        //int GetControlWindow(uint id, out IntPtr phwnd);

        void _VtblGap1_11(); // skip 12 methods https://stackoverflow.com/a/47567206/403671

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

        int GetParent(out IShellItem parent);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetDisplayName(SIGDN sigdnName);

        // 2 other methods to be defined
    }


    [ComImport, Guid("7e9fb0d3-919f-4307-ab2e-9b1860310c93"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem2
    {
        #region inherited from IShellItem
        void BindToHandler(
            [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc,
            [In] IntPtr bhid,   // A Guid for the Bind Handler (BHID)
            [In] IntPtr riid,   // The iid of the required interface
            [Out] out IntPtr ppv);  // IUnknown
        void GetParent(
            [Out] out IntPtr ppsi); // IShellItem
        void GetDisplayName(
            [In] SIGDN sigdnName,
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(
            [In] SFGAO sfgaoMask,
            [Out] out SFGAO psfgaoAttribs);
        void Compare(
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi,
            [In] SICHINTF hint,
            [Out] out int piOrder);
        #endregion
        void GetPropertyStore(
            [In] GETPROPERTYSTOREFLAGS flags,
            [In] IntPtr riid,   // IID_IPropertyStore
            [Out] out IntPtr ppv);  // IPropertyStore
        void GetPropertyStoreWithCreateObject(
            [In] GETPROPERTYSTOREFLAGS flags,
            [In] IntPtr punkCreateObject,   // factory for low-rights creation of type ICreateObject
            [In] IntPtr riid,   // IID_IPropertyStore
            [Out] out IntPtr ppv);  // IPropertyStore
        void GetPropertyStoreForKeys(
            [In] IntPtr rgKeys, // A Pointer to an array of PROPERTYKEY structures
            [In] uint cKeys,
            [In] GETPROPERTYSTOREFLAGS flags,
            [In] IntPtr riid,   // IID_IPropertyStore
            [Out] out IntPtr ppv);  // IPropertyStore
        void GetPropertyDescriptionList(
            [In] IntPtr keyType,    // A Propertykey defining the list to get eg: System.PropList.FillDetails
            [In] IntPtr riid,   // IID_IPropertyDescriptionList
            [Out] out IntPtr ppv);  // IPropertyDescriptionList
        void Update(
            [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc);
        void GetProperty(
            [In] IntPtr propKey,
            [Out] out IntPtr ppropvar); // PROPVARIANT
        void GetCLSID(
            [In] IntPtr propKey,
            [Out] out IntPtr pclsid);
        void GetFileTime(
            [In] IntPtr propKey,
            [Out] out long pft);
        void GetInt32(
            [In] IntPtr propKey,
            [Out] out int pi);
        void GetString(
            [In] IntPtr propKey,
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string ppsz);
        void GetUInt32(
            [In] IntPtr propKey,
            [Out] out uint pui);
        void GetUInt64(
            [In] IntPtr propKey,
            [Out] out ulong pull);
        void GetBool(
            [In] IntPtr propKey,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool pf);
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
        [DllImport("user32.dll")]
        public static extern void ClipCursor(ref System.Drawing.Rectangle rect);

        [DllImport("user32.dll")]
        public static extern void ClipCursor(IntPtr rect);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(string path, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItemImageFactory factory);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr memcpy(void* dst, void* src, UIntPtr count);

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hwnd, EnumWindowsProc func, IntPtr lParam);


        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();
        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("USER32.DLL")]
        private static extern IntPtr LoadImage(IntPtr hWnd, string name, int type, int cx, int cy, int fuLoad);
        [DllImport("USER32.DLL")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }


        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;
        public static bool ShowFileProperties(string Filename)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            return ShellExecuteEx(ref info);
        }

        private static Bitmap GetBitmapFromHBitmap(IntPtr nativeHBitmap)
        {
            Bitmap bmp = Image.FromHbitmap(nativeHBitmap);

            if (Image.GetPixelFormatSize(bmp.PixelFormat) < 32)
                return bmp;

            using (bmp) return CreateAlphaBitmap(bmp, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        private static unsafe Bitmap CreateAlphaBitmap(Bitmap srcBitmap, System.Drawing.Imaging.PixelFormat targetPixelFormat)
        {
            var result = new Bitmap(srcBitmap.Width, srcBitmap.Height, targetPixelFormat);

            var bmpBounds = new Rectangle(0, 0, srcBitmap.Width, srcBitmap.Height);
            var srcData = srcBitmap.LockBits(bmpBounds, ImageLockMode.ReadOnly, srcBitmap.PixelFormat);
            var destData = result.LockBits(bmpBounds, ImageLockMode.ReadOnly, targetPixelFormat);

            var srcDataPtr = (byte*)srcData.Scan0;
            var destDataPtr = (byte*)destData.Scan0;

            try
            {
                for (int y = 0; y <= srcData.Height - 1; y++)
                {
                    for (int x = 0; x <= srcData.Width - 1; x++)
                    {
                        //this is really important because one stride may be positive and the other negative
                        var position = srcData.Stride * y + 4 * x;
                        var position2 = destData.Stride * y + 4 * x;

                        memcpy(destDataPtr + position2, srcDataPtr + position, (UIntPtr)4);
                    }
                }
            }
            finally
            {
                srcBitmap.UnlockBits(srcData);
                result.UnlockBits(destData);
            }

            return result;
        }

        public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using MemoryStream memory = new MemoryStream();
            
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.DecodePixelWidth = 100;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        public static BitmapImage ExtractThumbnail(string filePath, System.Drawing.Size size, SIIGBF options)
        {
            var hBitmap = GetHBitmap(filePath, size, options);

            try
            {
                // return a System.Drawing.Bitmap from the hBitmap
                var bitmap = GetBitmapFromHBitmap(hBitmap);
                return BitmapToImageSource(bitmap);
            }
            finally
            {
                // delete HBitmap to avoid memory leaks
                DeleteObject(hBitmap);
            }
        }

        private static IntPtr GetHBitmap(string filePath, System.Drawing.Size size, SIIGBF flags)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            IShellItemImageFactory factory;
            int hr = SHCreateItemFromParsingName(filePath, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out factory);
            if (hr != 0)
                throw new Win32Exception(hr);

            IntPtr bmp;
            hr = factory.GetImage(size, flags, out bmp);
            if (hr != 0)
                throw new Win32Exception(hr);

            return bmp;
        }


        // Import necessary Win32 API functions
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);


        public static async Task<List<string>?> GetAllFilesFromExplorer(string baseFile)
        {
            IntPtr explorerHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "CabinetWClass", null);  // Get the handle of the Windows Explorer window
            IntPtr activeTab = FindWindowEx(explorerHandle, IntPtr.Zero, "ShellTabWindowClass", null); //Gets the handle of the currently selected tab


            Shell32.Shell shell = new Shell32.Shell();

            //Get all file explorer windows
            foreach (var window in shell.Windows())
            {
                var allFiles = new List<string>();
                var sp = (IServiceProvider)window;
                
                //Get "top" level browser (to traverse the explorer windows) 
                var SID_STopLevelBrowser = new Guid("4c96be40-915c-11cf-99d3-00aa004ae837");
                var browser = (IShellBrowser)sp.QueryService(SID_STopLevelBrowser, typeof(IShellBrowser).GUID);

                //If current tab in the explorer is not the selected tab, skip it
                browser.GetWindow(out IntPtr thisTab);
                if (thisTab != activeTab) continue;

                //If view is a valid IFolderView, then it is a file explorer instance
                var view = (IFolderView)browser.QueryActiveShellView();
                if (view != null)
                {
                    view.Items(SVGIO.SVGIO_FLAG_VIEWORDER, typeof(IShellItemArray).GUID, out var items);

                    //Gets the focused item index (index is in terms of the order of the folder)
                    try
                    {
                        view.GetFocusedItem(out int startInd);
                        var folderPaths = new List<string>();

                        if (items is IShellItemArray array)
                        {
                            for (var i = 0; i < array.GetCount(); i++)
                            {
                                //Item starts at the one that is currently selected or focused
                                var item = (IShellItem2)array.GetItemAt(i);
                                
                                //Get file location from property
                                var locationProp = new PropertyKey("System.ItemPathDisplay");
                                item.GetString(locationProp.MarshalledPointer, out string fullItemPath);
                                allFiles.Add(fullItemPath.ToLower());
                            }

                            ////Correct shell explorer was found 
                            if (allFiles.Contains(baseFile))
                            {
                                //Okay, so the startInd is actual index of the focused or selected item RELATIVE to the file explorer.
                                //Right now, array.getItemAt(0) always starts with the focused item first. So, we basically shift the 
                                //files so that they move to their correct locations.
                                var sortedFiles = new List<string>(allFiles);
                                for (int i = startInd, currInd = 0; currInd < sortedFiles.Count; i = (i + 1) % sortedFiles.Count, currInd++)
                                {
                                    sortedFiles[i] = allFiles[currInd];
                                }

                                //Exclude folders
                                sortedFiles = sortedFiles.Where(filePath => File.Exists(filePath)).ToList();
                                return sortedFiles;
                            }
                        }
                    }catch (Exception e)
                    {
                        await Logger.WriteData(e.ToString());
                    }
                }
            }

            return new List<string>(){ baseFile };
        }

    }
}
