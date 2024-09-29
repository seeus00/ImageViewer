using FluentFTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer.Util.FTP
{
    public static class FTPUtil
    {
        private static FtpClient? Client { get; set; }

        public static bool IsConnected(string host) => Client != null && Client!.IsConnected && Client.Host == host;

        public static void Connect(string ip, int port, string username, string password)
        {
            //If client hasn't been initialized or the host has changed, reconnect
            if (!IsConnected(ip))
            {
                try
                {
                    Client = new FtpClient(ip, username, password, port);
                    Client.Connect();
                }catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                    Client = null;
                }
            }
        }

        public static void Disconnect()
        {
            if (Client == null || !Client.IsConnected) return;

            Client.Disconnect();
        }

        public static byte[] GetBytes(string filePath)
        {
            if (Client == null || !Client.IsConnected) return null;

            byte[] bytes;
            Client.DownloadBytes(out bytes, filePath);

            return bytes;

        }
    }
}
