using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer.Util
{
    public static class ConfigManager
    {
        private static readonly string CONFIG_PATH =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/{typeof(MainWindow).Assembly.GetName().Name}/config.cfg";

        private static string DELIMETER = ":";

        public static Dictionary<string, string> CONFIG_INFO = new Dictionary<string, string>();

        static ConfigManager()
        {
            string? basePath = Path.GetDirectoryName(CONFIG_PATH);
            if (!Directory.Exists(basePath!)) Directory.CreateDirectory(basePath!);
            if (!File.Exists(CONFIG_PATH)) File.Create(CONFIG_PATH).Close();
        }

        public static async Task InitConfig()
        {
            foreach (string line in await File.ReadAllLinesAsync(CONFIG_PATH))
            {
                var data = line.Trim().Split(DELIMETER);
                CONFIG_INFO[data[0]] = data[1]; 
            }
        }

        public static async Task WriteConfig()
        {
            await File.WriteAllTextAsync(CONFIG_PATH, string.Empty);
            foreach (var configVal in CONFIG_INFO)
            {
                await File.WriteAllTextAsync(CONFIG_PATH, $"{configVal.Key}:{configVal.Value}");
            }
        }

        public static string? GetValue(string key) => (!CONFIG_INFO.ContainsKey(key)) ? null : CONFIG_INFO[key];
    }
}
