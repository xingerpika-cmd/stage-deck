using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using StageDeck.Core;
using StageDeck.Native;

namespace StageDeck;

public partial class SideBarWindow : Window
{
    private readonly StageManager _manager;
    private readonly Dictionary<IntPtr, IntPtr> _thumbnails = new();
    private List<WindowInfo> _currentWindows = new();

    private int CardWidth = Settings.CardSize;
    private int CardHeight => (int)(CardWidth * 0.7);
    private const int CardGap = 6;
    private const int CardsTop = 42;
    private const int CardMarginX = 6;
    private int ThumbHeight => (int)(CardWidth * 0.53);

    public void ResizeCards(double factor)
    {
        CardWidth = (int)(CardWidth * factor);
        ApplySize();
        Console.WriteLine($"[SideBar] Resized: {CardWidth}x{CardHeight}, thumb={ThumbHeight}");
        LayoutThumbnails();
    }

    public void SetCardSize(int width)
    {
        CardWidth = width;
        Settings.CardSize = width;
        Settings.Save();
        ApplySize();
        LayoutThumbnails();
    }

    private void ApplySize()
    {
        var hwnd = GetHwnd();
        if (hwnd != IntPtr.Zero)
        {
            User32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, CardWidth + 16,
                User32.GetSystemMetrics(User32.SM_CYSCREEN),
                User32.SWP_NOACTIVATE | User32.SWP_SHOWWINDOW | User32.SWP_NOMOVE);
        }
    }

    private DispatcherTimer? _mouseTimer;
    private bool _barVisible;

    public SideBarWindow(StageManager manager)
    {
        _manager = manager;
        InitializeComponent();

        int sw = User32.GetSystemMetrics(User32.SM_CXSCREEN);
        int sh = User32.GetSystemMetrics(User32.SM_CYSCREEN);
        Position = new PixelPoint(0, 0);
        Width = CardWidth + 16;
        Height = sh;

        _manager.OnWindowsChanged += OnWindowsChanged;
        _manager.OnStateChanged += OnStateChanged;
    }

    public IntPtr GetHwnd() => TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

    private void OnStateChanged()
    {
        try
        {
            if (_manager.IsActive)
            {
                Show();
                RenderTransform = new TranslateTransform(0, 0);
                int sh = User32.GetSystemMetrics(User32.SM_CYSCREEN);
                var hwnd = GetHwnd();
                if (hwnd != IntPtr.Zero)
                {
                    User32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, CardWidth + 16, sh,
                        User32.SWP_NOACTIVATE | User32.SWP_SHOWWINDOW);
                }
                StartMouseWatch();
            }
            else
            {
                StopMouseWatch();
                Hide();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SideBar] OnStateChanged error: {ex.Message}");
        }
    }

    private void StartMouseWatch()
    {
        StopMouseWatch();
        _mouseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _mouseTimer.Tick += (_, _) =>
        {
            User32.GetCursorPos(out var pt);
            int barW = CardWidth + 16;
            var hwnd = GetHwnd();
            if (hwnd == IntPtr.Zero) return;

            if (pt.X <= 3 && !_barVisible)
            {
                _barVisible = true;
                Topmost = true;
                User32.SetWindowPos(hwnd, User32.HWND_TOPMOST, 0, 0, barW,
                    User32.GetSystemMetrics(User32.SM_CYSCREEN),
                    User32.SWP_NOACTIVATE | User32.SWP_SHOWWINDOW | User32.SWP_NOMOVE);
            }
            else if (_barVisible && pt.X > barW + 50)
            {
                _barVisible = false;
                Topmost = false;
                User32.SetWindowPos(hwnd, User32.HWND_BOTTOM, 0, 0, barW,
                    User32.GetSystemMetrics(User32.SM_CYSCREEN),
                    User32.SWP_NOACTIVATE | User32.SWP_SHOWWINDOW | User32.SWP_NOMOVE);
            }
        };
        _mouseTimer.Start();
    }

    private void StopMouseWatch()
    {
        _mouseTimer?.Stop();
        _mouseTimer = null;
    }

    private void OnWindowsChanged(List<WindowInfo> windows)
    {
        _currentWindows = windows;
        Dispatcher.UIThread.Post(LayoutThumbnails);
    }

    private void LayoutThumbnails()
    {
        try
        {
            foreach (var thumb in _thumbnails.Values)
                DwmApi.DwmUnregisterThumbnail(thumb);
            _thumbnails.Clear();
            CardsPanel.Children.Clear();

            IntPtr destHwnd = GetHwnd();
            int idx = 0;

            // 垂直居中：计算卡片总高度
            int count = _currentWindows.Count;
            int totalH = count * CardHeight + Math.Max(0, count - 1) * CardGap;
            int screenH = User32.GetSystemMetrics(User32.SM_CYSCREEN);
            int centerTop = (screenH - totalH) / 2;
            if (centerTop < 10) centerTop = 10;
            CardsPanel.Margin = new Thickness(8, centerTop, 8, 12);

            foreach (var info in _currentWindows)
            {
                var normalBrush = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                var hoverBrush = new SolidColorBrush(Color.FromArgb(0xA0, 0x4A, 0x9E, 0xFF));
                var card = new Border
                {
                    Width = CardWidth,
                    Height = CardHeight,
                    CornerRadius = new CornerRadius(8),
                    Background = Brushes.Transparent,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)),
                    BorderThickness = new Thickness(1),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new Border { Height = ThumbHeight, Background = Brushes.Transparent },
                            new Border {
                                Background = new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)),
                                Child = new TextBlock { Text = info.Title, Foreground = new SolidColorBrush(Color.FromArgb(0xE0, 0xFF,0xFF,0xFF)), FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(8,4,8,4) }
                            }
                        }
                    }
                };

                // DWM 缩略图
                IntPtr thumbId = IntPtr.Zero;
                try
                {
                    if (DwmApi.DwmRegisterThumbnail(destHwnd, info.Handle, out thumbId) == 0)
                    {
                        _thumbnails[info.Handle] = thumbId;
                        int top = centerTop + idx * (CardHeight + CardGap);
                        var normalRect = new RECT { Left = CardMarginX, Top = top, Right = CardMarginX + CardWidth, Bottom = top + ThumbHeight };

                        var ptp = new DWM_THUMBNAIL_PROPERTIES
                        {
                            dwFlags = DwmApi.DWM_TNP_RECTDESTINATION | DwmApi.DWM_TNP_VISIBLE | DwmApi.DWM_TNP_OPACITY,
                            rcDestination = normalRect,
                            opacity = 220,
                            fVisible = 1,
                            fSourceClientAreaOnly = 1,
                        };
                        DwmApi.DwmUpdateThumbnailProperties(thumbId, ref ptp);

                        // hover 放大动画：从 normalRect 渐变到 hoverRect
                        var hoverRect = new RECT
                        {
                            Left = normalRect.Left - 6,
                            Top = normalRect.Top - 6,
                            Right = normalRect.Right + 6,
                            Bottom = normalRect.Bottom + 6
                        };

                        DispatcherTimer? animTimer = null;
                        void AnimateRect(RECT from, RECT to, int durationMs)
                        {
                            animTimer?.Stop();
                            var t0 = Environment.TickCount;
                            animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                            animTimer.Tick += (_, _) =>
                            {
                                double t = Math.Min(1.0, (double)(Environment.TickCount - t0) / durationMs);
                                double eased = t * t * (3 - 2 * t);
                                var r = new RECT
                                {
                                    Left = (int)(from.Left + (to.Left - from.Left) * eased),
                                    Top = (int)(from.Top + (to.Top - from.Top) * eased),
                                    Right = (int)(from.Right + (to.Right - from.Right) * eased),
                                    Bottom = (int)(from.Bottom + (to.Bottom - from.Bottom) * eased),
                                };
                                var p = new DWM_THUMBNAIL_PROPERTIES
                                {
                                    dwFlags = DwmApi.DWM_TNP_RECTDESTINATION,
                                    rcDestination = r,
                                    opacity = (byte)(220 + (255 - 220) * eased),
                                    fVisible = 1,
                                    fSourceClientAreaOnly = 1,
                                };
                                DwmApi.DwmUpdateThumbnailProperties(thumbId, ref p);
                                if (t >= 1) animTimer.Stop();
                            };
                            animTimer.Start();
                        }

                        card.PointerEntered += (_, _) =>
                        {
                            card.BorderBrush = hoverBrush;
                            AnimateRect(normalRect, hoverRect, 150);
                        };
                        card.PointerExited += (_, _) =>
                        {
                            card.BorderBrush = normalBrush;
                            AnimateRect(hoverRect, normalRect, 150);
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SideBar] DWM thumb failed: {ex.Message}");
                }

                var target = info;
                card.PointerPressed += (_, e) =>
                {
                    Console.WriteLine($"[SideBar] Card clicked: '{target.Handle:X}' '{target.Title}'");
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    {
                        _manager.SwitchTo(target.Handle);
                    }
                };

                CardsPanel.Children.Add(card);
                idx++;
            }
            Console.WriteLine($"[SideBar] LayoutThumbnails: {_currentWindows.Count} cards");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SideBar] LayoutThumbnails error: {ex.Message}");
        }
    }
}
