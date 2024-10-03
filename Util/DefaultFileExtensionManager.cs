using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ImageViewer.Util
{
    public static class DefaultFileExtensionManager
    {
        public class FileAssociation
        {
            public string Extension { get; set; }
            public string ProgId { get; set; }
            public string FileTypeDescription { get; set; }
            public string ExecutableFilePath { get; set; }
        }

        public class FileAssociations
        {
            // needed so that Explorer windows get refreshed after the registry is updated
            [System.Runtime.InteropServices.DllImport("Shell32.dll")]
            private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

            private const int SHCNE_ASSOCCHANGED = 0x8000000;
            private const int SHCNF_FLUSH = 0x1000;


            public static void EnsureAssociationsSet(params FileAssociation[] associations)
            {
                bool madeChanges = false;
                foreach (var association in associations)
                {
                    madeChanges |= SetAssociation(
                        association.Extension,
                        association.ProgId,
                        association.FileTypeDescription,
                        association.ExecutableFilePath);
                }

                if (madeChanges)
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                }
            }

            public static bool SetAssociation(string extension, string progId, string fileTypeDescription, string applicationFilePath)
            {
                bool madeChanges = false;
                madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
                madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + progId, fileTypeDescription);
                madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\shell\open\command", "\"" + applicationFilePath + "\" \"%1\"");
                return madeChanges;
            }

            private static bool SetKeyDefaultValue(string keyPath, string value)
            {
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key.GetValue(null) as string != value)
                    {
                        key.SetValue(null, value);
                        return true;
                    }
                }

                return false;
            }
        }

        //Set default file extensions for images
        public static void SetDefaultFileExtensions()
        {
            string filePath = System.Reflection.Assembly.GetEntryAssembly().Location.Replace(".dll", ".exe");
            foreach (var supportedExt in ImgExtensions.SUPPORTED_EXTENSIONS)
            {
                string extWithoutDot = supportedExt.Split(".")[1];
                FileAssociations.EnsureAssociationsSet(
                    new FileAssociation
                    {
                        Extension = supportedExt,
                        ProgId = $"{extWithoutDot}_Image_File",
                        FileTypeDescription = $"{extWithoutDot.ToUpper()} File",
                        ExecutableFilePath = filePath
                    });
            }
        }
    }
}
