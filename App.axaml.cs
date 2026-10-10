using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using StageDeck.Core;

namespace StageDeck;

public partial class App : Application
{
    private StageManager? _manager;
    private HotKeyLoop? _hotkey;
    private SideBarWindow? _sidebar;
    private StatusWindow? _status;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Settings.Load();
            _manager = new StageManager();

            _sidebar = new SideBarWindow(_manager);
            desktop.MainWindow = _sidebar;

            _sidebar.Opened += (_, _) =>
            {
                _manager.SetOurHwnd(_sidebar.GetHwnd());
            };

            _status = new StatusWindow();
            _status.SetSidebar(_sidebar);
            _status.SetQuitAction(() => ShutdownApp());
            _status.Show();
            _hotkey = new HotKeyLoop();
            _hotkey.OnHotKey += (id) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (id == HotKeyLoop.HK_TOGGLE)
                        {
                            _manager.Toggle();
                            _status?.UpdateStatus(_manager.IsActive, 0);
                        }
                        else if (id == HotKeyLoop.HK_QUIT)
                        {
                            ShutdownApp();
                        }
                        else if (id == HotKeyLoop.HK_SIZE_UP)
                        {
                            _sidebar?.ResizeCards(1.1);
                        }
                        else if (id == HotKeyLoop.HK_SIZE_DOWN)
                        {
                            _sidebar?.ResizeCards(0.9);
                        }
                        else if (id == HotKeyLoop.HK_SETTINGS)
                        {
                            _status?.Show();
                            _status?.Activate();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StageDeck] hotkey error: {ex}");
                    }
                });
            };
            _hotkey.Start();

            // 启动后自动开启台前调度
            Dispatcher.UIThread.Post(() =>
            {
                _manager.Toggle();
                _status?.UpdateStatus(_manager.IsActive, 0);
            }, DispatcherPriority.Background);

            Console.WriteLine("[StageDeck] Started. Ctrl+Alt+S toggle, Ctrl+Alt+Q quit.");
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void ShutdownApp()
    {
        try { _manager?.ExitStage(); } catch { }
        _hotkey?.Stop();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            d.Shutdown();
    }
}
