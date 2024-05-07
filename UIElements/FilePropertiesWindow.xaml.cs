using ImageViewer.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageViewer.UIElements
{
    /// <summary>
    /// Interaction logic for FilePropertiesWindow.xaml
    /// </summary>
    public partial class FilePropertiesWindow : Window
    {
        private class FileBinding
        {
            public string Header { get; set; }
            public string TextInfo { get; set; }
        }


        private FileInfo fileInfo;
        private Point mousePos;

        public FilePropertiesWindow(FileInfo info, Point initMousePos)
        {
            InitializeComponent();

            fileInfo = info;
            mousePos = initMousePos;

            PreviewKeyDown += new KeyEventHandler(HandleEsc);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableDarkmode.UseImmersiveDarkMode(new WindowInteropHelper(this).Handle, true);
            MoveBottomRightEdgeOfWindowToMousePosition();

            //Disable scrolling on cell click
            var scp = FindVisualChild<ScrollContentPresenter>(FilePropertiesDataGrid);
            scp.RequestBringIntoView += (s, e) => e.Handled = true;

            //Load file properties
            var properties = new List<FileBinding>
            {
                new FileBinding()
                {
                    Header = "Name:",
                    TextInfo = fileInfo.Name
                },
                new FileBinding()
                {
                    Header = "File Size:",
                    TextInfo = (Math.Round(fileInfo.Length / 1000000f, 2)).ToString() + " MB" //Convert bytes to MB
                },
                new FileBinding()
                {
                    Header = "Date created:",
                    TextInfo = fileInfo.CreationTime.ToString()
                },
                new FileBinding()
                {
                    Header = "Path:",
                    TextInfo = fileInfo.FullName
                },
            };

            using var imageStream = File.OpenRead(fileInfo.FullName);
            var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile,
                BitmapCacheOption.Default);

            var width = decoder.Frames[0].PixelWidth;
            var height = decoder.Frames[0].PixelHeight;

            properties.Add(new FileBinding()
            {
                Header = "Dimensions",
                TextInfo = $"{width} x {height}"
            });


            FilePropertiesDataGrid.ItemsSource = properties;

        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            foreach (childItem child in FindVisualChildren<childItem>(obj))
            {
                return child;
            }

            return null;
        }


        private void MoveBottomRightEdgeOfWindowToMousePosition()
        {
            Left = mousePos.X;
            Top = mousePos.Y;
        }


        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
