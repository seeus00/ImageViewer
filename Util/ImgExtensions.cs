using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer.Util
{
    public static class ImgExtensions
    {
        public const string GIF = ".gif";
        public const string WEBP = ".webp";
        public const string JPG = ".jpg";
        public const string JPEG = ".jpeg";
        public const string PNG = ".png";
        public const string AVIF = ".avif";
        public const string ICO = ".ico";
        public const string TIF = ".tif";

        public static string[] SUPPORTED_EXTENSIONS => GetExtensions();

        private static string[] GetExtensions()
        {
            FieldInfo[] fieldInfos = typeof(ImgExtensions).GetFields(BindingFlags.Public |
                 BindingFlags.Static | BindingFlags.FlattenHierarchy);

            return fieldInfos.Select(fi => (string)fi.GetValue(null)).ToArray();
        }
    }
}
