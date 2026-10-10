using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using StageDeck.Native;

namespace StageDeck.Core;

public class StageManager
{
    public double MainScale { get; set; } = 0.78;
    public int SideBarWidth { get; set; } = 140;

    public bool IsActive { get; private set; }

    private IntPtr _mainWindow;
    private readonly List<WindowInfo> _allWindows = new();
    private IntPtr _ourHwnd;
    private IntPtr _eventHook;
    private WinEventDelegate? _eventDelegate;
    private Thread? _watchThread;

    public event Action? OnStateChanged;
    public event Action<List<WindowInfo>>? OnWindowsChanged;

    public void SetOurHwnd(IntPtr hwnd) => _ourHwnd = hwnd;

    public void Toggle()
    {
        if (IsActive) ExitStage();
        else EnterStage();
    }

    public void EnterStage()
    {
        if (IsActive) return;

        _allWindows.Clear();
        var currentPid = Environment.ProcessId;

        User32.EnumWindows((hWnd, _) =>
        {
            if (hWnd == _ourHwnd) return true;

            // 排除自己进程的所有窗口（状态窗、侧栏等）
            User32.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == currentPid) return true;

            var info = WindowInfo.FromHandle(hWnd);
            if (info == null) return true;
            _allWindows.Add(info);
            return true;
        }, IntPtr.Zero);

        // 前台窗口可能是 StageDeck 自己，不在列表里，用列表第一个作为主窗口
        _mainWindow = User32.GetForegroundWindow();
        if (_allWindows.All(w => w.Handle != _mainWindow))
        {
            _mainWindow = _allWindows.Count > 0 ? _allWindows[0].Handle : IntPtr.Zero;
            Console.WriteLine($"[StageDeck] Foreground was our own, picked first in list: 0x{_mainWindow.ToInt64():X}");
        }

        Console.WriteLine($"[StageDeck] Foreground main=0x{_mainWindow.ToInt64():X}, total windows={_allWindows.Count}");

        foreach (var w in _allWindows)
        {
            // 设置窗口圆角
            int corner = DwmApi.DWMWCP_ROUND;
            int hr = DwmApi.DwmSetWindowAttribute(w.Handle, DwmApi.DWMWA_WINDOW_CORNER_PREFERENCE,
                ref corner, sizeof(int));
            Console.WriteLine($"[DWM] Corner on '{w.Title}' -> 0x{hr:X}");

            if (w.Handle == _mainWindow)
            {
                Console.WriteLine($"[StageDeck] Keeping main window '{w.Title}' as-is");
            }
            else
            {
                Console.WriteLine($"[StageDeck] Minimizing '{w.Title}'");
                if (!w.IsMinimized)
                {
                    User32.ShowWindow(w.Handle, User32.SW_MINIMIZE);
                }
            }
        }

