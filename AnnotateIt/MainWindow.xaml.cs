using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace AnnotateIt
{
    /// <summary>
    /// Represents the primary transparent, borderless overlay window for Annotate It.
    /// Acts as the root input-capturing surface above the Windows desktop.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private IntPtr _windowHandle;
        private AppMode _currentMode = AppMode.Drawing;
        private bool _isDraggingPanel;
        private Point _panelDragStartOffset;
        private DispatcherTimer? _passThroughHitCheckTimer;
        private bool _isPassThroughActive;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current interaction state of the overlay (Drawing, Eraser, or PassThrough).
        /// Automatically updates the UI and underlying OS window styles when changed.
        /// </summary>
        public AppMode CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
                ApplyModeState(_currentMode);
            }
        }

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
            ConfigurePrimaryScreenBounds();
            InitializePassThroughTimer();
        }

        #endregion

        #region Lifecycle & Window Setup

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            ApplyModeState(_currentMode);
        }

        private void ConfigurePrimaryScreenBounds()
        {
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
        }

        private void InitializePassThroughTimer()
        {
            // Lightweight 30ms timer active ONLY during PassThrough mode to detect cursor over panel
            _passThroughHitCheckTimer = new DispatcherTimer(DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            _passThroughHitCheckTimer.Tick += PassThroughHitCheckTimer_Tick;
        }

        private void ApplyModeState(AppMode mode)
        {
            if (_windowHandle == IntPtr.Zero) return;

            UpdateButtonStyles();

            if (mode == AppMode.PassThrough)
            {
                // Start tracking cursor proximity to panel and enable pass-through by default
                _passThroughHitCheckTimer?.Start();
                SetPassThroughState(true);
            }
            else
            {
                // Stop pass-through timer and ensure window captures all clicks
                _passThroughHitCheckTimer?.Stop();
                SetPassThroughState(false);
            }
        }

        private void SetPassThroughState(bool enablePassThrough)
        {
            if (_isPassThroughActive != enablePassThrough)
            {
                _isPassThroughActive = enablePassThrough;
                NativeMethods.SetClickThrough(_windowHandle, enablePassThrough);
            }
        }

        /// <summary>
        /// Checks if the mouse cursor is over the Control Panel while in PassThrough mode.
        /// If over the panel, disables WS_EX_TRANSPARENT so the user can click buttons/drag.
        /// If outside, re-enables WS_EX_TRANSPARENT so clicks pass to background desktop apps.
        /// </summary>
        private void PassThroughHitCheckTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentMode != AppMode.PassThrough || _isDraggingPanel) return;

            if (NativeMethods.GetCursorPos(out var pt))
            {
                Point wpfScreenPoint = new Point(pt.X, pt.Y);
                Point windowPoint = PointFromScreen(wpfScreenPoint);

                double panelLeft = Canvas.GetLeft(ControlPanelContainer);
                double panelTop = Canvas.GetTop(ControlPanelContainer);
                var panelRect = new Rect(panelLeft, panelTop, ControlPanelContainer.ActualWidth, ControlPanelContainer.ActualHeight);

                bool isOverPanel = panelRect.Contains(windowPoint);

                // If over the panel, do NOT pass through (so user can interact with the panel)
                SetPassThroughState(!isOverPanel);
            }
        }

        #endregion

        #region Control Panel Dragging

        private void PanelHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is IInputElement headerElement)
            {
                _isDraggingPanel = true;
                _panelDragStartOffset = e.GetPosition(ControlPanelContainer);
                headerElement.CaptureMouse();
                e.Handled = true;
            }
        }

        private void PanelHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingPanel)
            {
                Point cursorInCanvas = e.GetPosition(OverlayCanvas);

                double newLeft = cursorInCanvas.X - _panelDragStartOffset.X;
                double newTop = cursorInCanvas.Y - _panelDragStartOffset.Y;

                newLeft = Math.Clamp(newLeft, 0, OverlayCanvas.ActualWidth - ControlPanelContainer.ActualWidth);
                newTop = Math.Clamp(newTop, 0, OverlayCanvas.ActualHeight - ControlPanelContainer.ActualHeight);

                Canvas.SetLeft(ControlPanelContainer, newLeft);
                Canvas.SetTop(ControlPanelContainer, newTop);

                e.Handled = true;
            }
        }

        private void PanelHeader_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingPanel)
            {
                _isDraggingPanel = false;
                if (sender is IInputElement headerElement)
                {
                    headerElement.ReleaseMouseCapture();
                }
                e.Handled = true;
            }
        }

        #endregion

        #region Control Panel Tool Actions

        private void PenButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            CurrentMode = AppMode.Drawing;
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            CurrentMode = AppMode.Eraser;
        }

        private void PassThroughButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            CurrentMode = AppMode.PassThrough;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            for (int i = OverlayCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (OverlayCanvas.Children[i] != ControlPanelContainer)
                {
                    OverlayCanvas.Children.RemoveAt(i);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Close();
        }

        private void UpdateButtonStyles()
        {
            var activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89B4FA"));
            var inactiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#313244"));
            var activeText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11111B"));
            var inactiveText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CDD6F4"));

            if (FindName("PenToolButton") is Button penBtn)
            {
                penBtn.Background = (_currentMode == AppMode.Drawing) ? activeBrush : inactiveBrush;
                penBtn.Foreground = (_currentMode == AppMode.Drawing) ? activeText : inactiveText;
            }

            if (FindName("EraserToolButton") is Button eraserBtn)
            {
                eraserBtn.Background = (_currentMode == AppMode.Eraser) ? activeBrush : inactiveBrush;
                eraserBtn.Foreground = (_currentMode == AppMode.Eraser) ? activeText : inactiveText;
            }

            if (FindName("PassThroughButton") is Button passBtn)
            {
                passBtn.Background = (_currentMode == AppMode.PassThrough) ? activeBrush : inactiveBrush;
                passBtn.Foreground = (_currentMode == AppMode.PassThrough) ? activeText : inactiveText;
            }
        }

        #endregion

        #region Overlay Input Handlers

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource != OverlayCanvas && e.OriginalSource != this)
            {
                return;
            }

            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.D:
                    CurrentMode = AppMode.Drawing;
                    break;
                case Key.E:
                    CurrentMode = AppMode.Eraser;
                    break;
                case Key.P:
                case Key.Space:
                    CurrentMode = AppMode.PassThrough;
                    break;
            }
        }

        #endregion
    }
}
