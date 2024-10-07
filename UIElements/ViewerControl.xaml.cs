using ImageViewer.Util;
using ImageViewer.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
//using XamlAnimatedGif;

namespace ImageViewer.UIElements
{
    public partial class ViewerControl : UserControl
    {
        public double MinSize;
        public double ViewOffset { get { return MinSize / 4; } }

        public BitmapImage ImgSrc { get; set; }

        public string ImgPath { get; set; }

        private Point originPos;

        private Point? mousePosition;

        private double[] viewMatrix;
        private double[] invMatrix;

        private double[] previewMatrix;

        private double previewScale = 1;
        private double scale = 1;
        private double maxScale = 1;

        public int CurrRot { get; private set; } = 1;
        private int currAngle { get; set; }

        private Rect imgBounds;
        private Rect previewBounds;

        private bool dirty = true;

        private double windowWidth;
        private double windowHeight;

        private double windowWidthOffset;
        private double windowHeightOffset;

        private bool isSaving = false;

        private bool isDragging = false;
        private bool zoomedIn = false;

        private Rectangle previewRectangleOuter;
        private Rectangle previewRectangleInner;
        


        public ViewerControl()
        {
            InitializeComponent();

            viewMatrix = new double[] { 1, 0, 0, 1, 0, 0 };
            invMatrix = new double[] { 1, 0, 0, 1, 0, 0 };
            previewMatrix = new double[] { 1, 0, 0, 1, 0, 0 };

            ImgSrc = new BitmapImage();
        }

        private Point ToScreen(Point from)
        {
            if (dirty) Update();

            return new Point()
            {
                X = from.X * viewMatrix[0] + from.Y * viewMatrix[2] + viewMatrix[4],
                Y = from.X * viewMatrix[1] + from.Y * viewMatrix[3] + viewMatrix[5],
            };
        }

        private void Apply()
        {
            if (dirty) Update();

            ((MatrixTransform)MainImage.RenderTransform).Matrix = new Matrix(viewMatrix[0], viewMatrix[1], viewMatrix[2], viewMatrix[3],
                 viewMatrix[4] - windowWidthOffset, viewMatrix[5] - windowHeightOffset);


        }
        enum ScaleOptions
        {
            KEEP_ASPECT_RATIO,
            EXPAND_ASPECT_RATIO
        }

        private Size ScaleSize(Size size, double scaleX, double scaleY, ScaleOptions scaleOption)
        {
            var scaledSize = new Size(scaleX, scaleY);

            double ratioX = (double)scaledSize.Width / (double)size.Width;
            double ratioY = (double)scaledSize.Height / (double)size.Height;

            switch (scaleOption)
            {
                case ScaleOptions.KEEP_ASPECT_RATIO:
                    ratioX = ratioY = Math.Min(ratioX, ratioY);
                    break;
                case ScaleOptions.EXPAND_ASPECT_RATIO:
                    ratioX = ratioY = Math.Max(ratioX, ratioY);
                    break;
            }

            int newWidth = Convert.ToInt32(size.Width * ratioX);
            int newHeight = Convert.ToInt32(size.Height * ratioY);
 
            return new Size(newWidth, newHeight);
        }


