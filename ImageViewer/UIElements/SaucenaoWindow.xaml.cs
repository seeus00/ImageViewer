using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageViewer
{
    /// <summary>
    /// Interaction logic for SaucenaoWindow.xaml
    /// </summary>
    public partial class SaucenaoWindow : Window
    {
        public string Html { get; set; }
        private string currentUri;

        public SaucenaoWindow()
        {
            InitializeComponent();

            MainWebView.NavigationStarting += MainWebView_NavigationStarting;
        }

        private void MainWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (string.IsNullOrEmpty(currentUri))
            {
                currentUri = e.Uri;
            }else if (currentUri != e.Uri)
            {
                Debug.WriteLine(e.Uri);
                e.Cancel = true;

                Process.Start(new ProcessStartInfo { FileName = e.Uri, UseShellExecute = true });
                currentUri = e.Uri;
            }

        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            try
            {
                var webView2Environment = await CoreWebView2Environment.CreateAsync();
                await MainWebView.EnsureCoreWebView2Async(webView2Environment);

                if (!string.IsNullOrEmpty(Html))
                {
                    MainWebView.NavigateToString(Html);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
    }
}
