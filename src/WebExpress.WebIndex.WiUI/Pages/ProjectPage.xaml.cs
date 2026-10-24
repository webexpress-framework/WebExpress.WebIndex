using WebExpress.WebIndex.WiUI.Localization;
using System.Collections.ObjectModel;
using WebExpress.WebIndex.WiUI.Model;

namespace WebExpress.WebIndex.WiUI.Pages;

/// <summary>Manages project properties and durable references independently from index data.</summary>
public partial class ProjectPage : ContentPage
{
    private ObservableCollection<Project> _projects = [];
    private Project? _selected;
    private bool _restoringSelection;

    /// <summary>Initializes the project editor.</summary>
    public ProjectPage() => InitializeComponent();

    /// <summary>Reloads shortcuts added by the explorer.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        try { _selected = null; _projects = ProjectService.LoadProjects(); ProjectsList.ItemsSource = _projects; ProjectsList.SelectedItem = _projects.FirstOrDefault(x => x.IsSelected == true) ?? _projects.FirstOrDefault(); }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Copies settings into the form so typing does not mutate saved entries.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringSelection) return;
        if (!await CanLeaveProperties())
        {
            _restoringSelection = true;
            ProjectsList.SelectedItem = _selected;
            _restoringSelection = false;
            return;
        }
        _selected = e.CurrentSelection.FirstOrDefault() as Project;
        NameInput.Text = _selected?.ProjectName;
        PathInput.Text = _selected?.IndexPath;
        SaveButton.IsEnabled = RemoveButton.IsEnabled = AddIndexButton.IsEnabled = _selected != null;
        RenderLinks();
    }

    /// <summary>Creates a named project before editing its optional discovery settings.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnAdd(object sender, EventArgs e)
    {
        if (!await CanLeaveProperties()) return;
        var name = await DisplayPromptAsync(AppText.NewProjectTitle, AppText.ProjectName, placeholder: AppText.MyIndexes);
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            var project = new Project { ProjectName = name.Trim() };
            _projects.Add(project);
            ProjectService.SaveProjects(_projects);
            NameInput.Text = _selected?.ProjectName;
            PathInput.Text = _selected?.IndexPath;
            ProjectsList.SelectedItem = project;
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Chooses a directory through one of its schema files.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnBrowse(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = AppText.ChooseAnIndexSchema, FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> { [DevicePlatform.WinUI] = [".ws"] }) });
            if (file != null) PathInput.Text = Path.GetDirectoryName(file.FullPath);
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Validates folder and name before atomically persisting the project list.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private void OnSave(object sender, EventArgs e)
    {
        if (_selected == null) return;
        try
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text)) throw new InvalidDataException(AppText.EnterAProjectName);
            var path = string.IsNullOrWhiteSpace(PathInput.Text) ? null : Path.GetFullPath(PathInput.Text.Trim());
            if (path != _selected.IndexPath && path != null && !Directory.Exists(path)) throw new InvalidDataException(AppText.ChooseAnExistingDiscoveryFolderOrLeaveItBlank);
            if (_selected.IndexPath != path) _selected.IsDiscovered = false;
            _selected.ProjectName = NameInput.Text.Trim();
            _selected.IndexPath = path;
            foreach (var project in _projects) project.IsSelected = project == _selected;
            ProjectService.Discover(_selected);
            ProjectService.SaveProjects(_projects);
            RenderLinks();
            StatusLabel.Text = AppText.ProjectSavedLinksAreAvailableInTheIndexExplorerTree;
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Removes only the shortcut after confirmation.</summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event context.</param>
    private async void OnRemove(object sender, EventArgs e)
    {
        if (_selected == null || !await DisplayAlertAsync(AppText.RemoveProject, AppText.RemoveProjectConfirmation, AppText.RemoveShortcut, AppText.Cancel)) return;
        try
        {
            var removed = _selected;
            _selected = null;
            _projects.Remove(removed);
            ProjectService.SaveProjects(_projects);
            SaveButton.IsEnabled = RemoveButton.IsEnabled = AddIndexButton.IsEnabled = false;
            LinkedIndexes.Clear();
            NameInput.Text = PathInput.Text = "";
            StatusLabel.Text = AppText.ProjectShortcutRemoved;
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Shows durable file references and permits repairs without opening storage.</summary>
    private void RenderLinks()
    {
        LinkedIndexes.Clear();
        if (_selected == null) return;
        foreach (var index in _selected.Indexes)
        {
            var row = new Grid { Padding = 10, BackgroundColor = Color.FromArgb("#252526"), ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) } };
            var available = ProjectService.IsAvailable(index);
            var description = new VerticalStackLayout { Spacing = 4 };
            description.Add(new Label { Text = index.Name + (available ? "" : AppText.Missing), FontAttributes = FontAttributes.Bold, TextColor = available ? Colors.White : Colors.Gray });
            description.Add(new Label { Text = index.Path, FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.MiddleTruncation });
            description.Add(new Label { Text = AppText.Format("DocumentLinks", AppText.Count("Links", index.Documents.Count), index.Culture), FontSize = 12 });
            row.Add(description);
            var locate = new Button { Text = AppText.Locate, VerticalOptions = LayoutOptions.Center };
            locate.Clicked += async (_, _) =>
            {
                try
                {
                    var file = await PickIndex();
                    if (file == null) return;
                    if (_selected.Indexes.Any(x => x != index && string.Equals(x.Path, file.FullPath, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException(AppText.ThisIndexIsAlreadyLinked);
                    index.Path = file.FullPath;
                    ProjectService.SaveProjects(_projects);
                    RenderLinks();
                }
                catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
            };
            row.Add(locate, 1);
            var remove = new Button { Text = AppText.Unlink, VerticalOptions = LayoutOptions.Center };
            remove.Clicked += async (_, _) =>
            {
                if (!await DisplayAlertAsync(AppText.RemoveIndexLink, AppText.UnlinkIndexConfirmation, AppText.Unlink, AppText.Cancel)) return;
                try { _selected.Indexes.Remove(index); ProjectService.SaveProjects(_projects); RenderLinks(); }
                catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
            };
            row.Add(remove, 2);
            LinkedIndexes.Add(row);
        }
        if (_selected.Indexes.Count == 0) LinkedIndexes.Add(new Label { Text = AppText.NoIndexesLinkedAddASchemaFileToGetStarted, TextColor = Colors.Gray, Padding = 10 });
    }

    /// <summary>Chooses a schema using the platform file picker.</summary>
    /// <returns>The selected file, or null after cancellation.</returns>
    private static Task<FileResult?> PickIndex() => FilePicker.Default.PickAsync(new PickOptions { PickerTitle = AppText.LinkIndexSchema, FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> { [DevicePlatform.WinUI] = [".ws"] }) });

    /// <summary>Persists an index reference without copying its source files.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event context.</param>
    private async void OnAddIndex(object sender, EventArgs e)
    {
        if (_selected == null) return;
        try
        {
            var file = await PickIndex();
            if (file == null) return;
            ProjectService.LinkIndex(_selected, file.FullPath);
            ProjectService.SaveProjects(_projects);
            RenderLinks();
        }
        catch (Exception error) { StatusLabel.Text = AppText.Diagnostic(error.Message); }
    }

    /// <summary>Returns to the project tree after editing workspace settings.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event context.</param>
    private async void OnExplorer(object sender, EventArgs e)
    {
        if (!await CanLeaveProperties()) return;
        NameInput.Text = _selected?.ProjectName;
        PathInput.Text = _selected?.IndexPath;
        await Shell.Current.GoToAsync("//MainPage");
    }

    /// <summary>Prevents navigation from silently discarding edited project properties.</summary>
    /// <returns>Whether navigation may proceed.</returns>
    private Task<bool> CanLeaveProperties()
    {
        var changed = _selected != null && ((NameInput.Text ?? "") != (_selected.ProjectName ?? "") || (PathInput.Text ?? "") != (_selected.IndexPath ?? ""));
        return changed ? DisplayAlertAsync(AppText.UnsavedProjectProperties, AppText.DiscardTheUnsavedProjectNameOrFolderChanges, AppText.Discard, AppText.KeepEditing) : Task.FromResult(true);
    }
}
