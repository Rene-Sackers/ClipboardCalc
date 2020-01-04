using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipboardCalc
{
    class Hook
    {
        // API's
        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

        // Event
        public delegate void ClipboardChangedHandler();
        public event ClipboardChangedHandler ClipboardChanged;

        public void StartHook(Window mainWindow)
        {
            var handle = new WindowInteropHelper(mainWindow).Handle;
            _nextViewer = SetClipboardViewer(handle);
            var hwndSource = HwndSource.FromHwnd(handle);

            if (hwndSource != null)
            {
                hwndSource.AddHook(HookProcedure);
            } else
            {
                MessageBox.Show("Failed to hook to clipboard.");
            }
        }

        private IntPtr _nextViewer;
        private int _lastCopy;
        IntPtr HookProcedure(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref Boolean handled)
        {
            switch (msg)
            {
                case 0x2: //Getting this message will kill the hook, ignore it to prevent that (handled = True).
                    handled = true;
                    break;
                case 0x308: //0x308 = WM_DRAWCLIPBOARD2
                    if (Environment.TickCount - _lastCopy >= 250)
                    {
                        _lastCopy = Environment.TickCount;
                        ClipboardChanged();
                    }
                    break;
                case 0x30D: //0x30D = WM_CHANGECBCHAIN
                    if (wParam == _nextViewer)
                    {
                        _nextViewer = lParam;
                    }
                    else
                    {
                        SendMessage(_nextViewer, (uint)msg, wParam, lParam);
                    }
                    break;
            }

            return IntPtr.Zero;
        }
    }
}
