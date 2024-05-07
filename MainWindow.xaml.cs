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
using ImageViewer.UIElements;
using System.IO;
using ImageViewer.Util;
using System.Security.Policy;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Reflection;
using ImageViewer.Properties;
using System.Threading;
using System.Threading.Tasks;
using ImageViewer.Logging;
using System.CodeDom;
using System.Drawing;
using ImageViewer.Util.Extensions;
using ImageViewer.Util.HttpUtil;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Drawing.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Automation;
using XamlAnimatedGif;

namespace ImageViewer
{
    public partial class MainWindow : Window
    {
        private const string ORIENTATION_QUERY = "System.Photo.Orientation";

        private int currImgInd;
        private List<string>? imgFiles;

        private bool imageDoneLoading = false;
        private FileSystemWatcher fileSystemWatcher;

        private List<CancellationTokenSource> cancelTokens = new List<CancellationTokenSource>();
        private List<Thread> threads = new List<Thread>();


        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Minimized;

            MouseWheel += MainViewControl.CanvasMouseWheel;
            Closed += MainWindow_Closed;
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
            EnableDarkmode.UseImmersiveDarkMode(new WindowInteropHelper(this).Handle, true);

            //await ConfigManager.InitConfig();

            ////If default extensions not set, set them (ask for admin privledges)
            //if (ConfigManager.GetValue("default_ext") == null)
            //{
            //    if (!IsRunningAsAdministrator())
            //    {
            //        string startingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            //        string nameOfProgram = typeof(MainWindow).Assembly.GetName().Name + ".exe";

            //        // Setting up start info of the new process of the same application
            //        ProcessStartInfo processStartInfo = new ProcessStartInfo()
            //        {
            //            FileName = $"{startingPath}/{nameOfProgram}"
            //        };

            //        // Using operating shell and setting the ProcessStartInfo.Verb to “runas” will let it run as admin
            //        processStartInfo.UseShellExecute = true;
            //        processStartInfo.Verb = "runas";

            //        // Start the application as new process
            //        try
            //        {
            //            Process.Start(processStartInfo);
            //        } catch (Exception exc)
            //        {
            //            Debug.WriteLine(exc.StackTrace);
            //        }
            //        finally
            //        {
            //            // Shut down the current (old) process
            //            Application.Current.Shutdown();
            //        }

            //        return;
            //    } else if (ConfigManager.GetValue("default_ext") == null)
            //    {
            //        DefaultFileExtensionManager.SetDefaultFileExtensions();

            //        ConfigManager.CONFIG_INFO["default_ext"] = "yes";
            //        await ConfigManager.WriteConfig();
            //    }
            //}

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
                    string fileArg = args[1].Replace("users", "Users");

                    imgFiles = await Win32Util.GetAllFilesFromExplorer(fileArg);
                    if (!imgFiles.Any())
                    {
                        MessageBox.Show("No image files were found", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

                        if (Application.Current == null) return;
                        Application.Current.Shutdown();

                        return;
                    }

                    ////Gets the base path to all the files (just get the first one)
                    //string? basePath = Path.GetDirectoryName(imgFiles[0]);

                    ////Determine when new files are added to the folder so that we can update the img files accordingly 
                    //fileSystemWatcher = new FileSystemWatcher(basePath);
                    //fileSystemWatcher.EnableRaisingEvents = true;
                    //fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                    //fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;


                    imgFiles = imgFiles.Where(imgFile => IsValidImgFile(imgFile)).ToList();
                    currImgInd = imgFiles.IndexOf(fileArg);
                }

                await Task.Run(() => SetMainImage());
            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }


        private async void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            //if (imgFiles!.Contains(e.FullPath))
            //{
            //    string currImgPath = imgFiles[currImgInd];
            //    int prevInd = currImgInd;

            //    imgFiles.Remove(e.FullPath);
            //    currImgInd = imgFiles!.IndexOf(currImgPath);

            //    if (!imgFiles.Any())
            //    {
            //        Dispatcher.Invoke(Application.Current.Shutdown);
            //        return;
            //    }

            //    if (currImgInd == -1)
            //    {
            //        //If file was on the last, set it to the last, the first to the first, if it was neither, then the one to the left
            //        currImgInd = (prevInd == imgFiles.Count + 1) ? imgFiles.Count - 1 : (prevInd == 0) ? 0 : prevInd - 1;

