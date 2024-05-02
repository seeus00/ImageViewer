using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ImageViewer.UIElements
{
    public partial class ViewerControl : UserControl
    {
        public BitmapImage ImgSrc { get; set; }

        private Point originPos;
        private Point workPoint1;
        private Point workPoint2;

        private Point? mousePosition;

        private double[] viewMatrix;
        private double[] invMatrix;

        private double scale = 1;

        private double currAngle = 0;

        private Rect imgBounds;

        private bool dirty = true;

        private double windowWidth;
        private double windowHeight;


        private double windowWidthOffset;
        private double windowHeightOffset;

        private bool isSaving = false;
        private bool alreadyRotated = false;
        

        public ViewerControl()
        {
            InitializeComponent();

            viewMatrix = new double[] { 1, 0, 0, 1, 0, 0 };
            invMatrix = new double[] { 1, 0, 0, 1, 0, 0 };

            ImgSrc = new BitmapImage();
            
            //MainImage.LayoutUpdated += MainImage_LayoutUpdated;
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

        private void Update()
        {
            dirty = false;

            for (int i = 0; i < 10; i++)
            {
                var m = viewMatrix;

                m[3] = m[0] = scale;
                m[1] = m[2] = 0;
                m[4] = originPos.X;
                m[5] = originPos.Y;

                Constrain();
            }
        }

        public void ResetZoom()
        {
            //originPos = new Point(0, 0);
            //workPoint1 = new Point(0, 0);
            //workPoint2 = new Point(0, 0);

            var m = viewMatrix;

            double maxScale = Math.Min(windowWidth / (imgBounds.Right - imgBounds.Left),
                windowHeight / (imgBounds.Bottom - imgBounds.Top));

            m[0] = m[3] = scale = maxScale;
            m[1] = m[2] = 0;
        }

        private void Constrain()
        {
            double width = windowWidth;
            double height = windowHeight;

         

            //double maxScale = Math.Min(width / (imgBounds.Right - imgBounds.Left),
            //    height / (imgBounds.Bottom - imgBounds.Top));

            var m = viewMatrix;

            //if (scale < maxScale) m[0] = m[3] = scale = maxScale;

            m[0] = m[3] = scale;
            m[1] = m[2] = 0;

            workPoint1.X = imgBounds.Left;
            workPoint1.Y = imgBounds.Top;

            workPoint2 = ToScreen(workPoint1);


            if (workPoint2.X > 0) { m[4] = (originPos.X -= workPoint2.X); }
            if (workPoint2.Y > 0) { m[5] = (originPos.Y -= workPoint2.Y); }



            workPoint1.X = imgBounds.Right;
            workPoint1.Y = imgBounds.Bottom;

            workPoint2 = ToScreen(workPoint1);


           

            if (workPoint2.X < width) { m[4] = (originPos.X -= (workPoint2.X - width)) / 2f; }
            if (workPoint2.Y < height) { m[5] = (originPos.Y -= (workPoint2.Y - height)) / 2f; }
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


        public void Rotate(double angle)
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow == null || ImgSrc == null) return;


             //Debug.WriteLine($"{MainImage.ActualWidth}, {MainImage.ActualHeight}");
            imgBounds = new Rect(0, 0, ImgSrc.Width, ImgSrc.Height);

            //Measured in degrees
            currAngle = (currAngle + angle) % 360;

            //originPos = new Point(0, 0);
            SwapImgDims();

            //Rotate entire canvas (rotating just the image causes massive aids)
            var rotTransform = new RotateTransform(currAngle, originPos.X, originPos.Y);
            LayoutTransform = rotTransform;
            UpdateLayout();


            ResetZoom();

            dirty = true;
            Apply();
        }

        private void ScaleAt(Point at, double amt)
        {
            if (dirty) Update();
            scale *= amt;

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

        private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = (IInputElement)sender;
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
        }

        private Point lastMousePos;
        private Point currMousePos;


        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            var canvas = (IInputElement)sender;
            var p = e.GetPosition(canvas);

            lastMousePos = currMousePos;
            currMousePos = new Point(p.X, p.Y);


            if (mousePosition.HasValue)
            {
                var dx = currMousePos.X - lastMousePos.X;
                var dy = currMousePos.Y - lastMousePos.Y;

                Move(dx, dy);
                Apply();
            }

        }


        public void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var canvas = (IInputElement)sender;

            Window parentWindow = Window.GetWindow(this);
            var p = e.GetPosition(parentWindow);



            //p = new Point(windowWidth / 2, windowHeight / 2);

            //p.X += ImgSrc.Width / 2f;
            //p.Y += ImgSrc.Height / 2f;



            ScaleAt(new Point(windowWidth / 2f, windowHeight / 2f), Math.Exp((e.Delta / 150f) * 0.17f));
            //ScaleAt(p, Math.Exp((e.Delta / 120f) * 0.15f));

            //var centerX = scaledWinCoords.X / 2f;
            //var centerY = scaledWinCoords.Y / 2f;
            //if (e.Delta < 0)
            //{
            //    ScaleAt(new Point(centerX, centerY), Math.Exp((e.Delta / 120f) * 0.2f));
            //}
            //else
            //{
            //    ScaleAt(new Point(centerX, centerY), Math.Exp((e.Delta / 120f) * 0.2f));
            //}


            //TODO: Calculate right offset to fit image (dirty hack for now)
            //Refresh screen to fit image


            Apply();
        }

        public void Reset()
        {
            originPos = new Point(0, 0);
            workPoint1 = new Point(0, 0);
            workPoint2 = new Point(0, 0);

          
            currAngle = 0;
            LayoutTransform = Transform.Identity;
            UpdateLayout();

            dirty = true;
            Apply();
        }


        public void ApplyDimensions(bool reset = true, bool apply = true)
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow == null || ImgSrc == null) return;
           

            //Debug.WriteLine($"{MainImage.ActualWidth}, {MainImage.ActualHeight}");
            imgBounds = new Rect(0, 0, ImgSrc.Width, ImgSrc.Height);

            if (reset)
            {
                Reset();
            }



            //If rotated, switch the width and height of the canvas window
            if (currAngle == 90 || currAngle == 270)
            {
                double canvasWidth = ActualHeight;
                double canvasHeight = ActualWidth;



                windowWidthOffset = parentWindow.ActualWidth - canvasWidth;
                windowHeightOffset = canvasHeight - canvasHeight;

                windowWidth = parentWindow.ActualWidth + windowWidthOffset;
                windowHeight = canvasHeight + windowHeightOffset;


                SwapImgDims();
            }
            else
            {
                windowWidthOffset = parentWindow.ActualWidth - ActualWidth;
                windowHeightOffset = ActualHeight - ActualHeight;

                windowWidth = parentWindow.ActualWidth + windowWidthOffset;
                windowHeight = ActualHeight + windowHeightOffset;
            }







            dirty = true;
            //scale = 1;

            //viewMatrix = new double[] { 1, 0, 0, 1, 0, 0 };
            //invMatrix = new double[] { 1, 0, 0, 1, 0, 0 };
            ResetZoom();
            Apply();

        }

        public void SaveImage(string filePath)
        {
            if (isSaving || ImgSrc == null) return;

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


            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(myRotatedBitmapSource));
                encoder.Save(fileStream);
            }



            isSaving = false;

        }
    }
}
