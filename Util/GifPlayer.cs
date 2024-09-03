using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Controls;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Xml.Linq;
using System.Transactions;
using SixLabors.ImageSharp.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using System.Timers;
using System.Windows;

namespace ImageViewer.Util
{
    public class GifFrameEventArgs : EventArgs
    { 
        public BitmapImage FrameImg { get; set; }
        public int CurrFrame { get; set; }

        public GifFrameEventArgs(BitmapImage img, int frame)
        {
            FrameImg = img;
            CurrFrame = frame;
        }
        
    }

    internal class GifPlayer
    {
        private int startingGifFrame = 0;
        private CancellationTokenSource gifDonePlayingToken = new CancellationTokenSource();
        private CancellationTokenSource gifChangeFrameToken = new CancellationTokenSource(); //for changing the progress of the gif
        private bool paused = false;

        private System.Drawing.Image? gifImage;
        public int MaxFrames { get; set; }
        private System.Drawing.Image[]? frames;
        private int[]? frameDelays;

        public event EventHandler<GifFrameEventArgs>? FrameChanged;
        public event EventHandler<EventArgs>? GifCanceled;
        public event EventHandler<EventArgs>? GifPlaying;
        public event EventHandler<EventArgs>? DonePlaying;

        private double currSpeed = 1;
        private int gifPlaying = 0;

        public bool IsPlaying
        {
            get { return (Interlocked.CompareExchange(ref gifPlaying, 1, 1) == 1); }
            set
            {
                if (value) Interlocked.CompareExchange(ref gifPlaying, 1, 0);
                else Interlocked.CompareExchange(ref gifPlaying, 0, 1);
            }
        }


        public void ChangeSpeed(double speedAmt)
        {
            currSpeed = speedAmt;
        }

        public async void Clean()
        {
            //This token checks if the gif progress is changed
            if (gifChangeFrameToken != null)
            {
                gifChangeFrameToken.Cancel();
                gifChangeFrameToken.Dispose();
                
                gifChangeFrameToken = new CancellationTokenSource();
            }

            //This token checks if the gif is done
            if (gifDonePlayingToken != null)
            {
                gifDonePlayingToken.Cancel();
                gifDonePlayingToken.Dispose();

                gifDonePlayingToken = new CancellationTokenSource();
            }

            //Dispose tokens and frames
            if (frames != null)
            {
                foreach (var img in frames)
                {
                    if (img != null) img.Dispose();
                }

                gifImage!.Dispose();
                frames = null;
            }

            GifCanceled?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            gifChangeFrameToken.Cancel();
            paused = true;
        }

        public void Resume()
        {
            gifChangeFrameToken.Cancel();
            paused = false;
        }


        public async Task Play(CancellationTokenSource cancelToken)
        {
            if (cancelToken == null || cancelToken.IsCancellationRequested)
            {
                IsPlaying = false;
                paused = false;

                Clean();
                return;
            }


            IsPlaying = true;
            paused = false;

            if (gifDonePlayingToken == null || gifChangeFrameToken == null) return;
            GifPlaying?.Invoke(this, EventArgs.Empty);
            while (IsPlaying && !gifDonePlayingToken.IsCancellationRequested && !cancelToken.IsCancellationRequested)
            {
                if (startingGifFrame == MaxFrames) startingGifFrame = 0;

                await UpdateFrame(startingGifFrame);
                if (paused)
                {
                    while (paused) await Task.Delay(1, cancelToken.Token);
                    gifChangeFrameToken.Dispose();
                    gifChangeFrameToken = new CancellationTokenSource();
                }
               
                try
                {
                    PropertyItem? item = gifImage!.GetPropertyItem(0x5100); // FrameDelay in libgdiplus
                    var duration = BitConverter.ToInt32(item!.Value, startingGifFrame * 4) * 10;



                    duration = (duration == 0) ? 100 : duration;
                    Debug.WriteLine(duration);
                    await Task.Delay((int)(duration / currSpeed), cancelToken.Token);
                }
                catch(Exception e)
                {
                    await Task.Delay(1);
                }
                startingGifFrame++;
            }

           
            Clean();
            DonePlaying?.Invoke(this, EventArgs.Empty);
        }

        public async Task UpdateFrame(int frame)
        {
            startingGifFrame = frame;

            if (frames![startingGifFrame] == null)
            {
                gifImage!.SelectActiveFrame(FrameDimension.Time, startingGifFrame);
                frames[startingGifFrame] = new Bitmap(gifImage);
            }

            var bytes = ImageToByteArray(frames[startingGifFrame]);

            var bitmapImg = ArrayToBitmap(bytes);
            FrameChanged?.Invoke(this, new GifFrameEventArgs(bitmapImg, frame));
        }

        public byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }

           
        }

        public async Task InitDrawing(string gifPath, CancellationTokenSource cancelToken)
        {
            await Task.Run(() =>
            {
                gifImage = System.Drawing.Image.FromFile(gifPath);
                MaxFrames = gifImage.GetFrameCount(FrameDimension.Time);
                frames = new System.Drawing.Image[MaxFrames];

                currSpeed = 1;

                startingGifFrame = 0;
            });
        }

        public BitmapImage ArrayToBitmap(byte[] arr)
        {
            using (var ms = new MemoryStream(arr))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();

                image.Freeze();
                return image;
            }
        }
    }
}
