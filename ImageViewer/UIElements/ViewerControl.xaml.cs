using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

        private Rect imgBounds;

        private bool dirty = true;

        private double windowWidth;
        private double windowHeight;


        private double windowWidthOffset;
        private double windowHeightOffset;

        public ViewerControl()
        {
            InitializeComponent();

            viewMatrix = new double[] { 1, 0, 0, 1, 0, 0 };
            invMatrix = new double[] { 1, 0, 0, 1, 0, 0 };

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

        private void Update()
        {
            dirty = false;

            for (int i = 0; i < 10; i++)
            {
                var m = viewMatrix;
                var im = invMatrix;

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

            m[0] = m[3] = scale = (maxScale);
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

            workPoint1.X = imgBounds.Left;
            workPoint1.Y = imgBounds.Top;

            workPoint2 = ToScreen(workPoint1);

            if (workPoint2.X > 0) { m[4] = (originPos.X -= workPoint2.X); }
            if (workPoint2.Y > 0) { m[5] = (originPos.Y -= workPoint2.Y); }

            m[4] /= 2f;
            m[5] /= 2f;


            workPoint1.X = imgBounds.Right;
            workPoint1.Y = imgBounds.Bottom;

            workPoint2 = ToScreen(workPoint1);


            if (workPoint2.X < width) { m[4] = (originPos.X -= (workPoint2.X - width)) / 2f; }
            if (workPoint2.Y < height) { m[5] = (originPos.Y -= (workPoint2.Y - height)) / 2f; }
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
            var p = e.GetPosition(MainImage);


            p = new Point(windowWidth, windowHeight);

            //p.X += ImgSrc.Width / 2f;
            //p.Y += ImgSrc.Height / 2f;

            ScaleAt(p, Math.Exp((e.Delta / 120f) * 0.2f));

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

            dirty = true;
            Apply();
        }


        public void ApplyDimensions()
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow == null || ImgSrc == null) return;

            imgBounds = new Rect(0, 0, ImgSrc.Width, ImgSrc.Height);
           
            windowWidthOffset = parentWindow.ActualWidth - MainCanvas.ActualWidth;
            windowHeightOffset = ActualHeight - MainCanvas.ActualHeight;

            windowWidth = parentWindow.ActualWidth + windowWidthOffset;
            windowHeight = ActualHeight + windowHeightOffset;


            //viewMatrix = new double[] { 1, 0, 0, 1, 0, 0 };
            //invMatrix = new double[] { 1, 0, 0, 1, 0, 0 };


            dirty = true;
            //scale = 1;

            ResetZoom();
            Apply();
        }
    }
}
