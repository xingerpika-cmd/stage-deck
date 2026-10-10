using System;
using System.Threading;
using Avalonia.Threading;
using StageDeck.Native;

namespace StageDeck.Core;

public class HotKeyLoop
{
    public const int HK_TOGGLE = 0x9000;
    public const int HK_QUIT = 0x9001;
    public const int HK_SIZE_UP = 0x9002;
    public const int HK_SIZE_DOWN = 0x9003;
    public const int HK_SETTINGS = 0x9004;

    private Thread? _thread;
    private volatile bool _running;
    private bool _lastToggle;
    private bool _lastQuit;
    private bool _lastSizeUp;
    private bool _lastSizeDown;
    private bool _lastSettings;
    private DateTime _lastTrigger = DateTime.MinValue;
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(500);

    public event Action<int>? OnHotKey;

    public void Start()
    {
        _running = true;
        _thread = new Thread(Loop) { IsBackground = true };
        _thread.Start();
        Console.WriteLine("[HotKey] Started polling");
    }

    public void Stop()
    {
        _running = false;
    }

    private void Loop()
    {
        while (_running)
        {
            try
            {
                bool ctrl = (User32.GetAsyncKeyState(0x11) & 0x8000) != 0;
                bool alt = (User32.GetAsyncKeyState(0x12) & 0x8000) != 0;
                bool s = (User32.GetAsyncKeyState(0x53) & 0x8000) != 0;
                bool q = (User32.GetAsyncKeyState(0x51) & 0x8000) != 0;
                bool plus = (User32.GetAsyncKeyState(0xBB) & 0x8000) != 0;
                bool minus = (User32.GetAsyncKeyState(0xBD) & 0x8000) != 0;
                bool o = (User32.GetAsyncKeyState(0x4F) & 0x8000) != 0;

                bool nowToggle = ctrl && alt && s;
                bool nowQuit = ctrl && alt && q;
                bool nowSizeUp = ctrl && alt && plus;
                bool nowSizeDown = ctrl && alt && minus;
                bool nowSettings = ctrl && alt && o;

                if (nowToggle && !_lastToggle && DateTime.Now - _lastTrigger > Debounce)
                {
                    _lastTrigger = DateTime.Now;
                    Console.WriteLine("[HotKey] Toggle detected!");
                    Dispatcher.UIThread.Post(() => OnHotKey?.Invoke(HK_TOGGLE));
                }
                if (nowQuit && !_lastQuit && DateTime.Now - _lastTrigger > Debounce)
                {
                    _lastTrigger = DateTime.Now;
                    Console.WriteLine("[HotKey] Quit detected!");
                    Dispatcher.UIThread.Post(() => OnHotKey?.Invoke(HK_QUIT));
                }
                if (nowSizeUp && !_lastSizeUp && DateTime.Now - _lastTrigger > Debounce)
                {
                    _lastTrigger = DateTime.Now;
                    Dispatcher.UIThread.Post(() => OnHotKey?.Invoke(HK_SIZE_UP));
                }
                if (nowSizeDown && !_lastSizeDown && DateTime.Now - _lastTrigger > Debounce)
                {
                    _lastTrigger = DateTime.Now;
                    Dispatcher.UIThread.Post(() => OnHotKey?.Invoke(HK_SIZE_DOWN));
                }
                if (nowSettings && !_lastSettings && DateTime.Now - _lastTrigger > Debounce)
                {
                    _lastTrigger = DateTime.Now;
                    Dispatcher.UIThread.Post(() => OnHotKey?.Invoke(HK_SETTINGS));
                }

                _lastToggle = nowToggle;
                _lastQuit = nowQuit;
                _lastSizeUp = nowSizeUp;
                _lastSizeDown = nowSizeDown;
                _lastSettings = nowSettings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HotKey] error: {ex.Message}");
            }
            Thread.Sleep(50);
        }
    }
}
