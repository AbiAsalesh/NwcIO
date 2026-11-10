using System.Text.Json;
using NwcIO.Internal.Container;

namespace NwcIO.Internal.Json;

/// <summary>Writes a parsed <see cref="NwcDocument"/> to JSON.</summary>
public static class JsonExporter
{
    public static void Write(NwcDocument document, Stream output, NwcJsonExportOptions options)
    {
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions
        {
            Indented = options.Indented,
        });

        writer.WriteStartObject();

        writer.WriteStartObject("file");
        writer.WriteString("sourcePath", document.FileInfo.SourcePath);
        writer.WriteString("sourceTimestampUtc", document.FileInfo.SourceTimestampUtc?.ToString("O"));
        writer.WriteString("loaderPlugin", document.FileInfo.LoaderPlugin);
        writer.WriteStartArray("options");
        foreach (var option in document.FileInfo.Options)
        {
            writer.WriteStringValue(option);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.WriteNumber("propertyRecordCount", document.PropertyRecords.Count);
        writer.WriteStartArray("records");
        foreach (var record in document.PropertyRecords)
        {
            writer.WriteStartObject();
            writer.WriteNumber("firstInt", record.FirstInt);
            writer.WriteNumber("tag", record.Tag);
            writer.WriteString("name", record.Name);
            writer.WriteString("class", record.Class);
            if (record.Value is not null)
            {
                writer.WriteNumber("valueType", record.ValueType);
                writer.WriteString("value", record.Value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteNumber("guidCount", document.Guids.Count);
        writer.WriteStartArray("guids");
        foreach (var guid in document.Guids)
        {
            writer.WriteStringValue(guid.ToString("D"));
        }

        writer.WriteEndArray();

        if (options.IncludeStreamCatalog)
        {
            WriteStreamCatalog(writer, document);
        }

        writer.WriteEndObject();
        writer.Flush();
    }

    public static void WriteFile(NwcDocument document, string path, NwcJsonExportOptions options)
    {
        using var stream = File.Create(path);
        Write(document, stream, options);
    }

    private static void WriteStreamCatalog(Utf8JsonWriter writer, NwcDocument document)
    {
        var container = document.Container;
        writer.WriteStartObject("container");

        writer.WriteStartArray("sections");
        foreach (var section in container.Sections)
        {
            writer.WriteStartObject();
            writer.WriteNumber("fileOffset", section.FileOffset);
            writer.WriteString("format", section.Format);
            writer.WriteStartArray("tokens");
            foreach (var token in section.Tokens)
            {
                writer.WriteStringValue(token);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteNumber("streamCount", container.Streams.Count);
        writer.WriteStartArray("streams");
        foreach (var stream in container.Streams)
        {
            writer.WriteStartObject();
            writer.WriteNumber("index", stream.Index);
            writer.WriteNumber("fileOffset", stream.FileOffset);
            writer.WriteNumber("compressedLength", stream.CompressedLength);
            writer.WriteNumber("decompressedLength", stream.Data.Length);
            if (stream.GapBefore > 0)
            {
                writer.WriteNumber("gapBefore", stream.GapBefore);
            }

            writer.WriteString("kind", stream.Kind.ToString());
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        if (container.TrailingGap > 0)
        {
            writer.WriteNumber("trailingGap", container.TrailingGap);
        }

        writer.WriteEndObject();
    }
}
