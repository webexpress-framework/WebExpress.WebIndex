using WebExpress.WebIndex.WiUI.Localization;
using CommunityToolkit.Maui.Storage;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using WebExpress.WebIndex.WiUI.Explorer;
using WebExpress.WebIndex.WiUI.Model;

namespace WebExpress.WebIndex.WiUI.Pages;

/// <summary>Connects native workspace commands to validated explorer operations.</summary>
public partial class MainPage
{
    /// <summary>Opens isolated sample data for learning query and editing workflows.</summary>
    /// <param name="sender">The sample button.</param>
    /// <param name="e">The click event.</param>
    private async void OnSample(object sender, EventArgs e)
    {
        string? path = null;
        await RunOperation(AppText.PreparingSampleIndex, async token =>
        {
            path = await Task.Run(() => ExplorerSample.Create(Path.Combine(FileSystem.AppDataDirectory, "sample-index")), token);
        });
        if (path != null)
        {
            LoadFolder(Path.GetDirectoryName(path));
            SetCulture("en");
            await OpenIndex(path);
        }
    }

    /// <summary>Displays diagnostics and completion choices from the engine grammar.</summary>
    private void AnalyzeQuery()
    {
        if (_initializing || _operation != null || _session == null) return;
        try
        {
            var text = QueryEditor.Text ?? "";
            var error = _session.Validate(text);
            ValidationLabel.Text = error == null ? (text == _executedQuery ? AppText.ValidWQLDisplayedResult : AppText.ValidWQLRunToUpdateResults) : AppText.Format("QueryError", error.Position + 1, AppText.Diagnostic(error.Message));
            ValidationLabel.SetAppThemeColor(Label.TextColorProperty, error == null ? Color.FromArgb("#168064") : Color.FromArgb("#B42335"), error == null ? Color.FromArgb("#6EE7B7") : Color.FromArgb("#FDA4AF"));
            Suggestions.Clear();
            var caret = Math.Clamp(QueryEditor.CursorPosition, 0, text.Length);
            var range = ExplorerCompletion.Range(text, caret, QueryEditor.SelectionLength);
            if (range == null) return;
            var (start, end) = range.Value;
            var fragment = text[start..caret];
            var values = _session.Suggest(text[..start]).Where(x => x.StartsWith(fragment, StringComparison.OrdinalIgnoreCase)).Take(12).ToArray();
            if (values.Length == 0) { start = end = caret; values = _session.Suggest(text[..caret]).Take(12).ToArray(); }
            foreach (var value in values)
            {
                var from = start;
                var button = new Button { Text = value, Style = (Style)Resources["SuggestionChip"] };
                button.Clicked += (_, _) => { if ((QueryEditor.Text ?? "") == text) InsertSuggestion(value, from, end); };
                Suggestions.Add(button);
            }
        }
        catch (Exception error) { ValidationLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Retains text after the caret when inserting a completion.</summary>
    /// <param name="value">The chosen suggestion.</param>
    /// <param name="start">The replacement start.</param>
    /// <param name="end">The replacement end.</param>
    private void InsertSuggestion(string value, int start, int end)
    {
        var text = QueryEditor.Text ?? "";
        if (start > text.Length || end > text.Length) return;
        var insertion = ExplorerCompletion.Insert(text, start, end, value);
        QueryEditor.Text = insertion.Text;
        QueryEditor.Focus();
        QueryEditor.CursorPosition = insertion.Caret;
        QueryEditor.SelectionLength = 0;
    }

    /// <summary>Loads reverse terms only for the requested field.</summary>
    /// <returns>The completion task.</returns>
    private Task LoadTerms() => RunOperation(AppText.LoadingReverseTerms, async token =>
    {
        if (_session == null || FieldPicker.SelectedItem is not ExplorerField field) return;
        var terms = await Task.Run(() => _session.Terms(field.Name, token), token);
        _terms = terms.Cast<object>().ToArray();
        _page = 0;
        _selected = null;
        RenderTable();
        StatusLabel.Text = AppText.Format("TermCount", AppText.Count("Terms", _terms.Count), field.Name) + (_terms.Count == ExplorerSession.ResultLimit ? AppText.LimitedTo10000Terms : "");
    });

    /// <summary>Exports every filtered page of the loaded view.</summary>
    /// <param name="csv">Whether to produce CSV.</param>
    /// <returns>The completion task.</returns>
    private Task Export(bool csv) => RunOperation(AppText.ExportingLoadedRows, async token =>
    {
        if (_session == null || _visibleRows.Count == 0) { StatusLabel.Text = AppText.NoLoadedRowsToExport; return; }
        var text = csv ? ExplorerTable.Csv(_visibleRows, CurrentColumns()) : ExplorerTable.Json(_visibleRows);
        using var stream = new MemoryStream(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray());
        var result = await FileSaver.Default.SaveAsync($"{Path.GetFileNameWithoutExtension(_session.Path)}-{_view.ToLowerInvariant()}.{(csv ? "csv" : "json")}", stream, token);
        StatusLabel.Text = result.IsSuccessful ? AppText.Format("ExportComplete", _visibleRows.Count, result.FilePath) : result.Exception?.Message ?? AppText.ExportCanceled;
    });

    /// <summary>Edits through the connection owning the selected document.</summary>
    /// <param name="create">Whether to insert a new document.</param>
    /// <returns>The completion task.</returns>
    private async Task EditDocument(bool create)
    {
        if (_session?.Writable != true || (!create && _selected == null)) return;
        var session = _session;
        var json = create ? session.NewDocument() : JsonSerializer.Serialize(_selected, new JsonSerializerOptions { WriteIndented = true });
        var id = create ? (Guid?)null : (Guid)ExplorerTable.Value(_selected, "Id")!;
        var editor = new DocumentPage(create ? AppText.NewDocument : AppText.EditDocument, json, session.Schema, !create, async text =>
        {
            using var saving = new CancellationTokenSource();
            _operation = saving;
            try { await Task.Run(() => session.Save(text, id)); }
            finally
            {
                _operation = null;
                if (_closing) { _session?.Dispose(); _session = null; }
            }
            if (_closing) return;
            await Navigation.PopModalAsync();
            QueryEditor.Text = _executedQuery;
            await RunOperation(AppText.RefreshingAfterSave, ExecuteQuery);
        });
        await ShowModal(editor);
    }

    /// <summary>Avoids reloading project settings during the return from a local document dialog.</summary>
    /// <param name="page">The modal document page.</param>
    /// <returns>The navigation task.</returns>
    private async Task ShowModal(ContentPage page)
    {
        _returningFromModal = true;
        try { await Navigation.PushModalAsync(new NavigationPage(page)); }
        catch { _returningFromModal = false; throw; }
    }

    /// <summary>Releases the connection after any in-flight storage work has stopped.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        _operation?.Cancel();
        _queryTimer.Stop();
        if (_operation == null) { _session?.Dispose(); _session = null; }
        else _closing = true;
    }

    /// <summary>Opens a selected schema and remembers its containing project.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnOpen(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = AppText.OpenIndexSchema, FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> { [DevicePlatform.WinUI] = [".ws"] }) });
            if (file == null) return;
            LoadFolder(Path.GetDirectoryName(file.FullPath));
            await OpenIndex(file.FullPath);
            if (_session == null) return;
            _activeProject ??= CreateProject(new DirectoryInfo(_folder!).Name, _folder);
            ProjectService.LinkIndex(_activeProject, file.FullPath).Culture = (string)CulturePicker.SelectedItem;
            ProjectService.SaveProjects(_projects);
            RenderProjectTree();
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Applies the navigator filter while typing.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnIndexFilterChanged(object sender, TextChangedEventArgs e) => FilterIndexes();
    /// <summary>Filters the loaded result without executing WQL.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnResultFilterChanged(object sender, TextChangedEventArgs e) { _page = 0; RenderTable(); }
    /// <summary>Debounces parser analysis during typing.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnQueryChanged(object sender, TextChangedEventArgs e) { if (_queryTimer == null) return; _queryTimer.Stop(); _queryTimer.Start(); }
    /// <summary>Updates suggestions when the caret changes.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnCaretChanged(object sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(Editor.CursorPosition) && _queryTimer != null) { _queryTimer.Stop(); _queryTimer.Start(); } }
    /// <summary>Executes the editor expression explicitly.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnRun(object sender, EventArgs e) => await RunOperation(AppText.ExecutingWQL, ExecuteQuery);
    /// <summary>Requests cancellation without disposing active storage.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnCancel(object sender, EventArgs e) => _operation?.Cancel();
    /// <summary>Clears editor text without implicitly executing a query.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnClearQuery(object sender, EventArgs e) => QueryEditor.Text = "";
    /// <summary>Shows the last successful query result.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnDocuments(object sender, EventArgs e) => SwitchView("Documents");
    /// <summary>Shows schema types and indexing flags.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnSchema(object sender, EventArgs e) => SwitchView("Schema");
    /// <summary>Shows the selected reverse index.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnTerms(object sender, EventArgs e) { SwitchView("Terms"); await LoadTerms(); }
    /// <summary>Refreshes terms for the newly selected field.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnTermFieldChanged(object sender, EventArgs e) { if (!_initializing && _view == "Terms") await LoadTerms(); }
    /// <summary>Resets pagination after changing the page size.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnPageSizeChanged(object sender, EventArgs e) { if (!_initializing) { _page = 0; RenderTable(); } }
    /// <summary>Shows the preceding result page.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnPrevious(object sender, EventArgs e) { _page--; RenderTable(); }
    /// <summary>Shows the following result page.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnNext(object sender, EventArgs e) { _page++; RenderTable(); }
    /// <summary>Exports the loaded view as CSV.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnExportCsv(object sender, EventArgs e) => await Export(true);
    /// <summary>Exports the loaded view as JSON.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnExportJson(object sender, EventArgs e) => await Export(false);
    /// <summary>Opens a form with a generated document identifier.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnNew(object sender, EventArgs e) => await EditDocument(true);
    /// <summary>Edits the selected document while preserving its identifier.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnEdit(object sender, EventArgs e) => await EditDocument(false);

    /// <summary>Reopens the source to replace a stale snapshot.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnRefresh(object sender, EventArgs e)
    {
        if (_session != null) await OpenIndex(_session.Path, _session.Writable, true);
        else await RunOperation(AppText.RefreshingProjects, token => RefreshLinkedDocuments(token));
    }

    /// <summary>Reopens using the selected token processing culture.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnCultureChanged(object sender, EventArgs e)
    {
        if (_initializing || _session == null) return;
        try
        {
            foreach (var index in _projects.SelectMany(x => x.Indexes).Where(x => string.Equals(x.Path, _session.Path, StringComparison.OrdinalIgnoreCase))) index.Culture = (string)CulturePicker.SelectedItem;
            ProjectService.SaveProjects(_projects);
            await OpenIndex(_session.Path, _session.Writable, true);
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Closes storage and clears every dependent view.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnClose(object sender, EventArgs e) => await RunOperation(AppText.ClosingIndex, async token =>
    {
        await Task.Run(() => { _session?.Dispose(); _session = null; }, token);
        ResetResults();
        StatusLabel.Text = AppText.IndexClosed;
    });

    /// <summary>Switches explicitly between snapshot inspection and exclusive editing.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnToggleEditing(object sender, EventArgs e)
    {
        if (_session == null) return;
        var writable = !_session.Writable;
        if (writable && !await DisplayAlertAsync(AppText.EnableEditing, AppText.EnableEditingConfirmation, AppText.EnableEditing, AppText.Cancel)) return;
        await OpenIndex(_session.Path, writable, true);
    }

    /// <summary>Deletes only the selected document after confirmation.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnDelete(object sender, EventArgs e)
    {
        if (_session?.Writable != true || _selected == null || _view != "Documents") return;
        var id = (Guid)ExplorerTable.Value(_selected, "Id")!;
        if (!await DisplayAlertAsync(AppText.DeleteDocument, AppText.Format("ConfirmDelete", id), AppText.DeleteDocument, AppText.Cancel)) return;
        await RunOperation(AppText.DeletingDocument, async token =>
        {
            token.ThrowIfCancellationRequested();
            await Task.Run(() => _session.Delete(id));
            QueryEditor.Text = _executedQuery;
            await ExecuteQuery(CancellationToken.None);
            StatusLabel.Text = AppText.DocumentDeletedResultsAndReverseIndexesUpdated;
        });
    }

    /// <summary>Shows full selected values and term postings with a copy action.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnDetails(object sender, EventArgs e)
    {
        if (_selected == null) return;
        var text = JsonSerializer.Serialize(_selected, new JsonSerializerOptions { WriteIndented = true });
        var page = new ContentPage { Title = AppText.RowDetails };
        var content = new Grid { Padding = 20, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }, RowSpacing = 12 };
        var copy = new Button { Text = AppText.CopyFullJSON };
        copy.Clicked += async (_, _) => { await Clipboard.Default.SetTextAsync(text); copy.Text = AppText.Copied; };
        content.Add(copy);
        content.Add(new Editor { Text = text, IsReadOnly = true, FontFamily = "Consolas", FontSize = 14 }, 0, 1);
        page.Content = content;
        var close = new ToolbarItem { Text = AppText.Close };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        page.ToolbarItems.Add(close);
        await ShowModal(page);
    }

    /// <summary>Saves valid WQL as an index scoped favorite.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnFavorite(object sender, EventArgs e)
    {
        if (_session == null) return;
        try
        {
            var text = QueryEditor.Text ?? "";
            if (string.IsNullOrWhiteSpace(text) || _session.Validate(text) != null) { StatusLabel.Text = AppText.EnterValidWQLBeforeSavingIt; return; }
            _history.Remember(_session.Path, text, true);
            _history.Save(_historyPath);
            RefreshHistory();
            StatusLabel.Text = AppText.QuerySavedToFavorites;
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Restores a query for review before execution.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnHistorySelected(object sender, EventArgs e)
    {
        if (HistoryPicker.SelectedItem is ExplorerQuery query) QueryEditor.Text = query.Query;
    }

    /// <summary>Removes the selected recent query or favorite.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnRemoveHistory(object sender, EventArgs e)
    {
        if (HistoryPicker.SelectedItem is not ExplorerQuery query) return;
        try { _history.Queries.Remove(query); _history.Save(_historyPath); RefreshHistory(); }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Explains WQL using an available text field as an example.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnHelp(object sender, EventArgs e)
    {
        var field = _session?.Schema.Fields.FirstOrDefault(x => x.Type is "String" or "Text")?.Name ?? "Name";
        await DisplayAlertAsync(AppText.WQLQuickReference, AppText.Format("WqlHelpBody", field), AppText.GotIt);
    }
}
