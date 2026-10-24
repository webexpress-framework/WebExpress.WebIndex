using WebExpress.WebIndex.WiUI.Localization;
using System.Diagnostics;
using System.Globalization;
using WebExpress.WebIndex.WiUI.Explorer;
using WebExpress.WebIndex.WiUI.Model;

namespace WebExpress.WebIndex.WiUI.Pages;

/// <summary>Coordinates a native index workspace with serialized background storage operations.</summary>
public partial class MainPage : ContentPage
{
    private ExplorerSession? _session;
    private ExplorerHistory _history = new();
    private readonly string _historyPath = Path.Combine(FileSystem.AppDataDirectory, "query-history.json");
    private IReadOnlyList<object> _documents = [], _terms = [], _visibleRows = [];
    private object? _selected;
    private CancellationTokenSource? _operation;
    private readonly IDispatcherTimer _queryTimer;
    private string _view = "Documents", _executedQuery = "";
    private string? _sort, _folder;
    private bool _descending, _truncated, _windowHooked, _closing;
    private bool _initializing = true, _returningFromModal;
    private int _page;

    /// <summary>Initializes native controls before restoring workspace settings.</summary>
    public MainPage()
    {
        InitializeComponent();
        UpdateTabs();
        CulturePicker.ItemsSource = new[] { "en", "de", "fr", "es", "it", "nl" };
        CulturePicker.SelectedIndex = 0;
        PageSizePicker.ItemsSource = new[] { 25, 50, 100 };
        PageSizePicker.SelectedIndex = 1;
        _queryTimer = Dispatcher.CreateTimer();
        _queryTimer.Interval = TimeSpan.FromMilliseconds(250);
        _queryTimer.IsRepeating = false;
        _queryTimer.Tick += (_, _) => AnalyzeQuery();
        _initializing = false;
    }

