using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mime;

namespace ImageViewer.Util.HttpUtil
{
    public static class Requests
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<HttpResponseMessage> UploadImage(string url, string ext, byte[] ImageData)
        {
            using var requestContent = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(ImageData);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Image.Jpeg);

            requestContent.Add(imageContent, "file", $"image.jpg");

            return await client.PostAsync(url, requestContent);
        }

    }
}
