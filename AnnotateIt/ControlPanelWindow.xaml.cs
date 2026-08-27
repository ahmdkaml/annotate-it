using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AnnotateIt
{
    /// <summary>
    /// Represents the floating, draggable tool panel for the Annotate It overlay.
    /// Operates as an independent topmost window to avoid ink capture collisions.
    /// </summary>
    public partial class ControlPanelWindow : Window
    {
        private readonly MainWindow _overlay;
        private bool _isVertical;

        public ControlPanelWindow(MainWindow overlay)
        {
            InitializeComponent();
            _overlay = overlay;
            Owner = overlay;
            UpdateActiveButtonStyles();
        }

        private void PanelHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void PenButton_Click(object sender, RoutedEventArgs e)
        {
            _overlay.CurrentMode = AppMode.Drawing;
            UpdateActiveButtonStyles();
        }

        private void HighlighterButton_Click(object sender, RoutedEventArgs e)
        {
            _overlay.CurrentMode = AppMode.Highlighter;
            UpdateActiveButtonStyles();
        }

        private void LaserPointerButton_Click(object sender, RoutedEventArgs e)
        {
            _overlay.CurrentMode = AppMode.LaserPointer;
            UpdateActiveButtonStyles();
        }

        private void PointEraserButton_Click(object sender, RoutedEventArgs e)
        {
            _overlay.CurrentMode = AppMode.PointEraser;
            UpdateActiveButtonStyles();
        }

        private void StrokeEraserButton_Click(object sender, RoutedEventArgs e)
        {
            _overlay.CurrentMode = AppMode.StrokeEraser;
            UpdateActiveButtonStyles();
        }

        private void PassThroughButton_Click(object sender, RoutedEventArgs e)
        {
            _overlay.CurrentMode = AppMode.PassThrough;
            UpdateActiveButtonStyles();
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                _overlay.SetColor(color);

                string[] swatches = { "ColorPink", "ColorYellow", "ColorGreen", "ColorBlue" };
                foreach (var name in swatches)
                {
                    if (FindName(name) is Button colorBtn)
                    {
                        colorBtn.BorderThickness = (colorBtn == btn) ? new Thickness(2) : new Thickness(0);
                    }
                }

                if (_overlay.CurrentMode != AppMode.Drawing && _overlay.CurrentMode != AppMode.Highlighter)
                {
                    _overlay.CurrentMode = AppMode.Drawing;
                }
                UpdateActiveButtonStyles();
            }
        }

        private void OrientationToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isVertical = !_isVertical;
            Orientation target = _isVertical ? Orientation.Vertical : Orientation.Horizontal;

            ToolbarStackPanel.Orientation = target;
            ColorsStackPanel.Orientation = target;

            if (_isVertical)
            {
                PanelHeader.Width = 36;
                PanelHeader.Height = 22;
                PanelHeader.Margin = new Thickness(0, 0, 0, 4);
                DragGripText.Text = "···";

                Divider1.Width = 22;
                Divider1.Height = 1;
                Divider1.Margin = new Thickness(2, 4, 2, 4);

                Divider2.Width = 22;
                Divider2.Height = 1;
                Divider2.Margin = new Thickness(2, 4, 2, 4);
            }
            else
            {
                PanelHeader.Width = 22;
                PanelHeader.Height = 36;
                PanelHeader.Margin = new Thickness(0, 0, 4, 0);
                DragGripText.Text = "⋮⋮";

                Divider1.Width = 1;
                Divider1.Height = 22;
                Divider1.Margin = new Thickness(4, 2, 4, 2);

                Divider2.Width = 1;
                Divider2.Height = 22;
                Divider2.Margin = new Thickness(4, 2, 4, 2);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) => _overlay.ClearStrokes();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => _overlay.Close();

        public void UpdateActiveButtonStyles()
        {
            var activeBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89B4FA"));
            var inactiveBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#313244"));
            var activeIcon = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11111B"));
            var inactiveIcon = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CDD6F4"));

            void SetStyle(string name, bool active)
            {
                if (FindName(name) is Button btn)
                {
                    btn.Background = active ? activeBg : inactiveBg;
                    if (btn.Content is System.Windows.Shapes.Path path)
                    {
                        path.Fill = active ? activeIcon : inactiveIcon;
                    }
                }
            }

            SetStyle("PenToolButton", _overlay.CurrentMode == AppMode.Drawing);
            SetStyle("HighlighterButton", _overlay.CurrentMode == AppMode.Highlighter);
            SetStyle("LaserPointerButton", _overlay.CurrentMode == AppMode.LaserPointer);
            SetStyle("PointEraserButton", _overlay.CurrentMode == AppMode.PointEraser);
            SetStyle("StrokeEraserButton", _overlay.CurrentMode == AppMode.StrokeEraser);
            SetStyle("PassThroughButton", _overlay.CurrentMode == AppMode.PassThrough);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _overlay.ForwardKeyDown(e.Key);
            UpdateActiveButtonStyles();
        }
    }
}
