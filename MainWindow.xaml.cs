using ImageViewer.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using ImageViewer.Util;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using ImageViewer.Util.Extensions;
using ImageViewer.Util.HttpUtil;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Diagnostics;


namespace ImageViewer
{
    public partial class MainWindow : Window
    {
        //For phone metadata 
        private const string ORIENTATION_QUERY = "System.Photo.Orientation";

        private int currImgInd;
        private List<string>? imgFiles;

        private bool imageDoneLoading = false;

        private List<CancellationTokenSource> cancelTokens = new List<CancellationTokenSource>();
        private List<Thread> threads = new List<Thread>();

        public static readonly SemaphoreSlim gifSS = new SemaphoreSlim(1, 1);
        private GifPlayer gifPlayer;


        private CancellationTokenSource gifCancelToken = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Minimized;

            MouseWheel += MainViewControl.CanvasMouseWheel;
            Closed += MainWindow_Closed;

            //Init player for gifs
            gifPlayer = new GifPlayer();
            gifPlayer.FrameChanged += GifPlayer_FrameChanged;
            gifPlayer.GifCanceled += GifPlayer_GifCanceled;
            gifPlayer.GifPlaying += GifPlayer_GifPlaying;
            gifPlayer.DonePlaying += GifPlayer_DonePlaying; ;
        }

        

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        public static bool IsRunningAsAdministrator()
        {
            // Get current Windows user
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();

            // Get current Windows user principal
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);

            // Return TRUE if user is in role "Administrator"
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Enable dark mode
            EnableDarkmode.UseImmersiveDarkMode(new WindowInteropHelper(this).Handle, true);