        private void ConstrainOuterPreviewRect()
        {
            previewRectangleInner.Visibility = Visibility.Visible;
            previewRectangleOuter.Visibility = Visibility.Visible;

            var size = new Size(imgBounds.Width, imgBounds.Height);
            size = ScaleSize(size, MinSize, MinSize, ScaleOptions.EXPAND_ASPECT_RATIO);

            var docViewRect = new Rect(0, 0, previewRectangleOuter.Width, previewRectangleOuter.Height);
            int maxBevHeight = (int)Math.Abs(docViewRect.Height - 2 * ViewOffset);
            int maxBevWidth = (int)Math.Abs(docViewRect.Width - 2 * ViewOffset);

            if (size.Height > maxBevHeight)
            {
                size = ScaleSize(size, MinSize, maxBevHeight, ScaleOptions.KEEP_ASPECT_RATIO);
            }
            if (size.Width > maxBevWidth)
            {
                size = ScaleSize(size, maxBevWidth, MinSize, ScaleOptions.KEEP_ASPECT_RATIO);
            }


            var newRect = new Rect(0, 0, size.Width, size.Height);
            double xp = 0;
            double w = newRect.Width;
            double yp = 0;
            double h = newRect.Height;

            int xmin = (int)Math.Floor(xp);
            int xmax = (int)Math.Ceiling(xp + w);
            int ymin = (int)(Math.Floor(yp));
            int ymax = (int)(Math.Ceiling(yp + h));
            var alignedRect = new Rect(xmin, ymin, xmax - xmin, ymax - ymin);



            previewRectangleOuter.Width = alignedRect.Width;
            previewRectangleOuter.Height = alignedRect.Height;
            Canvas.SetLeft(previewRectangleOuter, 0);
            Canvas.SetTop(previewRectangleOuter, 0);

            UpdatePreviewRotation(previewRectangleOuter, new Rect());
        }

        private void ConstrainInnerPreviewRect()
        {
            var docSize = new Size(previewBounds.Width, previewBounds.Height);
            double bevZoom = 0;

            if (docSize.Height > docSize.Width)
            {
                bevZoom = previewRectangleOuter.Height / docSize.Height;
            }
            else
            {
                bevZoom = previewRectangleOuter.Width / docSize.Width;
            }


            var minDocSize = new Size(windowWidth / scale, windowHeight / scale);
            var minSize = new Point(Math.Min(minDocSize.Width, docSize.Width), Math.Min(minDocSize.Height, docSize.Height));
            minSize.X *= bevZoom;
            minSize.Y *= bevZoom;

            double newX = -originPos.X, newY = -originPos.Y;
            if (newX < 0) newX = 0;
            if (newY < 0) newY = 0;

            var newRect = new Rect(new Point(newX / scale * bevZoom, newY / scale * bevZoom), new Size(minSize.X, minSize.Y));

            previewRectangleInner.Width = newRect.Width;
            previewRectangleInner.Height = newRect.Height;
            Canvas.SetLeft(previewRectangleInner, newRect.X);
            Canvas.SetTop(previewRectangleInner, newRect.Y);


            UpdatePreviewRotation(previewRectangleInner, newRect);
        }

        private void Update()
        {
            dirty = false;

            Constrain();

            var m = viewMatrix;

            m[3] = m[0] = scale;
            m[1] = m[2] = 0;
            m[4] = originPos.X;
            m[5] = originPos.Y;

            if (previewRectangleInner == null || previewRectangleOuter == null) return;
            if (!zoomedIn)
            {
                previewRectangleInner.Visibility = Visibility.Hidden;
                previewRectangleOuter.Visibility = Visibility.Hidden;
                return;
            }
         
            previewRectangleInner.Visibility = Visibility.Visible;
            previewRectangleOuter.Visibility = Visibility.Visible;

            ConstrainInnerPreviewRect();            
        }

        public void ResetZoom()
        {
            var m = viewMatrix;

            maxScale = Math.Min(windowWidth / (imgBounds.Right - imgBounds.Left),
                windowHeight / (imgBounds.Bottom - imgBounds.Top));

            m[0] = m[3] = scale = maxScale;
            m[1] = m[2] = 0;
        }

