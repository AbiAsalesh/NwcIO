using NwcIO.Internal.Container;
using NwcIO.Internal.Parsing;
using NwcIO.Model;

namespace NwcIO;

/// <summary>
/// An opened Navisworks document (.nwc cache, .nwd document) parsed without Navisworks installed.
/// </summary>
public sealed class NwcDocument
{
    private NwcDocument(
        NwcFileInfo fileInfo,
        IReadOnlyList<NwcPropertyRecord> records,
        IReadOnlyList<Guid> guids,
        IReadOnlyList<NwcTransform> transforms,
        IReadOnlyList<NwcMesh> meshes,
        NwcNode? rootNode,
        NwcContainer container)
    {
        FileInfo = fileInfo;
        PropertyRecords = records;
        Guids = guids;
        Transforms = transforms;
        Meshes = meshes;
        RootNode = rootNode;
        Container = container;
    }

    public NwcFileInfo FileInfo { get; }

    /// <summary>Flat records parsed from the scene-tree/property streams.</summary>
    public IReadOnlyList<NwcPropertyRecord> PropertyRecords { get; }

    /// <summary>GUIDs from the GUID table streams, in file order.</summary>
    public IReadOnlyList<Guid> Guids { get; }

    /// <summary>3D spatial transformations and bounding boxes parsed from transform streams.</summary>
    public IReadOnlyList<NwcTransform> Transforms { get; }

    /// <summary>3D polygon meshes extracted from geometry streams and TOC tables.</summary>
    public IReadOnlyList<NwcMesh> Meshes { get; }

    /// <summary>Root of the hierarchical scene graph tree (matching Navisworks selection tree).</summary>
    public NwcNode? RootNode { get; }

    internal NwcContainer Container { get; }

    /// <summary>Opens and parses a Navisworks file.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not an LcUStream container.</exception>
    public static NwcDocument Open(string path, NwcReaderOptions? options = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Navisworks file not found: {path}", path);
        }

        options ??= NwcReaderOptions.Default;

        var container = NwcContainerReader.ReadFile(path);
        StreamClassifier.ClassifyAll(container.Streams);

        var fileInfo = new NwcFileInfo();
        var headerStream = container.Streams.FirstOrDefault(s => s.Kind == NwcStreamKind.PartitionHeader);
        if (headerStream is not null)
        {
            fileInfo = PartitionHeaderParser.Parse(headerStream);
        }

        var records = new List<NwcPropertyRecord>();
        if (options.ReadPropertyRecords)
        {
            foreach (var stream in container.Streams.Where(s => s.Kind == NwcStreamKind.SceneTree))
            {
                var scanner = new PropertyRecordScanner(stream.Data);
                records.AddRange(scanner.Scan());
            }
        }

        var guids = new List<Guid>();
        if (options.ReadGuids)
        {
            foreach (var stream in container.Streams.Where(s => s.Kind == NwcStreamKind.Guids))
            {
                if (GuidTableParser.TryParse(stream, out var parsed))
                {
                    guids.AddRange(parsed);
                }
            }
        }

        var transforms = new List<NwcTransform>();
        if (options.ReadTransforms)
        {
            foreach (var stream in container.Streams.Where(s => s.Kind == NwcStreamKind.Transforms))
            {
                transforms.AddRange(TransformParser.Parse(stream));
            }
        }

        var meshes = new List<NwcMesh>();
        if (options.ReadMeshes)
        {
            meshes.AddRange(MeshParser.Parse(container, transforms));
        }

        NwcNode? rootNode = null;
        if (options.BuildSceneTree)
        {
            string rootName = Path.GetFileNameWithoutExtension(fileInfo.SourcePath ?? path);
            rootNode = SceneTreeBuilder.Build(rootName, records, transforms, guids);
        }

        if (!options.KeepStreamCatalog)
        {
            container = container with { Streams = [] };
        }

        return new NwcDocument(fileInfo, records, guids, transforms, meshes, rootNode, container);
    }

    /// <summary>Exports 3D mesh geometry to a Wavefront OBJ file.</summary>
    public void SaveObj(string path, bool worldCoordinates = true)
    {
        Internal.Export.ObjExporter.WriteFile(Meshes, path, worldCoordinates, FileInfo.SourcePath);
    }

    /// <summary>Exports 3D mesh geometry to a <see cref="TextWriter"/> in Wavefront OBJ format.</summary>
    public void SaveObj(TextWriter writer, bool worldCoordinates = true)
    {
        Internal.Export.ObjExporter.Write(Meshes, writer, worldCoordinates, FileInfo.SourcePath);
    }

    /// <summary>Exports the document to a JSON file.</summary>
    public void SaveJson(string path, NwcJsonExportOptions? options = null)
    {
        options ??= NwcJsonExportOptions.Default;
        Internal.Json.JsonExporter.WriteFile(this, path, options);
    }

    /// <summary>Exports the document to a JSON stream.</summary>
    public void SaveJson(Stream stream, NwcJsonExportOptions? options = null)
    {
        options ??= NwcJsonExportOptions.Default;
        Internal.Json.JsonExporter.Write(this, stream, options);
    }

    /// <summary>Serializes the document to a JSON string.</summary>
    public string ToJson(NwcJsonExportOptions? options = null)
    {
        using var ms = new MemoryStream();
        SaveJson(ms, options);
        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }
}