            //        await Task.Run(() => SetMainImage());
            //    }
            //}
        }


        private async void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            //FileStream file = null;
            //try
            //{
            //    await Task.Delay(100);
            //    file = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            //}
            //catch (IOException)
            //{
            //    return;
            //}

            //if (imgFiles!.Contains(e.FullPath)) return;

            ////If image files don't contain the newly added image, then search through the directory again
            //await Dispatcher.BeginInvoke(async () =>
            //{
            //    string currImgPath = imgFiles[currImgInd];

            //    var newImgFiles = await Win32Util.GetAllFilesFromExplorer(e.FullPath);
            //    newImgFiles = newImgFiles.Where(imgFile => IsValidImgFile(imgFile)).ToList();
            //    currImgInd = newImgFiles.IndexOf(currImgPath);

            //    imgFiles = newImgFiles;

            //    string fileName = Path.GetFileName(imgFiles[currImgInd]);
            //    Title = $"{fileName} - [{currImgInd + 1}/{imgFiles.Count}]";
            //});
        }

        private async void SetMainImage()
        {
            foreach (var token in cancelTokens) token.Cancel();
            cancelTokens.Clear();

            foreach (var worker in threads) worker.Interrupt();
            threads.Clear();

            MainViewControl.ImgSrc = null;
            imageDoneLoading = false;

            await Dispatcher.BeginInvoke(() =>
            {
                string fileName = Path.GetFileName(imgFiles![currImgInd]);
                Title = $"{fileName} - [{currImgInd + 1}/{imgFiles.Count}]";
                FilePathDisplay.Text = imgFiles![currImgInd];

                //MainViewControl.MainImage.Source = null;
                //MainViewControl.ImgSrc = null;
            });


            var cancelToken = new CancellationTokenSource();
            cancelTokens.Add(cancelToken);


            //var frameChangedHandler = new EventHandler((object? sender, EventArgs e) =>
            //{
            //    var animator = AnimationBehavior.GetAnimator(MainViewControl.MainImage);
            //    GifSlider.Value = animator.CurrentFrameIndex;
            //});

            //var gifLoadedHandler = new RoutedEventHandler((object sender, RoutedEventArgs e) =>
            //{
            //    var animator = AnimationBehavior.GetAnimator(MainViewControl.MainImage);
            //    animator.CurrentFrameChanged += frameChangedHandler;

            //    GifSlider.Maximum = animator.FrameCount;
            //    GifSlider.Minimum = 0;
            //});

            //await Dispatcher.BeginInvoke(() =>
            //{
            //    var animator = AnimationBehavior.GetAnimator(MainViewControl.MainImage);
            //    if (animator != null)
            //    {
            //        animator.CurrentFrameChanged -= frameChangedHandler;
            //        AnimationBehavior.RemoveLoadedHandler(MainViewControl.MainImage, gifLoadedHandler);
            //        AnimationBehavior.SetSourceUri(MainViewControl.MainImage, null);
            //    }

            //    GifSlider.Visibility = Visibility.Hidden;
            //});

          
            var thread = new Thread(async () =>
            {
                try
                {
                    System.Windows.Controls.Image imgObj = null;

                    MainViewControl.ImgSrc = Win32Util.ExtractThumbnail(imgFiles![currImgInd], new System.Drawing.Size(200, 200), SIIGBF.SIIGBF_THUMBNAILONLY);

                    await Dispatcher.BeginInvoke(() =>
                    {
                        MainViewControl.MainImage.Source = MainViewControl.ImgSrc;
                        MainViewControl.ApplyDimensions();

                        imgObj = new System.Windows.Controls.Image();
                    }, System.Windows.Threading.DispatcherPriority.ContextIdle);


                    BitmapImage img = await Task.Run(() => GetBitmapImage(imgFiles![currImgInd], 
                        cancelToken), cancelToken.Token); 

                    cancelToken.Token.ThrowIfCancellationRequested();
                    MainViewControl.ImgSrc = img;

                    await Dispatcher.BeginInvoke(async () =>
                    {
                        try
                        {
                            if (Path.GetExtension(imgFiles[currImgInd]) == ImgExtensions.GIF)
                            {
                                //ImageBehavior.SetAnimatedSource(MainViewControl.MainImage, img);
                                //GifSlider.Visibility = Visibility.Visible;

                                AnimationBehavior.SetSourceUri(MainViewControl.MainImage, new Uri(imgFiles[currImgInd]));
                                //AnimationBehavior.AddLoadedHandler(MainViewControl.MainImage, gifLoadedHandler);
                            }
                            else
                            {
                               

                                MainViewControl.MainImage.Source = MainViewControl.ImgSrc;
                            }

                            MainViewControl.ApplyDimensions();
                            imageDoneLoading = true;

                        }catch (Exception e)
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
                //image.CreateOptions = BitmapCreateOptions.DelayCreation;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.Rotation = rotation;
                image.EndInit();

                image.Freeze();

                return image;
            }else
            {
                var image = new BitmapImage();

                image.BeginInit();
                image.UriSource = new Uri(filePath);
                //image.CreateOptions = BitmapCreateOptions.DelayCreation;
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
                string fileToDel = imgFiles[currImgInd];
                File.Delete(fileToDel);

                imgFiles.Remove(fileToDel);

                MoveFoward();
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
                if (imageDoneLoading) MainViewControl.Rotate(90);
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

        private void GifSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //var animator = AnimationBehavior.GetAnimator(MainViewControl.MainImage);
            //if (animator == null) return;

            //animator.Seek((int)GifSlider.Value);
        }
    }
}
