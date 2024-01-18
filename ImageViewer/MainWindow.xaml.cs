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
using XamlAnimatedGif;
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

namespace ImageViewer
{
    public partial class MainWindow : Window
    {

        private int currImgInd;
        private List<string>? imgFiles;

        private Dictionary<string, bool> loadedImgs;

        private bool imageDoneLoading = false;

        private CancellationTokenSource? cts;

        public MainWindow()
        {
            InitializeComponent();


            loadedImgs = new Dictionary<string, bool>();
            MouseWheel += MainViewControl.CanvasMouseWheel;
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

            await ConfigManager.InitConfig();

            //If default extensions not set, set them (ask for admin privledges)
            if (ConfigManager.GetValue("default_ext") == null)
            {
                if (!IsRunningAsAdministrator())
                {
                    string startingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
                    string nameOfProgram = typeof(MainWindow).Assembly.GetName().Name + ".exe";

                    // Setting up start info of the new process of the same application
                    ProcessStartInfo processStartInfo = new ProcessStartInfo()
                    {
                        FileName = $"{startingPath}/{nameOfProgram}"
                    };

                    // Using operating shell and setting the ProcessStartInfo.Verb to “runas” will let it run as admin
                    processStartInfo.UseShellExecute = true;
                    processStartInfo.Verb = "runas";

                    // Start the application as new process
                    try
                    {
                        Process.Start(processStartInfo);
                    }catch (Exception exc) 
                    { 
                        Debug.WriteLine(exc.StackTrace); 
                    }
                    finally
                    {
                        // Shut down the current (old) process
                        Application.Current.Shutdown();
                    }
                    
                    return;
                }else if (ConfigManager.GetValue("default_ext") == null)
                {
                    DefaultFileExtensionManager.SetDefaultFileExtensions();

                    ConfigManager.CONFIG_INFO["default_ext"] = "yes";
                    await ConfigManager.WriteConfig();
                }
            }

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length < 2 || !IsValidImgFile(args[1]))
            {
                MessageBox.Show("An image file was not loaded or was not valid", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

                if (Application.Current == null) return;
                Application.Current.Shutdown();

                return;
            }
            else
            {
                imgFiles = Win32Util.GetAllFilesFromExplorer(args[1]);
                if (!imgFiles.Any())
                {
                    MessageBox.Show("An image file was not loaded or was not valid", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

                    if (Application.Current == null) return;
                    Application.Current.Shutdown();

                    return;
                }


                imgFiles = imgFiles.Where(imgFile => IsValidImgFile(imgFile)).ToList();
                currImgInd = imgFiles.IndexOf(args[1]);

                SetMainImage();
                MainViewControl.ResetZoom();
            }
        }

        private async void SetMainImage()
        {
            imageDoneLoading = false;

            //If an image is already loading, then cancel that previous image so that we can load the current image
            if (cts != null) cts.Cancel();


            //Generate thumbnail to show something while main image loads
            if (!loadedImgs.ContainsKey(imgFiles[currImgInd]) || !loadedImgs[imgFiles[currImgInd]])
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var thumbnail = Win32Util.ExtractThumbnail(imgFiles[currImgInd], new System.Drawing.Size(50, 50), SIIGBF.SIIGBF_THUMBNAILONLY);
                    MainViewControl.ImgSrc = Win32Util.ToBitmapImage(thumbnail);

                    MainViewControl.MainImage.Source = MainViewControl.ImgSrc;

                    MainViewControl.ApplyDimensions();
                    MainViewControl.ResetZoom();
                    MainViewControl.Reset();
                });
            }
            


            cts = new CancellationTokenSource();

            await Task.Run(() =>
            {
                MainViewControl.ImgSrc = GetBitmapImage(imgFiles[currImgInd]);

                //If the user wants a new image to be loaded, then stop loading the current image
                //Useful for when the user wants to sift through multiple images quickly, and doesn't want to wait for one image to load before going to the next
                if (cts == null || cts.IsCancellationRequested)
                {
                    cts = null;
                    return;
                }

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    MainViewControl.MainImage.Source = MainViewControl.ImgSrc;

                    MainViewControl.ApplyDimensions();
                    MainViewControl.ResetZoom();
                    MainViewControl.Reset();


                    imageDoneLoading = true;
                    loadedImgs[imgFiles[currImgInd]] = true;


                    //For gifs
                    if (Path.GetExtension(imgFiles[currImgInd]) == ImgExtensions.GIF) 
                        AnimationBehavior.SetSourceUri(MainViewControl.MainImage, new Uri(imgFiles[currImgInd]));

                    string fileName = Path.GetFileName(imgFiles[currImgInd]);
                    Title = $"{fileName} - [{currImgInd + 1}/{imgFiles.Count}]";

                }, System.Windows.Threading.DispatcherPriority.Background);

                cts = null;
            });
        }

        private bool IsValidImgFile(string filePath) => ImgExtensions.SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(filePath));

        private BitmapImage GetBitmapImage(string filePath)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;

            var imgUri = new Uri(filePath);
            image.UriSource = imgUri;

            image.EndInit();
            image.Freeze();

            return image;
        }

        private void MoveBack() => currImgInd = currImgInd - 1 < 0 ? 0 : currImgInd - 1;
        private void MoveFoward() => currImgInd = currImgInd + 1 >= imgFiles.Count ? imgFiles.Count - 1 : currImgInd + 1;

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            MoveBack();
            SetMainImage();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MoveFoward();
            SetMainImage();
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
            switch (e.Key)
            {
                case Key.D:
                    MoveFoward();
                    SetMainImage();
                    break;
                case Key.A:
                    MoveBack();
                    SetMainImage();
                    break;
                case Key.Right:
                    MoveFoward();
                    SetMainImage();
                    break;
                case Key.Left:
                    MoveBack();
                    SetMainImage();
                    break;
                case Key.Delete:
                    DeletePrompt();
                    break;

            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!imageDoneLoading) return;

            MainViewControl.ApplyDimensions();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.SavePlacement();
        }
    }
}
