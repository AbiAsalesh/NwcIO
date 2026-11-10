using System.Text.Json;

namespace NwcIO;

/// <summary>Options for the JSON export.</summary>
public sealed class NwcJsonExportOptions
{
    public static NwcJsonExportOptions Default { get; } = new();

    /// <summary>Indent the output (default: true).</summary>
    public bool Indented { get; set; } = true;

    /// <summary>Include the per-stream container catalog in the output (default: false).</summary>
    public bool IncludeStreamCatalog { get; set; }
}
