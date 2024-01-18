using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private static void SetAssociation(string Extension, string KeyName, string OpenWith, string FileDescription)
        {
            RegistryKey baseKey;
            RegistryKey openMethod;
            RegistryKey shell;

            baseKey = Registry.ClassesRoot.CreateSubKey(Extension);
            baseKey.SetValue("", KeyName);

            openMethod = Registry.ClassesRoot.CreateSubKey(KeyName);
            openMethod.SetValue("", FileDescription);
            openMethod.CreateSubKey("DefaultIcon").SetValue("", "\"" + OpenWith + "\",0");
            shell = openMethod.CreateSubKey("Shell");
            shell.CreateSubKey("edit").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
            shell.CreateSubKey("open").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
            baseKey.Close();
            openMethod.Close();
            shell.Close();

            // Delete the key instead of trying to change it
            var currentUser = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\" + Extension, true);
            currentUser.DeleteSubKey("UserChoice", false);
            currentUser.Close();

            // Tell explorer the file association has been changed
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        public static bool IsFileExtensionSet()
        {
            string nameOfProgram = typeof(MainWindow).Assembly.GetName().Name;
            string exeProgram = typeof(MainWindow).Assembly.GetName().Name + ".exe";
            foreach (var supportedExt in ImgExtensions.SUPPORTED_EXTENSIONS)
            {
                var currentUser = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\" + supportedExt, true);
            }

            return true;
        }

        public static void SetDefaultFileExtensions()
        {
            string nameOfProgram = typeof(MainWindow).Assembly.GetName().Name;
            string exeProgram = typeof(MainWindow).Assembly.GetName().Name + ".exe";
            foreach (var supportedExt in ImgExtensions.SUPPORTED_EXTENSIONS)
            {
                SetAssociation(supportedExt, nameOfProgram, exeProgram, $"{supportedExt} file");
            }
        }
    }
}
