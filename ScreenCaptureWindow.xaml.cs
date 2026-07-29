using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AIAssistant;

public partial class ScreenCaptureWindow : Window
{
    private System.Windows.Point _startPoint;
    private bool _isDragging;
    
    public Bitmap? CapturedImage { get; private set; }

    public ScreenCaptureWindow()
    {
        InitializeComponent();
        this.Loaded += ScreenCaptureWindow_Loaded;
    }

    private void ScreenCaptureWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Cover all screens
        this.Left = SystemParameters.VirtualScreenLeft;
        this.Top = SystemParameters.VirtualScreenTop;
        this.Width = SystemParameters.VirtualScreenWidth;
        this.Height = SystemParameters.VirtualScreenHeight;

        canvas.Width = this.Width;
        canvas.Height = this.Height;
        overlay.Width = this.Width;
        overlay.Height = this.Height;

        // Capture screen
        var bmp = new Bitmap((int)this.Width, (int)this.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen((int)this.Left, (int)this.Top, 0, 0, bmp.Size);
        }

        // Set as background image
        screenImage.Source = BitmapToImageSource(bmp);
        
        CapturedImage = bmp;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isDragging = true;
            _startPoint = e.GetPosition(canvas);
            selectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(selectionRect, _startPoint.X);
            Canvas.SetTop(selectionRect, _startPoint.Y);
            selectionRect.Width = 0;
            selectionRect.Height = 0;
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var pos = e.GetPosition(canvas);
            var x = Math.Min(pos.X, _startPoint.X);
            var y = Math.Min(pos.Y, _startPoint.Y);
            var w = Math.Abs(pos.X - _startPoint.X);
            var h = Math.Abs(pos.Y - _startPoint.Y);

            Canvas.SetLeft(selectionRect, x);
            Canvas.SetTop(selectionRect, y);
            selectionRect.Width = w;
            selectionRect.Height = h;
        }
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            
            // Crop the image
            if (selectionRect.Width > 0 && selectionRect.Height > 0 && CapturedImage != null)
            {
                var x = (int)Canvas.GetLeft(selectionRect);
                var y = (int)Canvas.GetTop(selectionRect);
                var w = (int)selectionRect.Width;
                var h = (int)selectionRect.Height;

                var cropRect = new Rectangle(x, y, w, h);
                var croppedBmp = CapturedImage.Clone(cropRect, CapturedImage.PixelFormat);
                
                CapturedImage.Dispose();
                CapturedImage = croppedBmp;
                
                this.DialogResult = true;
            }
            else
            {
                this.DialogResult = false;
            }
            
            this.Close();
        }
    }

    private BitmapImage BitmapToImageSource(Bitmap bitmap)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }
}
