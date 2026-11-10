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
        IReadOnlyList<NwcMaterial> materials,
        NwcNode? rootNode,
        NwcXRefTable? xRefs,
        NwcDbCacheDirectory? dbCache,
        IReadOnlyList<NwcSavedViewpoint> savedViewpoints,
        NwcContainer container)
    {
        FileInfo = fileInfo;
        PropertyRecords = records;
        Guids = guids;
        Transforms = transforms;
        Meshes = meshes;
        Materials = materials;
        RootNode = rootNode;
        XRefs = xRefs;
        DbCache = dbCache;
        SavedViewpoints = savedViewpoints;
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

    /// <summary>Surface materials, RGBA colors and PBR properties extracted from properties and categories.</summary>
    public IReadOnlyList<NwcMaterial> Materials { get; }

    /// <summary>Root of the hierarchical scene graph tree (matching Navisworks selection tree).</summary>
    public NwcNode? RootNode { get; }

    /// <summary>External references (XRefs) parsed from XRefJson streams.</summary>
    public NwcXRefTable? XRefs { get; }

    /// <summary>Database cache directory parsed from LcOpDBCache streams.</summary>
    public NwcDbCacheDirectory? DbCache { get; }

    /// <summary>Saved viewpoints and camera views extracted from container viewpoint streams.</summary>
    public IReadOnlyList<NwcSavedViewpoint> SavedViewpoints { get; }

    /// <summary>Gets the total 3D bounding box over all meshes in the document.</summary>
    public BoundingBox3d BoundingBox => SpatialIndex.Bounds;

    /// <summary>Gets the primary/default viewpoint of the document.</summary>
    public NwcSavedViewpoint? DefaultViewpoint =>
        SavedViewpoints.Count > 0 ? SavedViewpoints[0] : (!BoundingBox.IsEmpty ? NwcSavedViewpoint.CreateDefault(BoundingBox) : null);

    private Model.Spatial.NwcSpatialIndex? _spatialIndex;

    /// <summary>3D spatial acceleration index (BVH) over meshes for raycasting and spatial selection.</summary>
    public Model.Spatial.NwcSpatialIndex SpatialIndex => _spatialIndex ??= new Model.Spatial.NwcSpatialIndex(Meshes);

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

        var materials = new List<NwcMaterial>();
        if (options.ReadMaterials)
        {
            materials.AddRange(MaterialParser.Parse(records, meshes, rootNode));
        }

        NwcXRefTable? xRefs = null;
        var xrefStream = container.Streams.FirstOrDefault(s => s.Kind == NwcStreamKind.XRefJson);
        if (xrefStream is not null)
        {
            xRefs = XRefTableParser.Parse(xrefStream);
        }

        NwcDbCacheDirectory? dbCache = null;
        var dbCacheStream = container.Streams.FirstOrDefault(s => s.Kind == NwcStreamKind.DbCache);
        if (dbCacheStream is not null)
        {
            dbCache = DbCacheParser.Parse(dbCacheStream);
        }

        var savedViewpoints = new List<NwcSavedViewpoint>();
        if (options.ReadSavedViewpoints)
        {
            foreach (var stream in container.Streams.Where(s => s.Kind == NwcStreamKind.SavedViews))
            {
                savedViewpoints.AddRange(ViewpointParser.Parse(stream));
            }
        }

        if (!options.KeepStreamCatalog)
        {
            container = container with { Streams = [] };
        }

        return new NwcDocument(fileInfo, records, guids, transforms, meshes, materials, rootNode, xRefs, dbCache, savedViewpoints, container);
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

    /// <summary>Exports 3D mesh geometry, materials, and scene hierarchy to a binary glTF 2.0 file (.glb).</summary>
    public void SaveGlb(string path, bool worldCoordinates = true)
    {
        Internal.Export.GlbExporter.WriteFile(Meshes, Materials, path, worldCoordinates, FileInfo.LoaderPlugin, SavedViewpoints);
    }

    /// <summary>Exports 3D mesh geometry, materials, and scene hierarchy to a binary glTF 2.0 stream (.glb).</summary>
    public void SaveGlb(Stream stream, bool worldCoordinates = true)
    {
        Internal.Export.GlbExporter.Write(Meshes, Materials, stream, worldCoordinates, FileInfo.LoaderPlugin, SavedViewpoints);
    }

    /// <summary>Exports 3D mesh geometry, materials, spatial structure, and properties to an IFC2x3 STEP file (.ifc).</summary>
    public void SaveIfc(string path, NwcIfcExportOptions? options = null)
    {
        Internal.Export.IfcExporter.WriteFile(this, path, options);
    }

    /// <summary>Exports 3D mesh geometry, materials, spatial structure, and properties to a <see cref="TextWriter"/> in IFC2x3 format.</summary>
    public void SaveIfc(TextWriter writer, NwcIfcExportOptions? options = null)
    {
        Internal.Export.IfcExporter.Write(this, writer, options);
    }

    /// <summary>Exports saved viewpoints to a buildingSMART BCF 2.1 zip archive (.bcfzip).</summary>
    public void SaveViewpointsBcf(string path)
    {
        Internal.Export.BcfExporter.WriteViewpointsFile(SavedViewpoints, path, Path.GetFileNameWithoutExtension(FileInfo.SourcePath ?? "Model"));
    }

    /// <summary>Exports saved viewpoints to a buildingSMART BCF 2.1 zip stream.</summary>
    public void SaveViewpointsBcf(Stream stream)
    {
        Internal.Export.BcfExporter.WriteViewpoints(SavedViewpoints, stream, Path.GetFileNameWithoutExtension(FileInfo.SourcePath ?? "Model"));
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