        //Shamelessly copied from here: https://github.com/xyb3rt/sxiv/blob/master/image.c#L366
        private void Constrain()
        {
            double maxScale = Math.Min(windowWidth / (imgBounds.Right - imgBounds.Left),
                windowHeight / (imgBounds.Bottom - imgBounds.Top));

            if (double.IsNaN(maxScale)) return;

            decimal decScale = Math.Truncate(100 * (decimal)scale) / 100;
            decimal decMaxScale = Math.Truncate(100 * (decimal)maxScale) / 100;

            zoomedIn = decScale > decMaxScale;

            double imgWidth = imgBounds.Right * scale;
            double imgHeight = imgBounds.Bottom * scale;

            if (imgWidth < windowWidth) originPos.X = (windowWidth - imgWidth) / 2;
            else if (originPos.X > 0) originPos.X = 0;
            else if (originPos.X + imgWidth < windowWidth) originPos.X = windowWidth - imgWidth;

            if (imgHeight < windowHeight) originPos.Y = (windowHeight - imgHeight) / 2;
            else if (originPos.Y > 0) originPos.Y = 0;
            else if (originPos.Y + imgHeight < windowHeight) originPos.Y = windowHeight - imgHeight;
        }

        private void SwapImgDims()
        {
            //Change width to height and vice versa because when you rotate an image, the dimensions get swapped
            var temp = windowHeight;
            windowHeight = windowWidth;
            windowWidth = temp;

            //Swap offsets
            temp = windowHeightOffset;
            windowHeightOffset = windowWidthOffset;
            windowWidthOffset = temp;
        }


        //Since we're rotating the entire canvas, we want to adjust the preview window so that it goes in the top left by
        //offsetting the top and left coordinates
        private void UpdatePreviewRotation(Rectangle previewRect, Rect newRect)
        {
            switch (currAngle)
            {
                case 0:
                    Canvas.SetLeft(previewRect, newRect.X);
                    Canvas.SetTop(previewRect, newRect.Y);
                    break;
                case 90:
                    Canvas.SetLeft(previewRect, newRect.X);
                    Canvas.SetTop(previewRect, windowHeight - previewRectangleOuter.Height - windowHeightOffset * 2 + newRect.Y);
                    break;
                case 180:
                    Canvas.SetLeft(previewRect, windowWidth - previewRectangleOuter.Width - windowWidthOffset * 2 + newRect.X);
                    Canvas.SetTop(previewRect, windowHeight - previewRectangleOuter.Height - windowHeightOffset * 2 + newRect.Y);
                    break;
                case 270:
                    Canvas.SetLeft(previewRect, windowWidth - previewRectangleOuter.Width - windowWidthOffset * 2 + newRect.X);
                    Canvas.SetTop(previewRect, newRect.Y);
                    break;
            }
        }

        public void Rotate()
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow == null || ImgSrc == null) return;

            currAngle = (currAngle + 90) % 360;
            SwapImgDims(); //Swaps the width and height

            //Rotate entire canvas (rotating just the image causes massive aids)
            var rotTransform = new RotateTransform(currAngle, originPos.X, originPos.Y);
            MainCanvas.LayoutTransform = rotTransform;
            MainCanvas.UpdateLayout();
            ResetZoom();

            UpdatePreviewRotation(previewRectangleOuter, new Rect());

