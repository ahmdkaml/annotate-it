using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnnotateIt
{
    /// <summary>
    /// Represents the primary transparent, borderless overlay window for Annotate It.
    /// Acts as the root ink-capturing surface above the Windows desktop.
    /// </summary>
    public partial class MainWindow : Window
    {
        private Cursor? _laserGlowCursor;
        private IntPtr _windowHandle;
        private AppMode _currentMode = AppMode.Drawing;
        private ControlPanelWindow? _controlPanel;

        private Color _selectedColor = (Color)ColorConverter.ConvertFromString("#F38BA8");
        private readonly double _penSize = 3.5;
        private readonly double _highlighterSize = 16.0;

        /// <summary>
        /// Gets or sets the active interaction mode for the overlay surface.
        /// </summary>
        public AppMode CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
                ApplyModeState(_currentMode);
                _controlPanel?.UpdateActiveButtonStyles();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            ConfigurePrimaryScreenBounds();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _laserGlowCursor = CreateLaserGlowCursor();

            _windowHandle = new WindowInteropHelper(this).Handle;

            // Wire auto-cleanup for ephemeral laser pointer strokes
            OverlayCanvas.StrokeCollected += OverlayCanvas_StrokeCollected;

            // Instantiate and display the independent floating control panel window
            _controlPanel = new ControlPanelWindow(this);
            _controlPanel.Show();

            ApplyModeState(_currentMode);
        }

        private void ConfigurePrimaryScreenBounds()
        {
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
        }

        public void SetColor(Color color)
        {
            _selectedColor = color;
            ApplyModeState(_currentMode);
        }

        public void ClearStrokes()
        {
            OverlayCanvas.Strokes.Clear();
        }

        /// <summary>
        /// Renders an intense glowing red halo aura with a crisp center dot into a custom Windows cursor.
        /// </summary>
        private Cursor CreateLaserGlowCursor()
        {
            int size = 32;
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var center = new Point(16, 16);

                // Outer wide vibrant glow aura
                dc.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(90, 255, 30, 30)),
                    null,
                    center, 15, 15);

                // Mid-layer intense glow ring
                dc.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(170, 255, 45, 45)),
                    null,
                    center, 9, 9);

                // Inner bright bloom
                dc.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(220, 255, 80, 80)),
                    null,
                    center, 5.5, 5.5);

                // Crisp solid center core dot
                dc.DrawEllipse(
                    new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    new Pen(new SolidColorBrush(Color.FromRgb(255, 20, 20)), 1.2),
                    center, 2.5, 2.5);
            }

            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var pngStream = new MemoryStream();
            encoder.Save(pngStream);
            byte[] pngBytes = pngStream.ToArray();

            // Construct standard .cur format memory stream (Hotspot at center 16, 16)
            using var curStream = new MemoryStream();
            using var writer = new BinaryWriter(curStream);

            writer.Write((short)0);             // Reserved
            writer.Write((short)2);             // Type: Cursor (2)
            writer.Write((short)1);             // Image count
            writer.Write((byte)size);           // Width
            writer.Write((byte)size);           // Height
            writer.Write((byte)0);              // Color count
            writer.Write((byte)0);              // Reserved
            writer.Write((short)16);            // Hotspot X
            writer.Write((short)16);            // Hotspot Y
            writer.Write((int)pngBytes.Length); // Image data byte size
            writer.Write((int)22);              // Offset to image data

            writer.Write(pngBytes);
            writer.Flush();
            curStream.Position = 0;

            return new Cursor(curStream);
        }

        private void ApplyModeState(AppMode mode)
        {
            if (_windowHandle == IntPtr.Zero) return;

            bool isPassThrough = (mode == AppMode.PassThrough);
            NativeMethods.SetClickThrough(_windowHandle, isPassThrough);
            if (mode == AppMode.LaserPointer && _laserGlowCursor != null)
            {
                OverlayCanvas.UseCustomCursor = true;
                OverlayCanvas.Cursor = _laserGlowCursor;
            }
            else
            {
                OverlayCanvas.UseCustomCursor = false;
                OverlayCanvas.Cursor = Cursors.Arrow;
            }
            switch (mode)
            {
                case AppMode.Drawing:
                    OverlayCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    OverlayCanvas.DefaultDrawingAttributes = new DrawingAttributes
                    {
                        Color = _selectedColor,
                        Width = _penSize,
                        Height = _penSize,
                        FitToCurve = true,
                        IgnorePressure = false,
                        IsHighlighter = false,
                        StylusTip = StylusTip.Ellipse
                    };
                    break;

                case AppMode.Highlighter:
                    OverlayCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    OverlayCanvas.DefaultDrawingAttributes = new DrawingAttributes
                    {
                        Color = Color.FromArgb(120, _selectedColor.R, _selectedColor.G, _selectedColor.B),
                        Width = _highlighterSize,
                        Height = _highlighterSize * 1.5,
                        FitToCurve = true,
                        IgnorePressure = true,
                        IsHighlighter = true,
                        StylusTip = StylusTip.Rectangle
                    };
                    break;

                case AppMode.LaserPointer:
                    OverlayCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    OverlayCanvas.DefaultDrawingAttributes = new DrawingAttributes
                    {
                        // Core laser line: bright, thin, solid
                        Color = Color.FromRgb(255, 60, 60),
                        Width = 4,
                        Height = 4,
                        FitToCurve = true,
                        IgnorePressure = false,
                        IsHighlighter = false,
                        StylusTip = StylusTip.Ellipse
                    };
                    break;
                case AppMode.PointEraser:
                    OverlayCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                    OverlayCanvas.EraserShape = new EllipseStylusShape(16, 16);
                    break;

                case AppMode.StrokeEraser:
                    OverlayCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    break;

                case AppMode.PassThrough:
                default:
                    OverlayCanvas.EditingMode = InkCanvasEditingMode.None;
                    break;
            }
        }

        private async void OverlayCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (_currentMode != AppMode.LaserPointer) return;

            var stroke = e.Stroke;



            // Gradually decrease stroke alpha
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(100);
                byte currentA = stroke.DrawingAttributes.Color.A;
                if (currentA > 50)
                {
                    var c = stroke.DrawingAttributes.Color;
                    stroke.DrawingAttributes.Color = Color.FromArgb((byte)(currentA - 45), c.R, c.G, c.B);
                }
            }

            // Remove expired stroke from canvas
            OverlayCanvas.Strokes.Remove(stroke);
        }

        public void ForwardKeyDown(Key key)
        {
            switch (key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.P:
                    CurrentMode = AppMode.Drawing;
                    break;
                case Key.H:
                    CurrentMode = AppMode.Highlighter;
                    break;
                case Key.L:
                    CurrentMode = AppMode.LaserPointer;
                    break;
                case Key.E:
                    CurrentMode = AppMode.PointEraser;
                    break;
                case Key.S:
                    CurrentMode = AppMode.StrokeEraser;
                    break;
                case Key.Space:
                case Key.Tab:
                    CurrentMode = AppMode.PassThrough;
                    break;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            ForwardKeyDown(e.Key);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _laserGlowCursor?.Dispose();
            _controlPanel?.Close();
        }
    }
}
