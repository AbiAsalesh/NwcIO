namespace NwcIO;

/// <summary>Options controlling what <see cref="NwcDocument.Open"/> reads and keeps.</summary>
public sealed class NwcReaderOptions
{
    public static NwcReaderOptions Default { get; } = new();

    /// <summary>Parse the scene-tree/property streams (default: true).</summary>
    public bool ReadPropertyRecords { get; set; } = true;

    /// <summary>Parse GUID tables (default: true).</summary>
    public bool ReadGuids { get; set; } = true;

    /// <summary>Keep the stream catalog (offsets, sizes, kinds) in the document (default: true).</summary>
    public bool KeepStreamCatalog { get; set; } = true;
}
