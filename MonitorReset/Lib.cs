using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibMR
{
    public class Gdi
    {
        public class Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public int Width { get => right - left; }
            public int Height { get => bottom - top; }

            public Rect()
            {

            }

            public Rect(PInvoke.RECT rect)
            {
                this.left = rect.left;
                this.top = rect.top;
                this.right = rect.right;
                this.bottom = rect.bottom;
            }

            override public string ToString()
            {
                return string.Format("⇱<{2},{0}>, ⇲<{3},{1}>", top, bottom, left, right);
            }
        }

        public class DisplayInfo
        {
            public string Flags { get; set; }
            public Rect Monitor { get; set; }
            public Rect WorkArea { get; set; }
        }

        public static Rect GetVirtualScreen()
        {
            Rect rect = new();

            rect.top = 0;
            rect.left = 0;
            rect.right = PInvoke.User32.GetSystemMetrics(PInvoke.User32.SystemMetric.SM_CXVIRTUALSCREEN);
            rect.bottom = PInvoke.User32.GetSystemMetrics(PInvoke.User32.SystemMetric.SM_CYVIRTUALSCREEN);

            return rect;
        }

        public static List<DisplayInfo> GetDisplays()
        {
            List<DisplayInfo> col = new();
            unsafe
            {
                _ = PInvoke.User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    delegate (IntPtr hMonitor, IntPtr hdcMonitor, PInvoke.RECT* lprcMonitor, void* dwData)
                    {
                        PInvoke.User32.MONITORINFO mi = new();
                        mi.cbSize = Marshal.SizeOf(mi);
                        bool ret = PInvoke.User32.GetMonitorInfo(hMonitor, ref mi);
                        if (ret)
                        {
                            DisplayInfo display = new();
                            display.Flags = mi.dwFlags switch
                            {
                                PInvoke.User32.MONITORINFO_Flags.MONITORINFOF_PRIMARY => "primary",
                                _ => "none"
                            };
                            display.WorkArea = new(mi.rcWork);
                            display.Monitor = new(mi.rcMonitor);
                            col.Add(display);
                        }
                        return true;
                    },
                    IntPtr.Zero);
            }

            return col;
        }

        public static void MoveWindowToPrimary(Proc.ProcWindow dat)
        {
            _ = PInvoke.User32.SetWindowPos(dat.Hwnd, IntPtr.Zero, 10, 10, 0, 0, PInvoke.User32.SetWindowPosFlags.SWP_NOSIZE);
        }
    }

    public class Proc
    {
        public class ProcWindow
        {
            public IntPtr Hwnd { get; set; }
            public string Name { get; set; }
            public int ProcId { get; set; }
            public int ThreadId { get; set; }
            public Gdi.Rect Window { get; set; }
        }

        public static List<ProcWindow> GetProcWindowCollection()
        {
            List<ProcWindow> col = new();

            PInvoke.User32.EnumWindows(
                delegate (IntPtr hwnd, IntPtr lParam)
                {
                    PInvoke.User32.WINDOWINFO info = new();
                    info.cbSize = (uint)Marshal.SizeOf(info);
                    var ret = PInvoke.User32.GetWindowInfo(hwnd, ref info);
                    if (ret && (info.dwStyle & (uint)PInvoke.User32.WindowStyles.WS_VISIBLE) != 0)
                    {
                        int threadId = PInvoke.User32.GetWindowThreadProcessId(hwnd, out int procId);
                        var proc = new ProcWindow
                        {
                            Hwnd = hwnd,
                            Name = PInvoke.User32.GetWindowText(hwnd),
                            ProcId = procId,
                            ThreadId = threadId,
                            Window = new(info.rcWindow)
                        };
                        col.Add(proc);
                    }
                    return true;
                }, IntPtr.Zero);

            return col;
        }
    }
}
