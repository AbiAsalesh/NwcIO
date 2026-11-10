namespace NwcIO.Model;

/// <summary>
/// Represents a named segment or table entry recorded in the Navisworks database cache stream.
/// </summary>
/// <param name="Name">Segment name (e.g., "cache\LcOpNwdGeometry", "cache\LcOpGuidStore").</param>
/// <param name="Flags">Flags or schema version associated with the segment.</param>
/// <param name="Offset">Relative byte offset inside the partition or cache stream.</param>
/// <param name="Length">Byte length of the segment data.</param>
/// <param name="Extra">Additional format or alignment flags.</param>
public sealed record NwcDbCacheEntry(
    string Name,
    int Flags,
    long Offset,
    long Length,
    long Extra);

/// <summary>
/// Master directory of cached data tables registered in the <c>LcOpDBCache</c> stream.
/// </summary>
public sealed record NwcDbCacheDirectory(
    string DatabaseName,
    IReadOnlyList<NwcDbCacheEntry> Entries)
{
    /// <summary>
    /// Finds a cache entry by its segment name (case-insensitive).
    /// </summary>
    public NwcDbCacheEntry? Find(string segmentName)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            if (string.Equals(Entries[i].Name, segmentName, StringComparison.OrdinalIgnoreCase) ||
                Entries[i].Name.EndsWith(segmentName, StringComparison.OrdinalIgnoreCase))
            {
                return Entries[i];
            }
        }

        return null;
    }
}