            dirty = true;
            Apply();
        }

        private void ScaleAt(Point at, double amt)
        {
            if (dirty) Update();
            scale *= amt;
            previewScale *= amt;

            originPos.X = at.X - (at.X - originPos.X) * amt;
            originPos.Y = at.Y - (at.Y - originPos.Y) * amt;

            dirty = true;
        }


        private void Move(double x, double y)
        {
            originPos.X += (int)x * 1.5f;
            originPos.Y += (int)y * 1.5f;

            dirty = true;
        }

        Point lastOriginPos;

        private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = (IInputElement)sender;

            var origEle = ((FrameworkElement)e.OriginalSource);
            previewRectClicked = previewRectangleInner == origEle;
            lastOriginPos = originPos;
            if (canvas.CaptureMouse())
            {
                mousePosition = e.GetPosition(canvas);
            }
        }

        private void CanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var canvas = (IInputElement)sender;
            canvas.ReleaseMouseCapture();
            mousePosition = null;

            previewRectClicked = false;
            Win32Util.ClipCursor(IntPtr.Zero);
        }

        private Point lastMousePos;
        private Point currMousePos;


        private bool previewRectClicked = false;

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            var canvas = (IInputElement)sender;
            var p = e.GetPosition(canvas);

            lastMousePos = currMousePos;
            currMousePos = new Point(p.X, p.Y);

            if (previewRectangleInner != null && previewRectClicked && mousePosition.HasValue)
            {
                //Handle preview movement (bird eye view)
                double ratio = Math.Min(windowHeight / previewRectangleInner.Height, windowWidth / previewRectangleInner.Width);
                double moveX = lastOriginPos.X + (currMousePos.X - mousePosition.Value.X) * -1 * ratio;
                double moveY = lastOriginPos.Y + (currMousePos.Y - mousePosition.Value.Y) * -1 * ratio;

                originPos.X = moveX;
                originPos.Y = moveY;

                dirty = true;
                Apply();
            }
            else
            {
                if (mousePosition.HasValue)
                {
                    var dx = currMousePos.X - lastMousePos.X;
                    var dy = currMousePos.Y - lastMousePos.Y;

                    Move(dx, dy);
                    Apply();
                }

                if (zoomedIn && mousePosition != null && lastMousePos != currMousePos) Mouse.SetCursor(Cursors.Hand);
            }
        }


        public void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //This code zooms to the center
            //ScaleAt(new Point(windowWidth / 2f, windowHeight / 2f), Math.Exp((e.Delta / 150f) * 0.17f));
            ScaleAt(currMousePos, Math.Exp((e.Delta / 120f) * 0.15f));

            Apply();
        }

        public void Reset()
        {
            originPos = new Point(0, 0);

            currAngle = 0;

            MainCanvas.LayoutTransform = Transform.Identity;
            UpdateLayout();

            dirty = true;
            Apply();
        }

        private readonly DrawingVisual visual = new DrawingVisual();
        private MediaElement gifMediaElement = new MediaElement();

        private void OnTimerTick(object sender, EventArgs e)
        {
            var width = gifMediaElement.NaturalVideoWidth;
            var height = gifMediaElement.NaturalVideoHeight;

            RenderTargetBitmap bitmap = null;

            if (width > 0 && height > 0)
            {
                if (bitmap == null ||
                    bitmap.PixelWidth != width ||
                    bitmap.PixelHeight != height)
                {
                    using (var dc = visual.RenderOpen())
                    {
                        dc.DrawRectangle(
                            new VisualBrush(gifMediaElement), null,
                            new Rect(0, 0, width, height));
                    }

                    bitmap = new RenderTargetBitmap(
                    width, height, 96, 96, PixelFormats.Default);
                }

                bitmap.Render(visual);
            }
        }

        public void LoadGif(string path)
        {
            var mediaEle = new MediaElement();
            mediaEle.Source = new Uri(path);
            mediaEle.Play();
       
        }

        public void ApplyDimensions(bool reset = true, bool resetZoom = true, bool apply = true)
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow == null || ImgSrc == null) return;
           
            imgBounds = new Rect(0, 0, ImgSrc.Width, ImgSrc.Height);
            previewBounds = new Rect(0, 0, ImgSrc.Width, ImgSrc.Height);
           
            if (reset)
            {
                Reset();
            }

            windowWidthOffset = parentWindow.ActualWidth - ActualWidth;
            windowHeightOffset = ActualHeight - ActualHeight;

            windowWidth = parentWindow.ActualWidth + windowWidthOffset;
            windowHeight = ActualHeight + windowHeightOffset;


            //If rotated, switch the width and height of the canvas window
            if (currAngle == 90 || currAngle == 270)
            {
                SwapImgDims();
            }

            if (previewRectangleInner != null || previewRectangleOuter != null)
            {
                MainCanvas.Children.Remove(previewRectangleInner);
                MainCanvas.Children.Remove(previewRectangleOuter);
            }

            MinSize = (windowWidth + windowHeight) / 6;


            previewRectangleOuter = new Rectangle();
            previewRectangleOuter.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            previewRectangleOuter.StrokeThickness = 1;
            previewRectangleOuter.Fill = new ImageBrush()
            {
                ImageSource = ImgSrc,
                Opacity = 0.5
            };

            previewRectangleOuter.Width = 0;
            previewRectangleOuter.Height = 0;
            MainCanvas.Children.Add(previewRectangleOuter);

            previewRectangleInner = new Rectangle();
            previewRectangleInner.RenderTransform = new MatrixTransform();
            previewRectangleInner.Fill = new SolidColorBrush(Color.FromArgb(100, 184, 6, 20)); ;
            MainCanvas.Children.Add(previewRectangleInner);

            dirty = true;

            ConstrainOuterPreviewRect();

            if (resetZoom) ResetZoom();
            Apply();

        }


        public void SaveImage(string filePath)
        {
            if (isSaving || ImgSrc == null) return;

            BitmapEncoder? encoder = null;
            string fileExt = System.IO.Path.GetExtension(ImgPath);
            switch (fileExt)
            {
                case ".jpg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;

            }
            if (encoder == null) return;

            isSaving = true;

            TransformedBitmap myRotatedBitmapSource = new TransformedBitmap();


            // BitmapSource objects like TransformedBitmap can only have their properties
            // changed within a BeginInit/EndInit block.
            myRotatedBitmapSource.BeginInit();

            // Use the BitmapSource object defined above as the source for this BitmapSource.
            // This creates a "chain" of BitmapSource objects which essentially inherit from each other.
            myRotatedBitmapSource.Source = ImgSrc;

            // Set rotation of transform to bitmapimage
            myRotatedBitmapSource.Transform = LayoutTransform;
            myRotatedBitmapSource.EndInit();

            myRotatedBitmapSource.Freeze();

            using var fileStream = new FileStream(filePath, FileMode.Create);

            encoder.Frames.Add(BitmapFrame.Create(myRotatedBitmapSource));
            encoder.Save(fileStream);


            isSaving = false;

        }

        private volatile int nrOfTouchPoints;
        private object mutex = new object();

        private void MainCanvas_TouchDown(object sender, TouchEventArgs e)
        {
            lock (mutex)
            {
                nrOfTouchPoints++;
            }
        }

        private void MainCanvas_TouchUp(object sender, TouchEventArgs e)
        {
            lock (mutex)
            {
                nrOfTouchPoints--;
            }
        }

        private void MainCanvas_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            int nrOfPoints = 0;

            lock (mutex)
            {
                nrOfPoints = nrOfTouchPoints;
            }

            if (nrOfPoints >= 2)
            {
                var matrix = MainImage.LayoutTransform.Value;

                Point? centerOfPinch = (e.ManipulationContainer as FrameworkElement)?.TranslatePoint(e.ManipulationOrigin, MainCanvas);

                if (centerOfPinch == null)
                {
                    return;
                }

                var deltaManipulation = e.DeltaManipulation;
                Point? originOfManipulation = (e.ManipulationContainer as FrameworkElement)?.TranslatePoint(e.ManipulationOrigin, MainImage);

                double delta = deltaManipulation.Scale.X;
                Debug.WriteLine(delta);
                delta = delta < 1 ? -delta : delta;

                ScaleAt(centerOfPinch!.Value, Math.Exp((delta / 4f) * 0.15f));
                Apply();

                e.Handled = true;
            }
            else
            {
                //ScrollViewerParent.ScrollToHorizontalOffset(ScrollViewerParent.HorizontalOffset - e.DeltaManipulation.Translation.X);
                //ScrollViewerParent.ScrollToVerticalOffset(ScrollViewerParent.VerticalOffset - e.DeltaManipulation.Translation.Y);
            }
        }

        private void MainCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = MainCanvas;
            e.Handled = true;
        }
    }
}
