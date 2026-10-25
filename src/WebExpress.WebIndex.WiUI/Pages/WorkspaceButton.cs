namespace WebExpress.WebIndex.WiUI.Pages;

/// <summary>Preserves left aligned labels while making the entire tree or table cell interactive.</summary>
internal sealed class WorkspaceButton : Button
{
    /// <summary>Shares pointer feedback with the compact workspace controls.</summary>
    public WorkspaceButton() => Style = (Style)Application.Current!.Resources["CompactButton"];

    /// <summary>Aligns the native content without narrowing the clickable surface.</summary>
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button button)
            button.HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
    }
}
