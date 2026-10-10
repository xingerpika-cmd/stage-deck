using Avalonia.Controls;
using Avalonia.Interactivity;
using StageDeck.Core;

namespace StageDeck;

public partial class StatusWindow : Window
{
    private SideBarWindow? _sidebar;
    private System.Action? _onQuit;

    public StatusWindow()
    {
        InitializeComponent();
        HideButton.Click += (_, _) => Hide();
        QuitButton.Click += (_, _) => _onQuit?.Invoke();
        SizeSlider.Value = Settings.CardSize;
        SizeLabel.Text = $"{Settings.CardSize}px";
        SizeSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty && _sidebar != null)
            {
                int size = (int)SizeSlider.Value;
                SizeLabel.Text = $"{size}px";
                _sidebar.SetCardSize(size);
            }
        };
    }

    public void SetSidebar(SideBarWindow sidebar)
    {
        _sidebar = sidebar;
    }

    public void SetQuitAction(System.Action onQuit)
    {
        _onQuit = onQuit;
    }

    public void UpdateStatus(bool isActive, int windowCount)
    {
        StatusText.Text = isActive
            ? $"台前调度已开启 · {windowCount} 个窗口在侧栏"
            : "运行中 · 按 Ctrl+Alt+S 开启";
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty && WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
            Hide();
        }
    }
}
