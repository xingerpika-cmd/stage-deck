using System;
using System.Text;
using StageDeck.Native;

namespace StageDeck.Core;

public class WindowInfo
{
    public IntPtr Handle { get; init; }
    public string Title { get; init; } = "";
    public RECT OriginalRect { get; set; }
    public bool IsMinimized { get; set; }
    public bool IsMaximized { get; set; }

    public static WindowInfo? FromHandle(IntPtr hWnd)
    {
        if (!User32.IsWindowVisible(hWnd)) return null;

        long style = User32.GetWindowLongPtrW(hWnd, User32.GWL_STYLE).ToInt64();
        long exStyle = User32.GetWindowLongPtrW(hWnd, User32.GWL_EXSTYLE).ToInt64();

        if ((style & User32.WS_CHILD) != 0) return null;

        int len = User32.GetWindowTextLength(hWnd);
        var title = "";
        if (len > 0)
        {
            var sb = new StringBuilder(len + 1);
            User32.GetWindowText(hWnd, sb, sb.Capacity);
            title = sb.ToString();
        }
        if (string.IsNullOrWhiteSpace(title)) title = "（无标题）";

        if (!User32.GetWindowRect(hWnd, out var rect)) return null;

        bool isMin = User32.IsIconic(hWnd);

        // 最小化窗口的 GetWindowRect 返回 160x28，不能用这个尺寸过滤
        if (!isMin && (rect.Width < 100 || rect.Height < 100))
        {
            Console.WriteLine($"[F small] '{title}' {rect.Width}x{rect.Height}");
            return null;
        }

        // toolwindow 且没有 appwindow
        if ((exStyle & User32.WS_EX_TOOLWINDOW) != 0 && (exStyle & User32.WS_EX_APPWINDOW) == 0)
        {
            Console.WriteLine($"[F toolwin] '{title}' ex=0x{exStyle:X}");
            return null;
        }

        // 有 owner 但没有 appwindow
        if (User32.GetWindow(hWnd, User32.GW_OWNER) != IntPtr.Zero && (exStyle & User32.WS_EX_APPWINDOW) == 0)
        {
            Console.WriteLine($"[F owner] '{title}' ex=0x{exStyle:X}");
            return null;
        }

        try
        {
            int cloaked;
            if (DwmApi.DwmGetWindowAttribute(hWnd, DwmApi.DWMWA_CLOAKED, out cloaked, sizeof(int)) == 0 && cloaked != 0)
            {
                Console.WriteLine($"[F cloaked] '{title}'");
                return null;
            }
        }
        catch { }

        Console.WriteLine($"[OK] '{title}' {rect.Width}x{rect.Height} at {rect.Left},{rect.Top}");
        return new WindowInfo
        {
            Handle = hWnd,
            Title = title,
            OriginalRect = rect,
            IsMinimized = User32.IsIconic(hWnd),
            IsMaximized = User32.IsZoomed(hWnd),
        };
    }
}
