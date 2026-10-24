using System.Collections.ObjectModel;
using WebExpress.WebIndex.WiUI.Model;
using Xunit;

namespace WebExpress.WebIndex.WiUI.Test;

/// <summary>Protects durable project references against missing files, migration and duplicate links.</summary>
public sealed class UnitTestProjectService : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "WiUIProjectTests", Guid.NewGuid().ToString("N"));

    /// <summary>Creates isolated settings and source paths for each test.</summary>
    public UnitTestProjectService() => Directory.CreateDirectory(_directory);

    /// <summary>Verifies that unavailable documents and indexes survive settings round trips.</summary>
    [Fact]
    public void SaveAndLoad_PreservesMissingIndexAndDocumentLinks()
    {
        // arrange
        var project = new Project { ProjectName = "Operations" };
        var index = ProjectService.LinkIndex(project, Path.Combine(_directory, "Missing.ws"));
        index.Culture = "de";
        var id = Guid.NewGuid();
        ProjectService.LinkDocument(index, id, "Pinned server");
        var settings = Path.Combine(_directory, "projects.xml");
        // act
        ProjectService.SaveProjects([project], settings);
        var restored = Assert.Single(ProjectService.LoadProjects(settings));
        ProjectService.Discover(restored);
        // validation
        var link = Assert.Single(restored.Indexes);
        Assert.False(ProjectService.IsAvailable(link));
        Assert.Equal("de", link.Culture);
        Assert.Equal(id, Assert.Single(link.Documents).Id);
        Assert.Equal("Pinned server", link.Documents[0].Name);
    }

    /// <summary>Ensures repeated linking retains the same document references and custom labels.</summary>
    [Fact]
    public void LinkIndexAndDocument_ReuseCanonicalIdentity()
    {
        // arrange
        var project = new Project();
        var path = Path.Combine(_directory, "Index.ws");
        var id = Guid.NewGuid();
        var index = ProjectService.LinkIndex(project, path);
        var document = ProjectService.LinkDocument(index, id, "Custom name");
        // act
        var duplicate = ProjectService.LinkIndex(project, path.ToUpperInvariant());
        var duplicateDocument = ProjectService.LinkDocument(duplicate, id, "New name");
        // validation
        Assert.Same(index, duplicate);
        Assert.Same(document, duplicateDocument);
        Assert.Equal("Custom name", document.Name);
        Assert.Single(project.Indexes);
        Assert.Single(index.Documents);
    }

    /// <summary>Checks that both source files control availability without discarding the reference.</summary>
    [Fact]
    public void MissingFiles_DisableLinkUntilRestored()
    {
        // arrange
        var project = new Project();
        var path = Path.Combine(_directory, "Index.ws");
        var index = ProjectService.LinkIndex(project, path);
        // act
        File.WriteAllText(path, "schema");
        var schemaOnly = ProjectService.IsAvailable(index);
        File.WriteAllText(Path.ChangeExtension(path, ".wds"), "documents");
        var complete = ProjectService.IsAvailable(index);
        File.Move(path, path + ".moved");
        var missing = ProjectService.IsAvailable(index);
        File.Move(path + ".moved", path);
        // validation
        Assert.False(schemaOnly);
        Assert.True(complete);
        Assert.False(missing);
        Assert.True(ProjectService.IsAvailable(index));
        Assert.Same(index, Assert.Single(project.Indexes));
    }

    /// <summary>Migrates the original folder-only XML without losing project selection.</summary>
    [Fact]
    public void LegacyProject_DiscoversIndexesOnceAndPersistsLinks()
    {
        // arrange
        var path = Path.Combine(_directory, "Index.ws");
        File.WriteAllText(path, "schema");
        var settings = Path.Combine(_directory, "legacy.xml");
        File.WriteAllText(settings, $"<ArrayOfProject><Project><ProjectName>Legacy</ProjectName><IndexPath>{System.Security.SecurityElement.Escape(_directory)}</IndexPath><IsSelected>true</IsSelected></Project></ArrayOfProject>");
        // act
        var projects = ProjectService.LoadProjects(settings);
        var project = Assert.Single(projects);
        ProjectService.Discover(project);
        ProjectService.SaveProjects(projects, settings);
        var restored = Assert.Single(ProjectService.LoadProjects(settings));
        // validation
        Assert.True(restored.IsSelected);
        Assert.True(restored.IsDiscovered);
        Assert.Equal(path, Assert.Single(restored.Indexes).Path);
    }

    /// <summary>Ensures removing a reference does not delete files or rediscover the removed link.</summary>
    [Fact]
    public void Unlink_DoesNotDeleteOrRediscoverSource()
    {
        // arrange
        var project = new Project { IndexPath = _directory };
        var path = Path.Combine(_directory, "Index.ws");
        File.WriteAllText(path, "unchanged");
        ProjectService.Discover(project);
        // act
        project.Indexes.Clear();
        ProjectService.Discover(project);
        // validation
        Assert.Empty(project.Indexes);
        Assert.Equal("unchanged", File.ReadAllText(path));
    }

    /// <summary>Removes only test-owned temporary files.</summary>
    public void Dispose() => Directory.Delete(_directory, true);
}
