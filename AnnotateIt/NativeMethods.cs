using System;
using System.Runtime.InteropServices;

namespace AnnotateIt
{
    /// <summary>
    /// Provides Win32 interop definitions to toggle OS-level mouse click-through behavior.
    /// </summary>
    internal static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Enables or disables OS-level mouse click-through for the specified window handle.
        /// </summary>
        /// <param name="hWnd">The target window handle.</param>
        /// <param name="enablePassThrough">True to let clicks pass through; false to capture clicks.</param>
        public static void SetClickThrough(IntPtr hWnd, bool enablePassThrough)
        {
            if (hWnd == IntPtr.Zero) return;

            int currentStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if (enablePassThrough)
            {
                SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            }
            else
            {
                SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle & ~WS_EX_TRANSPARENT);
            }
        }
    }
}
