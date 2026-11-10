using NwcIO.Internal.Container;

namespace NwcIO;

/// <summary>
/// Provides methods for serializing and saving <see cref="NwcDocument"/> instances into
/// Autodesk Navisworks (.nwc) files using pure managed compression and binary layouts.
/// </summary>
public static class NwcWriter
{
    /// <summary>
    /// Writes the document to the specified file path.
    /// </summary>
    public static void Write(NwcDocument document, string path, string? header = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var fs = File.Create(path);
        Write(document, fs, header);
    }

    /// <summary>
    /// Writes the document to the specified output stream.
    /// </summary>
    public static void Write(NwcDocument document, Stream stream, string? header = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);

        var streams = NwcStreamSerializer.SerializeDocument(document);
        NwcContainerWriter.Write(stream, streams, header);
    }

    /// <summary>
    /// Serializes the document into a byte array containing a complete .nwc file.
    /// </summary>
    public static byte[] WriteToBytes(NwcDocument document, string? header = null)
    {
        using var ms = new MemoryStream();
        Write(document, ms, header);
        return ms.ToArray();
    }
}