            //Sometimes, longer file args will be shortened. We want the full path to the file
            await Dispatcher.BeginInvoke(async () =>
            {
                var args = Environment.GetCommandLineArgs()
                   .Where(arg => File.Exists(arg))
                   .Select(arg => new FileInfo(arg).FullName)
                   .ToArray();

                if (args.Length < 2 || !IsValidImgFile(args[1]))
                {
                    MessageBox.Show("An image file was not loaded or was not valid", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

                    if (Application.Current == null) return;
                    Application.Current.Shutdown();

                    return;
                }
                else
                {
                    string fileArg = args[1].ToLower();

                    imgFiles = await Win32Util.GetAllFilesFromExplorer(fileArg);
                    if (!imgFiles!.Any())
                    {
                        MessageBox.Show("No image files were found", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

                        if (Application.Current == null) return;
                        Application.Current.Shutdown();

                        return;
                    }

                    imgFiles = imgFiles.Where(imgFile => IsValidImgFile(imgFile)).ToList();
                    currImgInd = imgFiles.IndexOf(fileArg);
                }

                await Task.Run(() => SetMainImage());
            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
                        
      
        private void LoadImgThread(Rotation rotation = Rotation.Rotate0)
        {
            var cancelToken = new CancellationTokenSource();
            cancelTokens.Add(cancelToken);

            var thread = new Thread(async () =>
            {
                try
                {
                    MainViewControl.ImgSrc = Win32Util.ExtractThumbnail(imgFiles![currImgInd], new System.Drawing.Size(200, 200), SIIGBF.SIIGBF_THUMBNAILONLY);

                    await Dispatcher.BeginInvoke(() =>
                    {
                        MainViewControl.MainImage.Source = MainViewControl.ImgSrc;
                        MainViewControl.ApplyDimensions();
                    }, System.Windows.Threading.DispatcherPriority.ContextIdle);


                    BitmapImage img = await Task.Run(() => GetBitmapImage(imgFiles![currImgInd], 
                        cancelToken, defaultRot: rotation), cancelToken.Token);

                    cancelToken.Token.ThrowIfCancellationRequested();
                    MainViewControl.ImgSrc = img;

                    await Dispatcher.BeginInvoke(async () =>
                    {
                        try
                        {
                            if (Path.GetExtension(imgFiles[currImgInd]) == ImgExtensions.GIF)
                            {
                                //MainViewControl.MainImage.Source = MainViewControl.ImgSrc;

                                await gifPlayer.InitDrawing(imgFiles[currImgInd], cancelToken);
                                MainViewControl.ApplyDimensions();
                                //await gifPlayer.UpdateFrame(20);
                                await gifPlayer.Play(cancelToken);
                            
                            }
                            else
                            {
                                MainViewControl.MainImage.Source = MainViewControl.ImgSrc;
                            }

                            MainViewControl.ApplyDimensions();
                            imageDoneLoading = true;

                        }
                        catch (Exception e)
                        {
                            //var animator = AnimationBehavior.GetAnimator(MainViewControl.MainImage);
                            //if (animator != null)
                            //{
                            //    AnimationBehavior.SetSourceUri(MainViewControl.MainImage, null);

                            //}
                        }
                    }, System.Windows.Threading.DispatcherPriority.Normal);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            });

            threads.Add(thread);
            thread.Start();
            thread.Join();
        }

        private void GifSpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = GifSpeedComboBox.SelectedItem as ComboBoxItem;
            if (item == null) return;

            string? content = item.Content.ToString();
            if (string.IsNullOrEmpty(content)) return;

            double speed = 1;
            switch (content)
            {
                case "0.5x":
                    speed = 0.5;
                    break;
                case "1x":
                    speed = 1;
                    break;
                case "1.5x":
                    speed = 1.5;
                    break;
                case "2x":
                    speed = 2;
                    break;
            }
            gifPlayer.ChangeSpeed(speed);
        }

        private async void GifSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            await gifPlayer.UpdateFrame((int)GifSlider.Value);
        }

        private async void GifSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            gifPlayer.Pause();
        }

        private void GifSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            gifPlayer.Resume();
        }

        private async void GifPlayer_DonePlaying(object? sender, EventArgs e)
        {
            await Dispatcher.BeginInvoke(() =>
            {
                GifSlider.Visibility = Visibility.Hidden;
                GifSpeedComboBox.Visibility = Visibility.Hidden;
            });
        }

        private async void GifPlayer_GifPlaying(object? sender, EventArgs e)
        {
            await Dispatcher.BeginInvoke(() => 
            { 
                GifSlider.Maximum = gifPlayer.MaxFrames - 1;
                GifSlider.Visibility = Visibility.Visible;
                GifSpeedComboBox.Visibility = Visibility.Visible;
                GifSpeedComboBox.SelectedItem = null;
            });
        }

        private async void GifPlayer_GifCanceled(object? sender, EventArgs e)
        {
            await Dispatcher.BeginInvoke(() =>
            {
                GifSlider.Visibility = Visibility.Hidden;
                GifSpeedComboBox.Visibility = Visibility.Hidden;
            });
        }

        private void GifPlayer_FrameChanged(object? sender, GifFrameEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MainViewControl.MainImage.Source = e.FrameImg;
                GifSlider.Value = e.CurrFrame;
                MainViewControl.ApplyDimensions(resetZoom: false, reset: false);
            });
        }

        private void DestroyCancelTokensAndThreads()
        {
            foreach (var token in cancelTokens)
            {
                token.Cancel();
                token.Dispose();
            }
            cancelTokens.Clear();

            foreach (var worker in threads) worker.Interrupt();
            threads.Clear();
        }


        private async void SetMainImage()
        {
            DestroyCancelTokensAndThreads();

            if (!File.Exists(imgFiles![currImgInd])) imgFiles.Remove(imgFiles![currImgInd]);

            MainViewControl.ImgPath = imgFiles![currImgInd];
            MainViewControl.ImgSrc = null;
            imageDoneLoading = false;

            await Dispatcher.BeginInvoke(() =>
            {
                string fileName = Path.GetFileName(imgFiles![currImgInd]);
                Title = $"{fileName} - [{currImgInd + 1}/{imgFiles.Count}]";
                FilePathDisplay.Text = imgFiles![currImgInd];
            });

            LoadImgThread();

        }


        private BitmapImage GetBitmapImage(string filePath, CancellationTokenSource token, bool useFileStream = true, Rotation defaultRot = Rotation.Rotate0)
        {
            //If image has metadata that dictates rotation, reset the rotation so that it always results in the original orientation
            //For images produced by mobile phones usually...
            if (useFileStream)
            {
                Rotation rotation = defaultRot;
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);


                BitmapFrame? bitmapFrame = BitmapFrame.Create(fileStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad);
                BitmapMetadata? bitmapMetadata = bitmapFrame.Metadata as BitmapMetadata;

                if ((bitmapMetadata != null) && (bitmapMetadata.ContainsQuery(ORIENTATION_QUERY)))
                {
                    object o = bitmapMetadata!.GetQuery(ORIENTATION_QUERY);

                    if (o != null)
                    {
                        switch ((ushort)o)
                        {
                            case 6:
                                rotation = Rotation.Rotate90;
                                break;
                            case 3:
                                rotation = Rotation.Rotate180;
                                break;
                            case 8:
                                rotation = Rotation.Rotate270;
                                break;
                        }
                    }
                }


                var image = new BitmapImage();

                image.BeginInit();
                image.StreamSource = fileStream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.Rotation = rotation;
                image.EndInit();

                image.Freeze();

                return image;
            } else
            {
                var image = new BitmapImage();

                image.BeginInit();
                image.UriSource = new Uri(filePath);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                image.Freeze();

                return image;
            }
        }


        private bool IsValidImgFile(string filePath) => ImgExtensions.SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(filePath).ToLower());


        private int MoveBack() => (currImgInd - 1 < 0) ? currImgInd : Interlocked.Decrement(ref currImgInd);
        private int MoveFoward() => (currImgInd + 1 >= imgFiles!.Count) ? currImgInd : Interlocked.Increment(ref currImgInd);

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            int prevInd = currImgInd;
            if (MoveBack() != prevInd) SetMainImage();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int prevInd = currImgInd;
            if (MoveFoward() != prevInd) SetMainImage();
        }

        private void DeletePrompt()
        {
            var resp = MessageBox.Show($"Are you sure you want to delete {Path.GetFileName(imgFiles[currImgInd])}", "File deletion",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (resp == MessageBoxResult.Yes)
            {
                if (gifPlayer.IsPlaying)
                {
                    gifPlayer.Pause();
                    gifPlayer.Clean();
                }

                string fileToDel = imgFiles[currImgInd];
                File.Delete(fileToDel);

                imgFiles.Remove(fileToDel);

                MoveBack();
                SetMainImage();
            }
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            int prevInd = currImgInd;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.S)
                {
                    MainViewControl.SaveImage(imgFiles![currImgInd]);   
                }
            }

            if (e.Key == Key.D || e.Key == Key.Right)
            {
                if (MoveFoward() != prevInd) SetMainImage();
            }else if (e.Key == Key.A || e.Key == Key.Left)
            {
                if (MoveBack() != prevInd) SetMainImage();
            }
            
            if (e.Key == Key.R)
            {
                MainViewControl.Rotate();
            }
            if (e.Key == Key.Delete)
            {
                DeletePrompt();
            }
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!imageDoneLoading) return;

            MainViewControl.ApplyDimensions(false);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.SavePlacement();

            DestroyCancelTokensAndThreads();
            gifPlayer.Clean();
        }

        private async void SearchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.BeginInvoke(async () =>
            {
                //Skip dot
                string ext = Path.GetExtension(imgFiles![currImgInd])[1..];
                var imgBytes = MainViewControl.MainImage.ImageSourceToBytes();

                var resp = await Requests.UploadImage("https://saucenao.com/search.php", ext, imgBytes);
                string html = await resp.Content.ReadAsStringAsync();

                string jqueryStr = Regex.Match(html, "\"(scripts\\/static\\/jquery-.*?)\"").Groups[1].Value;
                html = Regex.Replace(html, "\"scripts\\/static\\/jquery.*?\"", $"\"https://saucenao.com/{jqueryStr}\"");
                html = Regex.Replace(html, "css\\/saucenao-new\\.css", "https://saucenao.com/css/saucenao-new.css");
                
                await Dispatcher.BeginInvoke(async () =>
                {
                    var window = new SaucenaoWindow();
                    window.Html = html;
                    window.Show();
                });
            });
        }

        private void OpenFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(imgFiles![currImgInd])) Process.Start("explorer.exe", $"/select, \"{imgFiles![currImgInd]}\"");
        }

        private void FilePropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(imgFiles![currImgInd])) return;

            Win32Util.ShowFileProperties(imgFiles![currImgInd]);
        }

        private void MainViewControl_MouseMove(object sender, MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                var data = new DataObject(DataFormats.FileDrop, new string[] { imgFiles![currImgInd] }); ;
                DragDrop.DoDragDrop(MainViewControl.MainImage, data, DragDropEffects.Copy);
            }
        }

        private void CopyFullPathMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(imgFiles![currImgInd]);
        }
    }
}
