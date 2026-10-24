using WebExpress.WebIndex.WiUI.Localization;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebExpress.WebIndex.WiUI.Explorer;

namespace WebExpress.WebIndex.WiUI.Pages;

/// <summary>Provides typed field editing and an optional JSON view before committing a document.</summary>
public sealed class DocumentPage : ContentPage
{
    private readonly JsonObject _document;
    private readonly List<(ExplorerField Field, Entry Input, Switch Null)> _fields = [];
    private readonly Editor _json = new() { FontFamily = "Consolas", FontSize = 14, IsVisible = false };
    private readonly ScrollView _form;
    private readonly Label _error = new() { TextColor = Color.FromArgb("#FDA4AF") };
    private readonly Button _save = new() { Text = AppText.SaveDocument };
    private readonly Func<string, Task> _commit;
    private readonly string _original;
    private bool _saving;

    /// <summary>Builds a schema driven form while preserving explicit null and numeric values.</summary>
    /// <param name="title">The editor title.</param>
    /// <param name="json">The original complete document.</param>
    /// <param name="schema">The active index schema.</param>
    /// <param name="existing">Whether the identifier must remain immutable.</param>
    /// <param name="commit">The validated storage operation.</param>
    public DocumentPage(string title, string json, ExplorerSchema schema, bool existing, Func<string, Task> commit)
    {
        Title = title;
        _original = json;
        _document = JsonNode.Parse(json)!.AsObject();
        _commit = commit;
        var fields = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(0, 8, 16, 8) };
        foreach (var field in schema.Fields)
        {
            JsonNode? value = _document;
            foreach (var part in field.Name.Split('.')) value = value?[part];
            var input = new Entry
            {
                Text = value is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : value?.ToJsonString() ?? "",
                Placeholder = field.Type, IsReadOnly = existing && field.Name == "Id"
            };
            var nullable = new Switch { IsToggled = value == null, IsEnabled = field.Type is "String" or "Text" };
            input.IsEnabled = !nullable.IsToggled || !nullable.IsEnabled;
            nullable.Toggled += (_, e) => input.IsEnabled = !e.Value || !nullable.IsEnabled;
            input.TextChanged += (_, _) => { if (!nullable.IsEnabled && !string.IsNullOrEmpty(input.Text)) nullable.IsToggled = false; };
            var group = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) }, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
            group.Add(new Label { Text = field.Name + "  ·  " + field.Type, FontAttributes = FontAttributes.Bold });
            group.Add(input, 0, 1);
            group.Add(new Label { Text = "null", VerticalOptions = LayoutOptions.Center }, 1, 1);
            group.Add(nullable, 2, 1);
            fields.Add(group);
            _fields.Add((field, input, nullable));
        }

        _form = new ScrollView { Content = fields };
        var body = new Grid { Padding = 24, RowSpacing = 12, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) } };
        body.Add(new Label { Text = AppText.DocumentEditorDescription, FontSize = 13 });
        var toggle = new Button { Text = AppText.EditAsJSON, HorizontalOptions = LayoutOptions.Start };
        toggle.Clicked += (_, _) =>
        {
            try
            {
                if (!_json.IsVisible)
                {
                    _json.Text = BuildJson();
                    _json.IsVisible = true;
                    _form.IsVisible = false;
                    toggle.Text = AppText.JSONModeReopenEditorForFields;
                    toggle.IsEnabled = false;
                }
            }
            catch (Exception error) { _error.Text = AppText.Diagnostic(error.Message); }
        };
        body.Add(toggle, 0, 1);
        body.Add(_form, 0, 2);
        body.Add(_json, 0, 2);
        body.Add(_error, 0, 3);
        _save.Clicked += OnSave;
        body.Add(_save, 0, 4);
        Content = body;
        var cancel = new ToolbarItem { Text = AppText.Cancel };
        cancel.Clicked += async (_, _) => await Cancel();
        ToolbarItems.Add(cancel);
    }

    /// <summary>Converts form values to typed JSON without silently discarding malformed input.</summary>
    /// <returns>The complete document JSON.</returns>
    private string BuildJson()
    {
        foreach (var (field, input, nullable) in _fields)
        {
            var parts = field.Name.Split('.');
            var parent = _document;
            if (nullable.IsToggled)
            {
                JsonNode? existing = _document;
                foreach (var part in parts.SkipLast(1)) existing = existing?[part];
                if (existing == null) continue;
            }
            foreach (var part in parts.SkipLast(1))
            {
                parent[part] ??= new JsonObject();
                parent = parent[part]!.AsObject();
            }
            var text = input.Text ?? "";
            try
            {
                parent[parts[^1]] = nullable.IsToggled ? null : field.Type.ToLowerInvariant() switch
                {
                    "string" or "text" or "guid" or "datetime" or "datetimeoffset" or "timespan" or "char" => JsonValue.Create(text),
                    _ => JsonNode.Parse(text)
                };
            }
            catch (JsonException)
            {
                throw new InvalidDataException(AppText.Format("InvalidField", field.Name, field.Type));
            }
        }
        return _document.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Disables duplicate submission and retains entered values on validation failure.</summary>
    /// <param name="sender">The save button.</param>
    /// <param name="e">The click event.</param>
    private async void OnSave(object? sender, EventArgs e)
    {
        if (_saving) return;
        try
        {
            var json = _json.IsVisible ? _json.Text : BuildJson();
            _saving = true;
            _save.IsEnabled = false;
            _save.Text = AppText.Saving;
            _error.Text = "";
            await _commit(json);
        }
        catch (Exception error) { _error.Text = AppText.Diagnostic(error.Message); }
        finally { _saving = false; _save.IsEnabled = true; _save.Text = AppText.SaveDocument; }
    }

    /// <summary>Protects pending edits when closing the form.</summary>
    /// <returns>The completion task.</returns>
    private async Task Cancel()
    {
        if (_saving) return;
        var changed = true;
        try { changed = !JsonNode.DeepEquals(JsonNode.Parse(_original), JsonNode.Parse(_json.IsVisible ? _json.Text : BuildJson())); }
        catch (Exception) { /* invalid input still counts as an unsaved change */ }
        if (!changed || await DisplayAlertAsync(AppText.DiscardChanges, AppText.LeaveWithoutSavingThisDocument, AppText.Discard, AppText.KeepEditing)) await Navigation.PopModalAsync();
    }

    /// <summary>Routes hardware back navigation through the unsaved change check.</summary>
    /// <returns>True because navigation is handled asynchronously.</returns>
    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () => await Cancel());
        return true;
    }
}
