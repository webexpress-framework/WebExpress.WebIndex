using WebExpress.WebIndex.WiUI.Localization;

namespace WebExpress.WebIndex.WiUI.Pages;

/// <summary>Uses native desktop menus and dialogs to retain keyboard and pointer accessibility.</summary>
internal static class WorkspaceDialogs
{
    /// <summary>Anchors actions to their menu button instead of opening a confirmation immediately.</summary>
    /// <param name="anchor">The visible menu button.</param>
    /// <param name="actions">The available localized commands.</param>
    /// <returns>The chosen command, or null when dismissed.</returns>
    public static Task<string?> Menu(Button anchor, params string[] actions)
    {
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var menu = new Microsoft.UI.Xaml.Controls.MenuFlyout();
        foreach (var action in actions)
        {
            var item = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = action };
            item.Click += (_, _) => completion.TrySetResult(action);
            menu.Items.Add(item);
        }
        menu.Closed += (_, _) => completion.TrySetResult(null);
        menu.ShowAt((Microsoft.UI.Xaml.FrameworkElement)anchor.Handler!.PlatformView!);
        return completion.Task;
    }

    /// <summary>Highlights the primary action consistently in native confirmation dialogs.</summary>
    /// <param name="owner">The page owning the dialog.</param>
    /// <param name="title">The localized dialog title.</param>
    /// <param name="message">The consequences or informational content.</param>
    /// <param name="accept">The primary action caption.</param>
    /// <param name="cancel">The optional dismissal caption.</param>
    /// <returns>Whether the primary action was selected.</returns>
    public static async Task<bool> Confirm(Page owner, string title, string message, string accept, string? cancel = null)
    {
        var dialog = CreateDialog(owner, title, accept, cancel ?? "");
        dialog.Content = new Microsoft.UI.Xaml.Controls.TextBlock { Text = message, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap, MaxWidth = 440 };
        return await dialog.ShowAsync() == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary;
    }

    /// <summary>Retains existing names until a highlighted save action is accepted.</summary>
    /// <param name="owner">The page owning the dialog.</param>
    /// <param name="title">The localized title.</param>
    /// <param name="message">The field label.</param>
    /// <param name="initialValue">The current reference name.</param>
    /// <returns>The entered name, or null when canceled.</returns>
    public static async Task<string?> Prompt(Page owner, string title, string message, string initialValue)
    {
        var input = new Microsoft.UI.Xaml.Controls.TextBox { Header = message, Text = initialValue, MinWidth = 360 };
        var dialog = CreateDialog(owner, title, AppText.RenameLink, AppText.Cancel);
        dialog.Content = input;
        return await dialog.ShowAsync() == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary ? input.Text : null;
    }

    /// <summary>Shares native dialog ownership and primary action appearance across the workspace.</summary>
    /// <param name="owner">The page owning the native window.</param>
    /// <param name="title">The localized title.</param>
    /// <param name="accept">The primary action caption.</param>
    /// <param name="cancel">The dismissal caption.</param>
    /// <returns>The themed dialog ready for its content.</returns>
    private static Microsoft.UI.Xaml.Controls.ContentDialog CreateDialog(Page owner, string title, string accept, string cancel)
    {
        var window = (Microsoft.UI.Xaml.Window)owner.Window.Handler!.PlatformView!;
        var primary = new Microsoft.UI.Xaml.Style { TargetType = typeof(Microsoft.UI.Xaml.Controls.Button) };
        primary.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.BackgroundProperty, new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 122, 204))));
        primary.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.ForegroundProperty, new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))));
        primary.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.CornerRadiusProperty, new Microsoft.UI.Xaml.CornerRadius(0)));
        primary.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.MinHeightProperty, 32d));
        var secondary = new Microsoft.UI.Xaml.Style { TargetType = typeof(Microsoft.UI.Xaml.Controls.Button) };
        secondary.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.BackgroundProperty, new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 51, 55))));
        secondary.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.ForegroundProperty, new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 212, 212, 212))));
        secondary.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.CornerRadiusProperty, new Microsoft.UI.Xaml.CornerRadius(0)));
        secondary.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.MinHeightProperty, 32d));
        return new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            XamlRoot = window.Content.XamlRoot, RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark,
            Title = title, PrimaryButtonText = accept, CloseButtonText = cancel,
            PrimaryButtonStyle = primary, CloseButtonStyle = secondary, DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.None
        };
    }

    /// <summary>Collects project options before any index files are created.</summary>
    /// <param name="owner">The page owning the native dialog.</param>
    /// <returns>The validated name and sample option, or null when canceled.</returns>
    public static async Task<(string Name, bool Sample)?> NewProject(Page owner)
    {
        var name = new Microsoft.UI.Xaml.Controls.TextBox { Header = AppText.ProjectName, PlaceholderText = AppText.MyIndexes, MinWidth = 360 };
        var sample = new Microsoft.UI.Xaml.Controls.CheckBox { Content = AppText.IncludeSampleData };
        var content = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 16 };
        content.Children.Add(name);
        content.Children.Add(sample);
        var dialog = CreateDialog(owner, AppText.NewProjectTitle, AppText.CreateProject, AppText.Cancel);
        dialog.Content = content;
        dialog.IsPrimaryButtonEnabled = false;
        name.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(name.Text);
        var result = await dialog.ShowAsync();
        return result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary ? (name.Text.Trim(), sample.IsChecked == true) : null;
    }
}