    /// <summary>Reloads project settings and registers window cleanup.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_returningFromModal) { _returningFromModal = false; return; }
        if (!_windowHooked && Window != null)
        {
            Window.Destroying += OnWindowDestroying;
            _windowHooked = true;
        }
        try
        {
            _initializing = true;
            _projects = ProjectService.LoadProjects();
            foreach (var project in _projects) ProjectService.Discover(project);
            ProjectService.SaveProjects(_projects);
            _activeProject = _projects.FirstOrDefault(x => x.IsSelected == true) ?? _projects.FirstOrDefault();
            _history = ExplorerHistory.Load(_historyPath);
            RefreshHistory();
            RenderProjectTree();
        }
        catch (Exception error) { StatusLabel.Text = AppText.SettingsCouldNotBeLoaded + AppText.Diagnostic(error.Message); return; }
        finally { _initializing = false; }
        await RunOperation(AppText.CheckingProjectLinks, token => RefreshLinkedDocuments(token));
    }

    /// <summary>Prevents overlapping storage access while preserving cancellation.</summary>
    /// <param name="message">The progress message.</param>
    /// <param name="action">The asynchronous operation.</param>
    /// <returns>The completion task.</returns>
    private async Task RunOperation(string message, Func<CancellationToken, Task> action)
    {
        if (_operation != null) return;
        using var cancellation = new CancellationTokenSource();
        _operation = cancellation;
        Workspace.IsEnabled = false;
        BusyIndicator.IsVisible = BusyIndicator.IsRunning = CancelButton.IsVisible = true;
        StatusLabel.Text = message;
        try { await action(cancellation.Token); if (StatusLabel.Text == message) StatusLabel.Text = AppText.Ready; }
        catch (OperationCanceledException) { StatusLabel.Text = AppText.OperationCanceled; }
        catch (Exception error)
        {
            StatusLabel.Text = error is IOException ? AppText.IndexUnavailableCloseOtherApplicationsUsingItAndRetry + AppText.Diagnostic(error.Message) : AppText.Diagnostic(error.Message);
        }
        finally
        {
            _operation = null;
            Workspace.IsEnabled = true;
            BusyIndicator.IsVisible = BusyIndicator.IsRunning = CancelButton.IsVisible = false;
            UpdateActions();
            AnalyzeQuery();
            if (_closing) { _session?.Dispose(); _session = null; }
        }
    }

    /// <summary>Displays the active source directory without replacing durable project links.</summary>
    /// <param name="folder">The project directory.</param>
    private void LoadFolder(string? folder)
    {
        _folder = folder;
        FolderLabel.Text = folder ?? AppText.OpenAWsSchemaFileToBegin;
        ToolTipProperties.SetText(FolderLabel, FolderLabel.Text);
        RenderProjectTree();
    }

    /// <summary>Filters persistent references without changing the connection.</summary>
    private void FilterIndexes() => RenderProjectTree();

    /// <summary>Opens a connection and clears state belonging to the previous schema.</summary>
    /// <param name="path">The schema path.</param>
    /// <param name="writable">Whether editing is enabled.</param>
    /// <param name="preserveQuery">Whether to retain the query of this same index.</param>
    /// <returns>The completion task.</returns>
    private Task OpenIndex(string path, bool writable = false, bool preserveQuery = false) => RunOperation(AppText.OpeningIndex, async token =>
    {
        var culture = CultureInfo.GetCultureInfo((string)CulturePicker.SelectedItem);
        await Task.Run(() => { _session?.Dispose(); _session = null; }, token);
        ResetResults();
        if (!preserveQuery) QueryEditor.Text = "";
        ExplorerSession? opened = null;
        try
        {
            opened = await Task.Run(() => new ExplorerSession(path, writable, culture), token);
            token.ThrowIfCancellationRequested();
            _session = opened;
            opened = null;
        }
        finally { opened?.Dispose(); }
        IndexTitle.Text = Path.GetFileNameWithoutExtension(path);
        FieldPicker.ItemsSource = _session.Schema.Fields.Where(x => !x.Ignore).ToList();
        FieldPicker.SelectedIndex = -1;
        RefreshHistory();
        UpdateSummary();
        await ExecuteQuery(token);
    });

    /// <summary>Replaces results and records history only after a successful query.</summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The completion task.</returns>
    private async Task ExecuteQuery(CancellationToken token)
    {
        if (_session == null) return;
        var query = QueryEditor.Text ?? "";
        var error = _session.Validate(query);
        if (error != null)
        {
            QueryEditor.Focus();
            QueryEditor.CursorPosition = Math.Clamp(error.Position, 0, query.Length);
            QueryEditor.SelectionLength = Math.Clamp(error.Length, 0, query.Length - QueryEditor.CursorPosition);
            StatusLabel.Text = AppText.QueryNotExecutedPreviousResultsRemainDisplayed;
            return;
        }
        var watch = Stopwatch.StartNew();
        var rows = await Task.Run(() => _session.Query(query, token), token);
        token.ThrowIfCancellationRequested();
        _truncated = rows.Count > ExplorerSession.ResultLimit;
        _documents = rows.Take(ExplorerSession.ResultLimit).ToArray();
        _executedQuery = query;
        await RefreshLinkedDocuments(token, true);
        _terms = [];
        SwitchView("Documents");
        UpdateSummary();
        StatusLabel.Text = AppText.Format("QueryResult", AppText.Count("Documents", _documents.Count), watch.ElapsedMilliseconds) + (_truncated ? AppText.LimitedTo10000RowsRefineWQLOrUseSkipTake : "");
        try { _history.Remember(_session.Path, query); _history.Save(_historyPath); RefreshHistory(); }
        catch (Exception failure) { StatusLabel.Text += AppText.HistoryCouldNotBeSaved + AppText.Diagnostic(failure.Message); }
    }

    /// <summary>Clears schema dependent views after closing or replacing a connection.</summary>
    private void ResetResults()
    {
        _documents = _terms = _visibleRows = [];
        _truncated = false;
        _executedQuery = "";
        FieldPicker.ItemsSource = null;
        IndexTitle.Text = AppText.WelcomeToYourIndexes;
        IndexSummary.Text = AppText.OpenAnIndexToBrowseItsDocuments;
        Suggestions.Clear();
        RefreshHistory();
        ValidationLabel.Text = AppText.OpenAnIndexToStartSearching;
        SwitchView("Documents");
    }

    /// <summary>Shows the active mode and storage count next to the index name.</summary>
    private void UpdateSummary()
    {
        if (_session == null) return;
        IndexSummary.Text = AppText.Format("IndexSummary", AppText.Count("Documents", _session.Count), AppText.Count("Schema", _session.Schema.Fields.Count), (_session.Writable ? AppText.EditingEnabledExclusiveAccess : AppText.ReadSnapshot));
        EditModeButton.Text = _session.Writable ? AppText.FinishEditing : AppText.EnableEditing;
    }

    /// <summary>Projects one page into a native table with stable column widths.</summary>
    private void RenderTable()
    {
        if (Table == null) return;
        var fields = CurrentColumns();
        var source = _view == "Schema" ? _session?.Schema.Fields.Cast<object>().ToArray() ?? [] : _view == "Terms" ? _terms : _documents;
        _visibleRows = ExplorerTable.Select(source, fields, ResultFilter.Text ?? "", _sort, _descending);
        var size = PageSizePicker.SelectedItem is int count ? count : 50;
        var pages = Math.Max(1, (int)Math.Ceiling(_visibleRows.Count / (double)size));
        _page = Math.Clamp(_page, 0, pages - 1);
        Table.Clear();
        EmptyState.IsVisible = _visibleRows.Count == 0;
        EmptyTitle.Text = _session == null ? AppText.StartExploring : AppText.NoRowsToDisplay;
        EmptyDescription.Text = _session == null ? AppText.ChooseAnIndexOnTheLeftToBrowseItsDocuments
            : _view == "Terms" ? AppText.TermsEmptyDescription
            : AppText.TryAnotherQueryOrClearTheLoadedRowFilter;
        if (fields.Length > 0 && _visibleRows.Count > 0)
        {
            var header = CreateRow(fields.Length);
            for (var c = 0; c < fields.Length; c++)
            {
                var field = fields[c];
                var button = new Button { Text = (_view == "Documents" ? field : AppText.Get("Column" + field)) + (_sort == field ? (_descending ? "  ▾" : "  ▴") : ""),
                    BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#D4D4D4"), CornerRadius = 0,
                    FontSize = 12, FontAttributes = FontAttributes.Bold, BorderWidth = 0, Padding = new Thickness(12, 6),
                    HorizontalOptions = LayoutOptions.Start, MinimumHeightRequest = 28 };
                ToolTipProperties.SetText(button, AppText.SortLoadedRowsBy + field);
                button.Clicked += (_, _) => { _descending = _sort == field && !_descending; _sort = field; _page = 0; RenderTable(); };
                var cell = new Border { BackgroundColor = Color.FromArgb("#2D2D30"), Stroke = Color.FromArgb("#454545"), StrokeThickness = 0.5, Content = button };
                header.Add(cell, c);
            }
            Table.Add(header);
            var number = 0;
            foreach (var item in _visibleRows.Skip(_page * size).Take(size))
            {
                var row = CreateRow(fields.Length);
                row.BindingContext = item;
                var alternate = number++ % 2 == 0;
                row.SetAppThemeColor(BackgroundColorProperty, alternate ? Colors.White : Color.FromArgb("#F6F8FC"), alternate ? Color.FromArgb("#1E1E1E") : Color.FromArgb("#242424"));
                if (ReferenceEquals(item, _selected)) row.BackgroundColor = Color.FromArgb("#094771");
                for (var c = 0; c < fields.Length; c++)
                {
                    var text = ExplorerTable.Format(ExplorerTable.Value(item, fields[c]));
                    var label = new Label { Text = text, FontSize = 13, Padding = new Thickness(12, 6), MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
                    ToolTipProperties.SetText(label, text.Length > 2000 ? text[..2000] + "…" : text);
                    if (c == 0)
                    {
                        var select = new Button { Text = text, BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#D4D4D4"),
                            Padding = new Thickness(12, 6), FontSize = 13, CornerRadius = 0, HorizontalOptions = LayoutOptions.Start };
                        SemanticProperties.SetDescription(select, AppText.SelectRow + text);
                        select.Clicked += (_, _) => SelectRow(item, fields[0]);
                        row.Add(select, c);
                    }
                    else row.Add(label, c);
                }
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => SelectRow(item, fields[0]);
                row.GestureRecognizers.Add(tap);
                Table.Add(row);
                Table.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#DCE3EE"), Opacity = 0.4 });
            }
        }
        PageLabel.Text = AppText.Count(_view, _visibleRows.Count) + (_view == "Documents" && _truncated ? AppText.First10000 : "");
        PageNumber.Text = $"{_page + 1} / {pages}";
        PreviousButton.IsEnabled = _page > 0;
        NextButton.IsEnabled = _page + 1 < pages;
        UpdateTabs();
        if (_selected != null && !_visibleRows.Contains(_selected)) _selected = null;
        UpdateActions();
    }

    /// <summary>Connects the active tab visually to its content without styling navigation as an action.</summary>
    private void UpdateTabs()
    {
        var tabs = new[]
        {
            (Key: "Documents", Button: DocumentsTab, Surface: DocumentsTabSurface, Accent: DocumentsTabAccent),
            (Key: "Schema", Button: SchemaTab, Surface: SchemaTabSurface, Accent: SchemaTabAccent),
            (Key: "Terms", Button: TermsTab, Surface: TermsTabSurface, Accent: TermsTabAccent)
        };
        foreach (var tab in tabs)
        {
            var selected = tab.Key == _view;
            tab.Surface.BackgroundColor = Color.FromArgb(selected ? "#252526" : "#2D2D30");
            tab.Accent.Opacity = selected ? 1 : 0;
            tab.Button.TextColor = Color.FromArgb(selected ? "#FFFFFF" : "#A0A0A0");
            tab.Button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    /// <summary>Synchronizes the visible selection and document actions for mouse and keyboard input.</summary>
    /// <param name="item">The selected row.</param>
    /// <param name="firstField">The identifying field for non-document views.</param>
    private void SelectRow(object item, string firstField)
    {
        _selected = item;
        SelectionLabel.Text = AppText.Selected + ExplorerTable.Format(ExplorerTable.Value(item, _view == "Documents" ? "Id" : firstField));
        var number = 0;
        foreach (var row in Table.Children.OfType<Grid>().Where(x => x.BindingContext != null))
        {
            var alternate = number++ % 2 == 0;
            row.BackgroundColor = ReferenceEquals(row.BindingContext, item) ? Color.FromArgb("#094771")
                : Color.FromArgb(alternate ? "#1E1E1E" : "#242424");
        }
        UpdateActions();
    }

    /// <summary>Keeps headers and cells aligned during horizontal scrolling.</summary>
    /// <param name="columns">The column count.</param>
    /// <returns>The table row.</returns>
    private static Grid CreateRow(int columns)
    {
        var row = new Grid { ColumnSpacing = 0 };
        for (var i = 0; i < columns; i++) row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(210)));
        return row;
    }

    /// <summary>Shares column order between display and export.</summary>
    /// <returns>The current property paths.</returns>
    private string[] CurrentColumns() => _view switch
    {
        "Schema" => ["Name", "Type", "Ignore"], "Terms" => ["Value", "Frequency", "Documents"],
        _ => _session?.Schema.Fields.OrderBy(x => x.Name == "Name" ? 0 : x.Name == "Id" ? 2 : 1).Select(x => x.Name).ToArray() ?? []
    };

    /// <summary>Enables mutations only for a writable connection and the appropriate selection.</summary>
    private void UpdateActions()
    {
        var available = _session != null && _operation == null;
        EditModeButton.IsEnabled = RunButton.IsEnabled = available;
        NewButton.IsEnabled = available && _session!.Writable;
        DetailsButton.IsEnabled = available && _selected != null;
        LinkButton.IsEnabled = available && _selected != null && _view == "Documents";
        UpdateButton.IsEnabled = DeleteButton.IsEnabled = available && _session!.Writable && _view == "Documents" && _selected != null;
        if (_selected == null) SelectionLabel.Text = AppText.SelectARowToInspectItsFullValues;
    }

    /// <summary>Scopes recent queries and favorites to the active index.</summary>
    private void RefreshHistory() => HistoryPicker.ItemsSource = _history.Queries.Where(x => x.Index.Equals(_session?.Path, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.Favorite).ToArray();

    /// <summary>Changes views without discarding the last successful query.</summary>
    /// <param name="view">The chosen workspace view.</param>
    private void SwitchView(string view)
    {
        _view = view;
        _page = 0;
        _sort = null;
        _selected = null;
        FieldPicker.IsVisible = view == "Terms";
        RenderTable();
    }
}
