#nullable enable
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using WebExpress.WebIndex.WebAttribute;
using WebExpress.WebIndex.WiUI.Explorer;
using Xunit;

namespace WebExpress.WebIndex.WiUI.Test;

/// <summary>Exercises the shipped explorer services against real persisted indexes.</summary>
public sealed class UnitTestExplorer : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "WebIndexExplorerTests", Guid.NewGuid().ToString("N"));
    private string SchemaPath => Path.Combine(_directory, nameof(ExplorerTestItem) + ".ws");

    /// <summary>Seeds an isolated storage index for each test.</summary>
    public UnitTestExplorer()
    {
        using var manager = new ExplorerTestManager(_directory);
        manager.Create<ExplorerTestItem>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
        manager.Insert(new ExplorerTestItem { Id = Guid.NewGuid(), Name = "Server alpha", Price = 10.5m, Rank = 10, Address = new ExplorerTestAddress { City = "Berlin" } });
        manager.Insert(new ExplorerTestItem { Id = Guid.NewGuid(), Name = "Server beta", Price = 2.25m, Rank = 2, Address = new ExplorerTestAddress { City = "London" } });
    }

    /// <summary>Reuses the last bounded result until a document mutation invalidates it.</summary>
    [Fact]
    public void Query_ReusesSnapshotAndInvalidatesAfterSaveAndDelete()
    {
        // arrange
        using var session = Open(true);
        var original = session.Query("", TestContext.Current.CancellationToken);

        // act
        var repeated = session.Query("", TestContext.Current.CancellationToken);

        // validation
        Assert.Same(original, repeated);
        var first = original[0];
        var id = (Guid)ExplorerTable.Value(first, "Id")!;
        session.Save(ExplorerTable.Json([first]).TrimStart('[').TrimEnd(']'), id);
        var saved = session.Query("", TestContext.Current.CancellationToken);
        Assert.NotSame(original, saved);
        session.Delete(id);
        Assert.Single(session.Query("", TestContext.Current.CancellationToken));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.Throws<OperationCanceledException>(() => session.Query("", canceled.Token));
    }

    /// <summary>Reevaluates explicit WQL expressions instead of caching time-dependent function results.</summary>
    [Fact]
    public void Query_ExplicitExpressionsAreReevaluated()
    {
        // arrange
        using var session = Open();
        var first = session.Query("Rank > 0", TestContext.Current.CancellationToken);

        // act
        var second = session.Query("Rank > 0", TestContext.Current.CancellationToken);

        // validation
        Assert.NotSame(first, second);
        Assert.Equal(first.Count, second.Count);
    }

    /// <summary>Stops a canceled open before touching source files or creating a snapshot.</summary>
    [Fact]
    public void Snapshot_PreCanceledOpenPreservesSource()
    {
        // arrange
        var before = Hashes();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        // act
        Assert.Throws<OperationCanceledException>(() => new ExplorerSession(SchemaPath, false, CultureInfo.InvariantCulture, canceled.Token));

        // validation
        Assert.Equal(before, Hashes());
    }

    /// <summary>Resolves pinned identities independently of WQL and detects deletion after reopen.</summary>
    [Fact]
    public void LinkedDocument_ResolvesIdentityAndDetectsDeletion()
    {
        // arrange
        Guid id;
        using (var session = Open(true))
        {
            var first = session.Query("", TestContext.Current.CancellationToken)[0];
            id = (Guid)ExplorerTable.Value(first, "Id")!;
            Assert.NotNull(session.Find(id));
            Assert.Single(session.Query($"Id = '{id}'", TestContext.Current.CancellationToken));
            // act
            session.Delete(id);
            // validation
            Assert.Null(session.Find(id));
        }
        using var reopened = Open();
        Assert.Null(reopened.Find(id));
        Assert.Null(reopened.Find(Guid.NewGuid()));
    }

    /// <summary>Ensures inspection leaves every source file byte identical.</summary>
    [Fact]
    public void Snapshot_QueryAndTerms_DoNotChangeSource()
    {
        // arrange
        var before = Hashes();
        // act
        using (var session = Open())
        {
            Assert.Equal(2, session.Query("", TestContext.Current.CancellationToken).Count);
            Assert.Single(session.Query("Price > 5", TestContext.Current.CancellationToken));
            Assert.Single(session.Query("Address.City = 'Berlin'", TestContext.Current.CancellationToken));
            Assert.NotEmpty(session.Terms("Name", TestContext.Current.CancellationToken));
            Assert.Equal(2, session.Terms("Price", TestContext.Current.CancellationToken).Count);
        }
        // validation
        Assert.Equal(before, Hashes());
    }

    /// <summary>Verifies create, update, reverse lookup, delete and reopen as one storage round trip.</summary>
    [Fact]
    public void WritableSession_Crud_PersistsAndUpdatesReverseIndexes()
    {
        // arrange
        var id = Guid.NewGuid();
        var item = new ExplorerTestItem { Id = id, Name = "Orion probe", Rank = 30, Price = 9007199254740993.1234m, Address = new ExplorerTestAddress { City = "Paris" } };
        // act
        using (var session = Open(true))
        {
            session.Save(JsonSerializer.Serialize(item), null);
            Assert.Single(session.Query("Name ~ 'Orion'", TestContext.Current.CancellationToken));
            item.Name = "Vega satellite";
            item.Price = 42.75m;
            session.Save(JsonSerializer.Serialize(item), id);
            Assert.Empty(session.Query("Name ~ 'Orion'", TestContext.Current.CancellationToken));
            Assert.Single(session.Query("Name ~ 'Vega'", TestContext.Current.CancellationToken));
            var updated = session.Query("Name ~ 'Vega'", TestContext.Current.CancellationToken).Single();
            Assert.Equal(item.Price, ExplorerTable.Value(updated, "Price"));
            Assert.Single(session.Query("Price = 42.75", TestContext.Current.CancellationToken));
        }
        // validation
        using (var reopened = Open(true))
        {
            Assert.Single(reopened.Query("Name ~ 'Vega'", TestContext.Current.CancellationToken));
            reopened.Delete(id);
            Assert.Empty(reopened.Query("Name ~ 'Vega'", TestContext.Current.CancellationToken));
            Assert.Equal(2u, reopened.Count);
        }
        using var final = Open();
        Assert.Equal(2u, final.Count);
    }

    /// <summary>Rejects duplicate identifiers and incomplete edits before changing data.</summary>
    [Fact]
    public void InvalidWrites_LeaveDocumentsUnchanged()
    {
        // arrange
        using var session = Open(true);
        var item = session.Query("", TestContext.Current.CancellationToken).First();
        var id = (Guid)ExplorerTable.Value(item, "Id")!;
        var json = JsonSerializer.Serialize(item);
        // act
        Assert.Throws<InvalidDataException>(() => session.Save(json, null));
        Assert.Throws<InvalidDataException>(() => session.Save(json, Guid.NewGuid()));
        Assert.Throws<InvalidDataException>(() => session.Save("{\"Id\":\"" + id + "\"}", id));
        Assert.Throws<InvalidDataException>(() => session.Save(json.Replace("\"Name\":", "\"Name\":\"duplicate\",\"Name\":"), id));
        // validation
        Assert.Equal(2u, session.Count);
        Assert.Equal(json, JsonSerializer.Serialize(session.Query("", TestContext.Current.CancellationToken).First()));
    }

    /// <summary>Prevents all document mutations through a read snapshot.</summary>
    [Fact]
    public void Snapshot_RejectsWrites()
    {
        // arrange
        using var session = Open();
        // act
        Assert.Throws<InvalidOperationException>(() => session.Save(session.NewDocument(), null));
        Assert.Throws<InvalidOperationException>(() => session.Delete(Guid.NewGuid()));
        // validation
        Assert.Equal(2u, session.Count);
    }

    /// <summary>Distinguishes malformed WQL from empty results and honors cancellation.</summary>
    [Fact]
    public void Query_ValidatesAndCancels()
    {
        // arrange
        using var session = Open();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        // act
        var error = session.Validate("Missing = 3");
        var fields = session.Suggest("");
        var operators = session.Suggest("Name ");
        // validation
        Assert.NotNull(error);
        Assert.Contains("Name", fields);
        Assert.Contains("=", operators);
        Assert.Throws<InvalidOperationException>(() => session.Query("Missing = 3", TestContext.Current.CancellationToken));
        Assert.Throws<OperationCanceledException>(() => session.Query("", cancellation.Token));
        Assert.Equal(10, ExplorerTable.Value(Assert.Single(session.Query("order by Rank desc take 1", TestContext.Current.CancellationToken)), "Rank"));
    }

    /// <summary>Preserves typed ordering and nested paths across filtering and export.</summary>
    [Fact]
    public void Table_SortsNumbersAndEscapesCsv()
    {
        // arrange
        var rows = new object[] { new ExplorerTestItem { Rank = 10, Name = "=SUM(1,2)", Address = new ExplorerTestAddress { City = "Berlin" } }, new ExplorerTestItem { Rank = 2, Name = "a,\"b\"\nc" } };
        // act
        var sorted = ExplorerTable.Select(rows, ["Name", "Rank"], "", "Rank", false);
        var filtered = ExplorerTable.Select(rows, ["Address.City"], "berlin", null, false);
        var csv = ExplorerTable.Csv(rows, ["Name", "Rank"]);
        // validation
        Assert.Equal(2, ExplorerTable.Value(sorted[0], "Rank"));
        Assert.Single(filtered);
        Assert.Contains("\"'=SUM(1,2)\"", csv);
        Assert.Contains("\"a,\"\"b\"\"\nc\"", csv);
        Assert.Equal(2, JsonDocument.Parse(ExplorerTable.Json(rows)).RootElement.GetArrayLength());
    }

    /// <summary>Bounds recent queries while retaining scoped favorites across restart.</summary>
    [Fact]
    public void History_RoundTripRetainsFavoritesAndScopes()
    {
        // arrange
        var history = new ExplorerHistory();
        var path = Path.Combine(_directory, "history.json");
        history.Remember(SchemaPath, "Name = 'saved'", true);
        // act
        for (var i = 0; i < 60; i++) history.Remember(SchemaPath, $"Rank = {i}");
        history.Remember("other.ws", "Rank = 59");
        history.Save(path);
        var loaded = ExplorerHistory.Load(path);
        // validation
        Assert.Equal(51, loaded.Queries.Count);
        Assert.Single(loaded.Queries, x => x.Favorite);
        Assert.Equal(2, loaded.Queries.Count(x => x.Query == "Rank = 59"));
    }

    /// <summary>Rejects schema mismatches before a writable connection can migrate the source.</summary>
    [Fact]
    public void WritableSchemaMismatch_DoesNotRewriteSource()
    {
        // arrange
        var json = File.ReadAllText(SchemaPath).Replace("\"Abstract\": false", "\"Abstract\": true");
        File.WriteAllText(SchemaPath, json);
        var before = Hashes();
        // act
        Assert.Throws<InvalidDataException>(() => Open(true));
        // validation
        Assert.Equal(before, Hashes());
    }

    /// <summary>Rejects unsupported schema types without opening storage.</summary>
    [Fact]
    public void UnsupportedType_DoesNotNarrowStoredValues()
    {
        // arrange
        File.WriteAllText(SchemaPath, File.ReadAllText(SchemaPath).Replace("\"Decimal\"", "\"UnknownAmount\""));
        var before = Hashes();
        // act
        Assert.Throws<InvalidDataException>(() => Open());
        // validation
        Assert.Equal(before, Hashes());
    }

    /// <summary>Opens the isolated test schema.</summary>
    /// <param name="writable">Whether writes are enabled.</param>
    /// <returns>The session owned by the caller.</returns>
    private ExplorerSession Open(bool writable = false) => new(SchemaPath, writable, CultureInfo.GetCultureInfo("en"));

    /// <summary>Replaces an entire identifier while retaining following query text.</summary>
    [Fact]
    public void Completion_MidTokenAndQuotedLiteral_PreserveText()
    {
        // arrange
        const string text = "Name = 'O''Brien'";
        var range = ExplorerCompletion.Range(text, 2, 0)!.Value;
        // act
        var inserted = ExplorerCompletion.Insert(text, range.Start, range.End, "Name");
        var literal = ExplorerCompletion.Range(text, 13, 0);
        // validation
        Assert.Equal(text, inserted.Text);
        Assert.Equal(4, inserted.Caret);
        Assert.Null(literal);
    }

    /// <summary>Keeps commas attached to the preceding argument.</summary>
    [Fact]
    public void Completion_Punctuation_DoesNotLeaveLeadingSpace()
    {
        // arrange
        const string text = "Name in ('alpha' ";
        // act
        var inserted = ExplorerCompletion.Insert(text, text.Length, text.Length, ",");
        // validation
        Assert.Equal("Name in ('alpha', ", inserted.Text);
    }

    /// <summary>Releases exclusive source locks after failed schema validation and normal disposal.</summary>
    [Fact]
    public void WritableSession_Dispose_ReleasesStorageLocks()
    {
        // arrange
        var session = Open(true);
        // act
        session.Dispose();
        // validation
        using var source = new FileStream(Path.ChangeExtension(SchemaPath, ".wds"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(source.Length > 0);
    }

    /// <summary>Uses the index language for numeric tokens even when Windows uses another decimal separator.</summary>
    /// <param name="indexType">The engine storage variant.</param>
    [Theory]
    [InlineData(IndexType.Storage)]
    [InlineData(IndexType.Memory)]
    public void NumericIndex_UsesItsCulture(IndexType indexType)
    {
        // arrange
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            using var manager = new ExplorerTestManager(Path.Combine(_directory, "culture"));
            manager.Create<ExplorerTestItem>(CultureInfo.GetCultureInfo("en"), indexType);
            var item = new ExplorerTestItem { Id = Guid.NewGuid(), Price = 2.25m };
            // act
            manager.Insert(item);
            var result = manager.Retrieve<ExplorerTestItem>("Price < 5").ToArray();
            // validation
            Assert.Single(result);
            Assert.Equal(2.25m, result[0].Price);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    /// <summary>Captures names and content hashes to detect source mutations.</summary>
    /// <returns>The deterministic fingerprint.</returns>
    private string[] Hashes() => Directory.GetFiles(_directory).Order().Select(path => Path.GetFileName(path) + ":" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToArray();

    /// <summary>Removes only the temporary directory owned by this test.</summary>
    public void Dispose() => Directory.Delete(_directory, true);
}

/// <summary>Provides the real storage engine for explorer fixtures.</summary>
internal sealed class ExplorerTestManager : WebIndex.IndexManager
{
    /// <summary>Initializes an isolated storage context.</summary>
    /// <param name="directory">The test directory.</param>
    public ExplorerTestManager(string directory) => Initialization(new IndexContext { IndexDirectory = directory });
}

/// <summary>Exercises scalar and nested storage contracts.</summary>
public sealed class ExplorerTestItem : IIndexItem
{
    /// <summary>Gets or sets the stable identifier.</summary>
    [IndexIgnore]
    public Guid Id { get; set; }
    /// <summary>Gets or sets indexed text.</summary>
    public string? Name { get; set; }
    /// <summary>Gets or sets an exact decimal.</summary>
    public decimal Price { get; set; }
    /// <summary>Gets or sets numeric ordering data.</summary>
    public int Rank { get; set; }
    /// <summary>Gets or sets nested data.</summary>
    public ExplorerTestAddress? Address { get; set; }
}

/// <summary>Provides a nested indexed property.</summary>
public sealed class ExplorerTestAddress
{
    /// <summary>Gets or sets the indexed city.</summary>
    public string? City { get; set; }
}
