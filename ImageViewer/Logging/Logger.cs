using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer.Logging
{
    public static class Logger
    {
        private static readonly string LOGGING_PATH = 
            $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/{typeof(MainWindow).Assembly.GetName().Name}/log_file.log";

        static Logger()
        {
            string? basePath = Path.GetDirectoryName(LOGGING_PATH);
            if (!Directory.Exists(basePath!)) Directory.CreateDirectory(basePath!);
            if (!File.Exists(LOGGING_PATH)) File.Create(LOGGING_PATH).Close();
        }

        public static async Task WriteData(string data)
        {
            await File.AppendAllTextAsync(LOGGING_PATH, data + "\n\n");
        }

    }
}
