namespace WebExpress.WebIndex.WiUI;

/// <summary>Creates a desktop workspace sized for side by side index inspection.</summary>
public partial class App : Application
{
    /// <summary>Loads shared application resources.</summary>
    public App() { InitializeComponent(); UserAppTheme = AppTheme.Dark; }

    /// <summary>Creates the main window while preserving the application identity.</summary>
    /// <param name="activationState">The platform activation context.</param>
    /// <returns>The configured desktop window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell()) { Width = 1440, Height = 900, MinimumWidth = 1100, MinimumHeight = 700,
            TitleBar = new TitleBar { Icon = "webindex.png", Title = "WebIndex Explorer", BackgroundColor = Color.FromArgb("#2D2D30"), ForegroundColor = Colors.White } };
    }
}
