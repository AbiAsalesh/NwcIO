# NwcIO

**NwcIO** is a fast, 100% managed .NET library and CLI tool for inspecting and reading Autodesk Navisworks files (`.nwc`, `.nwd`) **without requiring Autodesk Navisworks to be installed**.

Mirrors the architectural principles of [mahach666/RvtIO](https://github.com/mahach666/RvtIO): clean-room reverse-engineering, zero native dependencies, high performance, and structured JSON export.

## Features

- **No Navisworks Required**: Zero dependencies on Navisworks Manage, Simulate, Freedom, or Autodesk ObjectARX/Navisworks .NET API.
- **Pure Managed .NET**: Targets `net8.0` and `net10.0`.
- **LcUStream Container Parser**: Stream-based ingestion of `#LcUStream` sections, back-to-back zlib stream decompression, and automatic resync framing.
- **Hierarchical Scene Graph**: Reconstructs Navisworks selection tree (Layers, Categories, Families, Types, Composite groups, Element instances) with nested properties.
- **3D Spatial Transforms**: Decodes instance poses (3D translation, rotation quaternions, local bounding boxes) and computes 4x4 affine transformation matrices.
- **GUID Extraction**: Extracts raw 16-byte element GUID tables.
- **Metadata**: Reads source document paths (.rvt, .dwg, etc.), modification timestamps, and exporter plugin identifiers.
- **JSON Exporter & CLI**: Exports scene metadata, hierarchy tree, properties, and 3D transforms to structured JSON.

## CLI Usage

Install or run via `dotnet`:

```bash
# Export to JSON
nwcio input.nwc output.json

# Export with detailed execution report to stderr
nwcio input.nwc output.json --report

# Include raw container stream catalog in JSON output
nwcio input.nwc output.json --catalog

# Compact (non-indented) JSON output
nwcio input.nwc output.json --no-indent
```

### Exit Codes
- `0`: Success
- `1`: Usage error (missing or invalid flags)
- `2`: File not found
- `3`: Data format error or parsing failure

## Library Usage (C#)

```csharp
using NwcIO;

// Open and parse document
var doc = NwcDocument.Open("model.nwc");

// File metadata
Console.WriteLine($"Source RVT: {doc.FileInfo.SourcePath}");
Console.WriteLine($"Exported:   {doc.FileInfo.SourceTimestampUtc}");
Console.WriteLine($"Plugin:     {doc.FileInfo.LoaderPlugin}");

// Elements & properties
Console.WriteLine($"Found {doc.PropertyRecords.Count} property records:");
foreach (var record in doc.PropertyRecords)
{
    Console.WriteLine($"  [{record.Tag}] {record.Name} ({record.Class}) = {record.Value}");
}

// GUIDs
Console.WriteLine($"Found {doc.Guids.Count} GUIDs");

// Save to JSON
doc.SaveJson("model.json");
```

## Documentation

- Format notes: [docs/format-notes.md](docs/format-notes.md)

## License

MIT License. Copyright (c) 2026 mahach666.
