using WebExpress.WebIndex.WiUI.Localization;
using System.Collections.ObjectModel;
using System.Globalization;
using WebExpress.WebIndex.WiUI.Explorer;
using WebExpress.WebIndex.WiUI.Model;

namespace WebExpress.WebIndex.WiUI.Pages;

/// <summary>Maintains durable project references separately from transient query results.</summary>
public partial class MainPage
{
    private ObservableCollection<Project> _projects = [];
    private Project? _activeProject;
    private readonly HashSet<object> _collapsed = [];
    private readonly Dictionary<(string, Guid), bool> _documentAvailability = [];
    private readonly Dictionary<string, string> _linkErrors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Builds an expandable project tree while retaining unavailable references.</summary>
    private void RenderProjectTree()
    {
        if (ProjectTree == null) return;
        ProjectTree.Clear();
        var filter = IndexFilter.Text ?? "";
        foreach (var project in _projects)
        {
            var matchesProject = (project.ProjectName ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase);
            var indexes = project.Indexes.Where(x => matchesProject || x.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || x.Documents.Any(d => d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (!matchesProject && indexes.Length == 0) continue;
            AddTreeRow(project, 0, project.ProjectName ?? AppText.ProjectLabel, true, true, () =>
            {
                _activeProject = project;
                foreach (var item in _projects) item.IsSelected = item == project;
                ProjectService.SaveProjects(_projects);
                FolderLabel.Text = project.IndexPath ?? AppText.LinkedIndexesFromMultipleFolders;
                RenderProjectTree();
                return Task.CompletedTask;
            });
            if (_collapsed.Contains(project) && filter.Length == 0) continue;
            foreach (var index in indexes)
            {
                var available = ProjectService.IsAvailable(index);
                AddTreeRow(index, 1, index.Name + (available ? "" : AppText.Missing), available, index.Documents.Count > 0,
                    async () => { _activeProject = project; SetCulture(index.Culture); await OpenIndex(index.Path); },
                    () => ManageIndex(project, index));
                if (_collapsed.Contains(index) && filter.Length == 0) continue;
                foreach (var document in index.Documents)
                {
                    if (!matchesProject && !index.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        && !document.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
                    var known = _documentAvailability.TryGetValue((index.Path, document.Id), out var exists);
                    var enabled = available && !_linkErrors.ContainsKey(index.Path) && (!known || exists);
                    var suffix = !available || known && !exists ? AppText.Missing : _linkErrors.ContainsKey(index.Path) ? AppText.Unavailable : !known ? AppText.Unchecked : "";
                    AddTreeRow(document, 2, document.Name + suffix, enabled, false,
                        () => OpenLinkedDocument(project, index, document), () => ManageDocument(index, document));
                }
            }
        }
        if (_projects.Count == 0) ProjectTree.Add(new Label { Text = AppText.NoProjectsYetCreateAProjectThenLinkIndexesAndDocuments, Padding = 16, TextColor = Colors.Gray });
    }

    /// <summary>Provides keyboard accessible tree actions and a separate menu for missing links.</summary>
    /// <param name="node">The expansion identity.</param>
    /// <param name="depth">The indentation level.</param>
    /// <param name="text">The display label.</param>
    /// <param name="available">Whether the target can be activated.</param>
    /// <param name="expandable">Whether child references exist.</param>
    /// <param name="open">The activation operation.</param>
    /// <param name="menu">The optional link management operation.</param>
    private void AddTreeRow(object node, int depth, string text, bool available, bool expandable, Func<Task> open, Func<Task>? menu = null)
    {
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(24), new ColumnDefinition(GridLength.Star), new ColumnDefinition(30) }, Padding = new Thickness(depth * 14 + 4, 0, 2, 0) };
        if (ReferenceEquals(node, _activeProject)) row.BackgroundColor = Color.FromArgb("#37373D");
        if (expandable)
        {
            var toggle = TreeButton(_collapsed.Contains(node) ? "▸" : "▾");
            SemanticProperties.SetDescription(toggle, AppText.ExpandOrCollapse + text);
            toggle.Clicked += (_, _) => { if (!_collapsed.Add(node)) _collapsed.Remove(node); RenderProjectTree(); };
            row.Add(toggle);
        }
        var label = TreeButton((depth == 0 ? "▣  " : depth == 1 ? "▤  " : "↳  ") + text);
        label.IsEnabled = available;
        label.HorizontalOptions = LayoutOptions.Start;
        label.TextColor = available ? Color.FromArgb("#D4D4D4") : Color.FromArgb("#777777");
        ToolTipProperties.SetText(label, node is ProjectIndex index ? index.Path : node is ProjectDocument document ? document.Id.ToString() : text);
        label.Clicked += async (_, _) => { try { await open(); } catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); } };
        row.Add(label, 1);
        if (menu != null)
        {
            var more = TreeButton("⋯");
            SemanticProperties.SetDescription(more, AppText.ManageLink + text);
            more.Clicked += async (_, _) => { try { await menu(); } catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); } };
            row.Add(more, 2);
        }
        ProjectTree.Add(row);
    }

    /// <summary>Uses compact flat controls for keyboard navigation inside the tree.</summary>
    /// <param name="text">The accessible button text.</param>
    /// <returns>The compact tree action.</returns>
    private static Button TreeButton(string text) => new() { Style = new Style(typeof(Button)), Text = text, BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#D4D4D4"), FontSize = 12, Padding = new Thickness(3, 2), CornerRadius = 0, MinimumWidthRequest = 0, MinimumHeightRequest = 28 };

    /// <summary>Checks linked identities without using the currently filtered result set.</summary>
    /// <param name="token">The cancellation token.</param>
    /// <param name="activeOnly">Whether to refresh only the open index after a query or mutation.</param>
    /// <returns>The completion task.</returns>
    private async Task RefreshLinkedDocuments(CancellationToken token, bool activeOnly = false)
    {
        foreach (var project in _projects)
        {
            ProjectService.Discover(project);
            foreach (var index in project.Indexes.Where(x => x.Documents.Count > 0))
            {
                token.ThrowIfCancellationRequested();
                if (activeOnly && !string.Equals(_session?.Path, index.Path, StringComparison.OrdinalIgnoreCase)) continue;
                _linkErrors.Remove(index.Path);
                if (!ProjectService.IsAvailable(index))
                {
                    foreach (var document in index.Documents) _documentAvailability[(index.Path, document.Id)] = false;
                    continue;
                }
                try
                {
                    var results = await Task.Run(() =>
                    {
                        var active = string.Equals(_session?.Path, index.Path, StringComparison.OrdinalIgnoreCase);
                        using var snapshot = active ? null : new ExplorerSession(index.Path, false, CultureInfo.GetCultureInfo(index.Culture));
                        var session = active ? _session! : snapshot!;
                        return index.Documents.ToDictionary(x => x.Id, x => session.Find(x.Id) != null);
                    }, token);
                    foreach (var result in results) _documentAvailability[(index.Path, result.Key)] = result.Value;
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    _linkErrors[index.Path] = AppText.Diagnostic(error.Message);
                    foreach (var document in index.Documents) _documentAvailability.Remove((index.Path, document.Id));
                }
            }
        }
        ProjectService.SaveProjects(_projects);
        RenderProjectTree();
    }

    /// <summary>Opens a pinned document by identity without depending on pagination.</summary>
    /// <param name="project">The owning project.</param>
    /// <param name="index">The linked index.</param>
    /// <param name="document">The document reference.</param>
    /// <returns>The completion task.</returns>
    private async Task OpenLinkedDocument(Project project, ProjectIndex index, ProjectDocument document)
    {
        _activeProject = project;
        if (!string.Equals(_session?.Path, index.Path, StringComparison.OrdinalIgnoreCase))
        {
            SetCulture(index.Culture);
            await OpenIndex(index.Path);
        }
        if (!string.Equals(_session?.Path, index.Path, StringComparison.OrdinalIgnoreCase)) return;
        QueryEditor.Text = $"Id = '{document.Id}'";
        await RunOperation(AppText.OpeningLinkedDocument, ExecuteQuery);
        var selected = _documents.FirstOrDefault();
        if (selected != null) SelectRow(selected, "Id");
        else RenderTable();
    }

    /// <summary>Changes the token language without triggering a redundant reopen.</summary>
    /// <param name="culture">The stored culture name.</param>
    private void SetCulture(string culture)
    {
        _initializing = true;
        CulturePicker.SelectedItem = culture;
        if (CulturePicker.SelectedIndex < 0) CulturePicker.SelectedIndex = 0;
        _initializing = false;
    }

    /// <summary>Creates a workspace that may link indexes from multiple directories.</summary>
    /// <param name="name">The project name.</param>
    /// <param name="folder">The optional discovery directory.</param>
    /// <returns>The new project.</returns>
    private Project CreateProject(string name, string? folder = null)
    {
        var project = new Project { ProjectName = name, IndexPath = folder, IsSelected = true };
        foreach (var existing in _projects) existing.IsSelected = false;
        _projects.Add(project);
        _activeProject = project;
        ProjectService.SaveProjects(_projects);
        RenderProjectTree();
        return project;
    }

    /// <summary>Creates a named project without requiring a source directory.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event context.</param>
    private async void OnNewProject(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(AppText.NewProjectTitle, AppText.ProjectName, placeholder: AppText.MyIndexes);
        if (string.IsNullOrWhiteSpace(name)) return;
        try { CreateProject(name.Trim()); } catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Links the selected document persistently using its immutable identity.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event context.</param>
    private async void OnLinkDocument(object sender, EventArgs e)
    {
        if (_session == null || _selected == null || _view != "Documents") return;
        try
        {
            if (_activeProject == null)
            {
                var name = await DisplayPromptAsync(AppText.CreateProject, AppText.NameTheProjectForThisDocument, initialValue: AppText.MyIndexes);
                if (string.IsNullOrWhiteSpace(name)) return;
                CreateProject(name.Trim());
            }
            var index = ProjectService.LinkIndex(_activeProject!, _session.Path);
            index.Culture = (string)CulturePicker.SelectedItem;
            var id = (Guid)ExplorerTable.Value(_selected, "Id")!;
            var nameValue = ExplorerTable.Value(_selected, "Name")?.ToString();
            ProjectService.LinkDocument(index, id, string.IsNullOrWhiteSpace(nameValue) ? id.ToString() : nameValue);
            _documentAvailability[(index.Path, id)] = true;
            _collapsed.Remove(_activeProject!);
            _collapsed.Remove(index);
            ProjectService.SaveProjects(_projects);
            RenderProjectTree();
            StatusLabel.Text = AppText.Format("DocumentLinked", _activeProject!.ProjectName);
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Opens project properties in the same application shell.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event context.</param>
    private async void OnManageProjects(object sender, EventArgs e) => await Shell.Current.GoToAsync("//ProjectPage");

    /// <summary>Offers reference operations even while the source index is missing.</summary>
    /// <param name="project">The owning project.</param>
    /// <param name="index">The index reference.</param>
    /// <returns>The completion task.</returns>
    private async Task ManageIndex(Project project, ProjectIndex index)
    {
        var action = await DisplayActionSheetAsync(index.Name, AppText.Cancel, null, AppText.RenameLink, AppText.LocateIndex, AppText.RemoveLink);
        if (action == AppText.RenameLink)
        {
            var name = await DisplayPromptAsync(AppText.RenameLink, AppText.DisplayName, initialValue: index.Name);
            if (!string.IsNullOrWhiteSpace(name)) index.Name = name.Trim();
        }
        else if (action == AppText.LocateIndex)
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = AppText.Format("LocateFile", Path.GetFileName(index.Path)), FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> { [DevicePlatform.WinUI] = [".ws"] }) });
            if (file == null) return;
            if (project.Indexes.Any(x => x != index && string.Equals(x.Path, file.FullPath, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException(AppText.ThisIndexIsAlreadyLinkedToTheProject);
            index.Path = file.FullPath;
        }
        else if (action == AppText.RemoveLink)
        {
            if (!await DisplayAlertAsync(AppText.RemoveIndexLink, AppText.RemoveIndexConfirmation, AppText.RemoveLink, AppText.Cancel)) return;
            project.Indexes.Remove(index);
        }
        else return;
        ProjectService.SaveProjects(_projects);
        await RunOperation(AppText.RefreshingLinks, token => RefreshLinkedDocuments(token));
    }

    /// <summary>Renames or removes a document reference without modifying its source.</summary>
    /// <param name="index">The owning index reference.</param>
    /// <param name="document">The document reference.</param>
    /// <returns>The completion task.</returns>
    private async Task ManageDocument(ProjectIndex index, ProjectDocument document)
    {
        var action = await DisplayActionSheetAsync(document.Name, AppText.Cancel, null, AppText.RenameLink, AppText.RemoveLink);
        if (action == AppText.RenameLink)
        {
            var name = await DisplayPromptAsync(AppText.RenameLink, AppText.DisplayName, initialValue: document.Name);
            if (!string.IsNullOrWhiteSpace(name)) document.Name = name.Trim();
        }
        else if (action == AppText.RemoveLink) index.Documents.Remove(document);
        else return;
        ProjectService.SaveProjects(_projects);
        RenderProjectTree();
    }
}
