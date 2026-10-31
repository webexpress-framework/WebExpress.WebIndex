#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WebExpress.WebIndex.WiUI.Model;

/// <summary>Stores a named workspace and its durable index and document links.</summary>
public class Project : INotifyPropertyChanged
{
    private string? _projectName;
    private string? _indexPath;

    /// <summary>Notifies project editors when the displayed name or path changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>Gets or sets the project display name.</summary>
    public string? ProjectName { get => _projectName; set { _projectName = value; OnPropertyChanged(); } }
    /// <summary>Gets or sets the optional directory used when discovering indexes.</summary>
    public string? IndexPath { get => _indexPath; set { _indexPath = value; OnPropertyChanged(); } }
    /// <summary>Gets or sets the project restored on startup.</summary>
    public bool? IsSelected { get; set; }
    /// <summary>Gets or sets whether legacy folder discovery has completed.</summary>
    public bool IsDiscovered { get; set; }
    /// <summary>Gets or sets links that survive missing or moved files.</summary>
    public ObservableCollection<ProjectIndex> Indexes { get; set; } = [];

    /// <summary>Notifies bindings without introducing platform dependencies into persistence.</summary>
    /// <param name="propertyName">The changed property.</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>Retains an index file reference independently of its current availability.</summary>
public sealed class ProjectIndex
{
    /// <summary>Gets or sets the schema path.</summary>
    public string Path { get; set; } = "";
    /// <summary>Gets or sets the friendly tree label.</summary>
    public string Name { get; set; } = "";
    /// <summary>Gets or sets the token language used when opening this index.</summary>
    public string Culture { get; set; } = "en";
    /// <summary>Gets or sets the durable document references below this index.</summary>
    public ObservableCollection<ProjectDocument> Documents { get; set; } = [];
}

/// <summary>Retains a stable document reference even when the source document is deleted.</summary>
public sealed class ProjectDocument
{
    /// <summary>Gets or sets the immutable document identity.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the label shown in the project tree.</summary>
    public string Name { get; set; } = "";
}
