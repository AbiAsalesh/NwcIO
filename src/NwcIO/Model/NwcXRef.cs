namespace NwcIO.Model;

/// <summary>
/// Represents an external cross-reference (XRef) to an external file
/// (e.g., texture, CAD file, link model) referenced by a Navisworks document.
/// </summary>
public sealed record NwcXRef(string OriginalPath, string RemappedPath)
{
    /// <summary>
    /// Gets the most specific available path (remapped path if present, otherwise original path).
    /// </summary>
    public string EffectivePath => !string.IsNullOrEmpty(RemappedPath) ? RemappedPath : OriginalPath;

    /// <summary>
    /// Checks whether the referenced file exists on disk, optionally relative to a base directory.
    /// </summary>
    public bool Exists(string? baseDirectory = null)
    {
        var path = EffectivePath;
        if (File.Exists(path))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(baseDirectory))
        {
            var combined = Path.Combine(baseDirectory, Path.GetFileName(path));
            if (File.Exists(combined))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Table of external references embedded in the Navisworks file container.
/// </summary>
public sealed record NwcXRefTable(
    string Type,
    int Version,
    string SavedFilename,
    IReadOnlyList<NwcXRef> References);
