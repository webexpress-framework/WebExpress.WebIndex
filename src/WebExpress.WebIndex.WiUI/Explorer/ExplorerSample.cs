#nullable enable
using System.Globalization;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.WiUI.Explorer;

/// <summary>Creates a separate sample index for learning queries and document editing.</summary>
public static class ExplorerSample
{
    /// <summary>Creates sample data once without replacing an existing index.</summary>
    /// <param name="directory">The dedicated sample directory.</param>
    /// <returns>The sample schema path.</returns>
    public static string Create(string directory)
    {
        var schema = Path.Combine(directory, nameof(ExplorerSampleAsset) + ".ws");
        if (File.Exists(schema)) return schema;
        using var manager = new SampleManager(directory);
        manager.Create<ExplorerSampleAsset>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
        var sites = new[] { "Berlin", "London", "Paris", "Amsterdam" };
        for (var i = 1; i <= 128; i++)
        {
            manager.Insert(new ExplorerSampleAsset
            {
                Id = Guid.NewGuid(), Name = $"{(i % 3 == 0 ? "Database" : "Application")} server {i:000}",
                Site = sites[i % sites.Length], Status = i % 7 == 0 ? "Maintenance" : "Online",
                Capacity = i * 16, Cost = 125.50m + i * 3.25m, Notes = i % 5 == 0 ? null : "Sample asset for exploring WQL."
            });
        }
        return schema;
    }

    /// <summary>Owns the dedicated sample storage during initialization.</summary>
    private sealed class SampleManager : WebIndex.IndexManager
    {
        /// <summary>Uses an explicitly supplied sample directory.</summary>
        /// <param name="directory">The sample directory.</param>
        public SampleManager(string directory) => Initialization(new IndexContext { IndexDirectory = directory });
    }
}

/// <summary>Provides realistic scalar data for the built-in sample workspace.</summary>
public sealed class ExplorerSampleAsset : IIndexItem
{
    /// <summary>Gets or sets the stable document identity.</summary>
    [IndexIgnore]
    public Guid Id { get; set; }
    /// <summary>Gets or sets the asset label.</summary>
    public string Name { get; set; } = "";
    /// <summary>Gets or sets the deployment site.</summary>
    public string Site { get; set; } = "";
    /// <summary>Gets or sets the operating state.</summary>
    public string Status { get; set; } = "";
    /// <summary>Gets or sets the provisioned capacity.</summary>
    public int Capacity { get; set; }
    /// <summary>Gets or sets the monthly cost.</summary>
    public decimal Cost { get; set; }
    /// <summary>Gets or sets optional maintenance notes.</summary>
    public string? Notes { get; set; }
}