        IsActive = true;
        SetDesktopIcons(false);
        OnStateChanged?.Invoke();
        OnWindowsChanged?.Invoke(_allWindows.Where(w => w.Handle != _mainWindow).ToList());
        StartWindowWatch();
        Console.WriteLine($"[StageDeck] Entered, main=0x{_mainWindow.ToInt64():X}, windows={_allWindows.Count}");
    }

    private void StartWindowWatch()
    {
        StopWindowWatch();
        _eventDelegate = OnWinEvent;
        _watchThread = new Thread(() =>
        {
            _eventHook = User32.SetWinEventHook(
                0x8002, 0x8003, // EVENT_OBJECT_SHOW, EVENT_OBJECT_HIDE
                IntPtr.Zero, _eventDelegate, 0, 0,
                0x0002); // WINEVENT_OUTOFCONTEXT
            Console.WriteLine($"[StageDeck] Window watch started, hook=0x{_eventHook.ToInt64():X}");
            // 消息循环
            var msg = new User32.MSG();
            while (User32.GetMessage(out msg, IntPtr.Zero, 0, 0) > 0) { }
        });
        _watchThread.IsBackground = true;
        _watchThread.Start();
    }

    private void StopWindowWatch()
    {
        if (_eventHook != IntPtr.Zero)
        {
            User32.UnhookWinEvent(_eventHook);
            _eventHook = IntPtr.Zero;
        }
    }

    private readonly object _lock = new();

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hWnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (idObject != 0) return;
        if (hWnd == _ourHwnd) return;

        var currentPid = Environment.ProcessId;
        User32.GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == currentPid) return;

        if (eventType == 0x8002) // SHOW
        {
            var info = WindowInfo.FromHandle(hWnd);
            if (info != null)
            {
                lock (_lock)
                {
                    if (_allWindows.Any(w => w.Handle == hWnd)) return;
                    _allWindows.Add(info);
                }
                Console.WriteLine($"[StageDeck] New window: '{info.Title}'");
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    List<WindowInfo> others;
                    lock (_lock)
                        others = _allWindows.Where(w => w.Handle != _mainWindow).ToList();
                    OnWindowsChanged?.Invoke(others);
                });
            }
        }
        else if (eventType == 0x8003) // HIDE
        {
            lock (_lock)
            {
                var existing = _allWindows.FirstOrDefault(w => w.Handle == hWnd);
                if (existing == null) return;
                _allWindows.Remove(existing);
                Console.WriteLine($"[StageDeck] Window closed: '{existing.Title}'");
            }
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                List<WindowInfo> others;
                lock (_lock)
                    others = _allWindows.Where(w => w.Handle != _mainWindow).ToList();
                OnWindowsChanged?.Invoke(others);
            });
        }
    }

    private static IntPtr FindDefView()
    {
        var progman = User32.FindWindow("Progman", null);
        var defView = User32.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (defView == IntPtr.Zero)
        {
            var workerW = User32.FindWindow("WorkerW", null);
            defView = User32.FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
        }
        return defView;
    }

    private static void SetDesktopIcons(bool show)
    {
        var defView = FindDefView();
        if (defView != IntPtr.Zero)
        {
            var listView = User32.FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
            if (listView != IntPtr.Zero)
            {
                User32.ShowWindow(listView, show ? User32.SW_SHOW_NORMAL : User32.SW_HIDE);
                Console.WriteLine($"[StageDeck] Desktop icons {(show ? "shown" : "hidden")}, listview=0x{listView.ToInt64():X}");
            }
            else
            {
                Console.WriteLine("[StageDeck] SysListView32 not found");
            }
        }
        else
        {
            Console.WriteLine("[StageDeck] SHELLDLL_DefView not found");
        }
    }

    public void ExitStage()
    {
        if (!IsActive) return;

        foreach (var w in _allWindows)
        {
            User32.ShowWindow(w.Handle, User32.SW_RESTORE);
        }

        _mainWindow = IntPtr.Zero;
        IsActive = false;
        StopWindowWatch();
        SetDesktopIcons(true);
        OnStateChanged?.Invoke();
        OnWindowsChanged?.Invoke(new List<WindowInfo>());
        Console.WriteLine("[StageDeck] Exited");
    }

    public void SwitchTo(IntPtr targetHwnd)
    {
        if (!IsActive) return;
        if (targetHwnd == _mainWindow) return;

        User32.ShowWindow(_mainWindow, User32.SW_MINIMIZE);
        User32.ShowWindow(targetHwnd, User32.SW_RESTORE);
        User32.SetForegroundWindow(targetHwnd);
        User32.AnimateWindow(targetHwnd, 250, User32.AW_BLEND | User32.AW_ACTIVATE);

        _mainWindow = targetHwnd;

        var others = _allWindows.Where(w => w.Handle != _mainWindow).ToList();
        OnWindowsChanged?.Invoke(others);
        Console.WriteLine($"[StageDeck] Switched to 0x{targetHwnd.ToInt64():X}, others={others.Count}");
    }

    public IntPtr GetMainWindow() => _mainWindow;
}
