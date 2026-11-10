using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using NwcIO;
using NwcIO.Internal.Json;
using NwcIO.Model;
using NwcIO.Model.Spatial;

namespace NwcIO.Cli;

internal static class Program
{
    private const string UsageText = """
        Usage: nwcio <input.nwc|input.nwd|input.nwf|input_dir> [output.json|output.obj|output.glb] [options]
               nwcio --help | --version

        Reads Autodesk Navisworks files (.nwc, .nwd, .nwf) or assemblies of models
        and exports scene trees, properties, 3D transforms, materials, and 3D polygon meshes
        (GLB/OBJ/JSON) with BVH spatial acceleration, without requiring Navisworks.

        Arguments:
          <input>            Path to .nwc, .nwd, .nwf file or a folder of .nwc models.
          [output]           Destination path for exported file (.json, .obj, or .glb).
                             Default: <input-without-ext>.json (or standard output if '-').

        Options:
          --glb <file.glb>   Export 3D geometry and materials to glTF 2.0 Binary format (GLB)
          --obj <file.obj>   Export 3D geometry to Wavefront OBJ format
          --local-coords     Export geometry in local coordinate space (default: world coordinates)
          --no-meshes        Disable 3D mesh geometry extraction
          --no-materials     Disable material and surface color extraction
          --catalog          Include raw container stream catalog (offsets, sizes, kinds)
          --no-transforms    Exclude 3D transform matrices from JSON export
          --no-tree          Exclude hierarchical scene graph tree from JSON export
          --no-indent        Write compact JSON instead of pretty-printed indented JSON
          --bvh-stats        Include Bounding Volume Hierarchy (BVH) spatial index statistics
          --raycast <coords> Perform 3D raycast test: "originX,originY,originZ,dirX,dirY,dirZ"
          --clashes          Run cross-model clash detection for multi-model assemblies
          --report           Print summary report to standard error
          -h, --help         Show this help message and exit
          -v, --version      Show version information and exit

        Exit codes:
          0  Success
          1  Usage error (missing or invalid arguments)
          2  File or folder not found
          3  Data format error or parsing failure
        """;

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine(UsageText);
            return args.Length == 0 ? 1 : 0;
        }

        if (args.Contains("--version") || args.Contains("-v"))
        {
            var version = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(Program).Assembly.GetName().Version?.ToString()
                ?? "0.1.0";
            Console.WriteLine($"nwcio {version}");
            return 0;
        }

        string? inputPath = null;
        string? outputPath = null;
        string? explicitObjPath = null;
        string? explicitGlbPath = null;
        string? raycastArg = null;
        bool checkClashes = false;
        bool bvhStats = false;
        bool worldCoords = true;
        bool readMeshes = true;
        bool readMaterials = true;
        bool includeCatalog = false;
        bool includeTransforms = true;
        bool includeTree = true;
        bool indent = true;
        bool printReport = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--catalog":
                    includeCatalog = true;
                    break;
                case "--no-transforms":
                    includeTransforms = false;
                    break;
                case "--no-tree":
                    includeTree = false;
                    break;
                case "--no-meshes":
                    readMeshes = false;
                    break;
                case "--no-materials":
                    readMaterials = false;
                    break;
                case "--local-coords":
                    worldCoords = false;
                    break;
                case "--bvh-stats":
                    bvhStats = true;
                    break;
                case "--clashes":
                    checkClashes = true;
                    break;
                case "--raycast":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: Missing argument for --raycast.");
                        return 1;
                    }
                    raycastArg = args[++i];
                    break;
                case "--obj":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: Missing argument for --obj.");
                        return 1;
                    }
                    explicitObjPath = args[++i];
                    break;
                case "--glb":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: Missing argument for --glb.");
                        return 1;
                    }
                    explicitGlbPath = args[++i];
                    break;
                case "--no-indent":
                    indent = false;
                    break;
                case "--report":
                    printReport = true;
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        Console.Error.WriteLine($"Error: Unrecognized option '{arg}'.");
                        Console.Error.WriteLine(UsageText);
                        return 1;
                    }

                    if (inputPath is null)
                    {
                        inputPath = arg;
                    }
                    else if (outputPath is null)
                    {
                        outputPath = arg;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error: Unexpected extra argument '{arg}'.");
                        return 1;
                    }

                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            Console.Error.WriteLine("Error: Missing input file path.");
            Console.Error.WriteLine(UsageText);
            return 1;
        }

        bool isDirectory = Directory.Exists(inputPath);
        bool isFile = File.Exists(inputPath);

        if (!isDirectory && !isFile)
        {
            Console.Error.WriteLine($"Error: Input '{inputPath}' does not exist as file or directory.");
            return 2;
        }

        var sw = Stopwatch.StartNew();

        // Handle multi-model directory assembly
        if (isDirectory)
        {
            return ProcessAssemblyDirectory(
                inputPath, outputPath, explicitGlbPath, explicitObjPath,
                worldCoords, checkClashes, bvhStats, raycastArg, printReport, indent, sw);
        }

        // Handle NWF assembly file
        if (inputPath.EndsWith(".nwf", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessNwfAssembly(
                inputPath, outputPath, explicitGlbPath, explicitObjPath,
                worldCoords, checkClashes, bvhStats, raycastArg, printReport, indent, sw);
        }

        // Single NWC/NWD document
        outputPath ??= Path.ChangeExtension(inputPath, ".json");

        NwcDocument doc;
        try
        {
            doc = NwcDocument.Open(inputPath, new NwcReaderOptions
            {
                ReadMeshes = readMeshes,
                ReadMaterials = readMaterials,
                ReadPropertyRecords = true,
                ReadGuids = true,
                ReadTransforms = includeTransforms,
                BuildSceneTree = includeTree,
                KeepStreamCatalog = includeCatalog,
            });
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine($"Error: File '{inputPath}' is not a valid Navisworks container: {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading '{inputPath}': {ex.Message}");
            return 3;
        }

        bool outputIsObj = outputPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase);
        bool outputIsGlb = outputPath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase);

        try
        {
            if (outputIsGlb)
            {
                EnsureParentDirectory(outputPath);
                doc.SaveGlb(outputPath, worldCoords);
            }
            else if (outputIsObj)
            {
                EnsureParentDirectory(outputPath);
                doc.SaveObj(outputPath, worldCoords);
            }
            else
            {
                var exportOptions = new NwcJsonExportOptions
                {
                    Indented = indent,
                    IncludeTransforms = includeTransforms,
                    IncludeSceneTree = includeTree,
                    IncludeMeshes = readMeshes,
                    IncludeMaterials = readMaterials,
                    IncludeStreamCatalog = includeCatalog,
                };

                if (outputPath == "-")
                {
                    using var stdout = Console.OpenStandardOutput();
                    JsonExporter.Write(doc, stdout, exportOptions);
                }
                else
                {
                    EnsureParentDirectory(outputPath);
                    JsonExporter.WriteFile(doc, outputPath, exportOptions);
                }
            }

            if (!string.IsNullOrEmpty(explicitGlbPath))
            {
                EnsureParentDirectory(explicitGlbPath);
                doc.SaveGlb(explicitGlbPath, worldCoords);
            }

            if (!string.IsNullOrEmpty(explicitObjPath))
            {
                EnsureParentDirectory(explicitObjPath);
                doc.SaveObj(explicitObjPath, worldCoords);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error exporting to '{outputPath}': {ex.Message}");
            return 3;
        }

        // Raycast test if requested
        if (!string.IsNullOrEmpty(raycastArg))
        {
            ExecuteRaycastTest(doc.SpatialIndex, raycastArg);
        }

        sw.Stop();

        if (printReport)
        {
            int treeNodeCount = CountNodes(doc.RootNode);
            int totalVertices = doc.Meshes.Sum(m => m.Vertices.Count);
            int totalTriangles = doc.Meshes.Sum(m => m.TriangleCount);

            Console.Error.WriteLine("=== NwcIO Execution Report ===");
            Console.Error.WriteLine($"Input:            {inputPath}");
            Console.Error.WriteLine($"Source document:  {doc.FileInfo.SourcePath ?? "<unknown>"}");
            Console.Error.WriteLine($"Loader plugin:    {doc.FileInfo.LoaderPlugin ?? "<unknown>"}");
            Console.Error.WriteLine($"Timestamp:        {doc.FileInfo.SourceTimestampUtc?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) ?? "<unknown>"}");
            Console.Error.WriteLine($"Scene tree nodes: {treeNodeCount}");
            Console.Error.WriteLine($"Property records: {doc.PropertyRecords.Count}");
            Console.Error.WriteLine($"GUIDs:            {doc.Guids.Count}");
            Console.Error.WriteLine($"3D Transforms:    {doc.Transforms.Count}");
            Console.Error.WriteLine($"3D Meshes:        {doc.Meshes.Count}");
            Console.Error.WriteLine($"Vertices:         {totalVertices:N0}");
            Console.Error.WriteLine($"Triangles:        {totalTriangles:N0}");
            Console.Error.WriteLine($"Materials:        {doc.Materials.Count}");
            if (doc.XRefs is not null)
            {
                Console.Error.WriteLine($"XRefs:            {doc.XRefs.References.Count}");
            }
            if (doc.DbCache is not null)
            {
                Console.Error.WriteLine($"DbCache Entries:  {doc.DbCache.Entries.Count}");
            }
            if (bvhStats && doc.Meshes.Count > 0)
            {
                var idx = doc.SpatialIndex;
                Console.Error.WriteLine($"BVH Nodes:        {idx.NodeCount} ({idx.LeafCount} leaves, max depth {idx.MaxDepth})");
                Console.Error.WriteLine($"Bounding box:     {idx.Bounds}");
            }
            Console.Error.WriteLine($"Elapsed:          {sw.ElapsedMilliseconds} ms");
            Console.Error.WriteLine($"Output:           {outputPath}");
        }
        else
        {
            Console.WriteLine(FormattableString.Invariant(
                $"Exported {doc.PropertyRecords.Count} records, {doc.Guids.Count} GUIDs, {doc.Transforms.Count} transforms, {doc.Meshes.Count} meshes, {doc.Materials.Count} materials to '{outputPath}' in {sw.ElapsedMilliseconds} ms."));
        }

        return 0;
    }

    private static int ProcessAssemblyDirectory(
        string dirPath,
        string? outputPath,
        string? explicitGlbPath,
        string? explicitObjPath,
        bool worldCoords,
        bool checkClashes,
        bool bvhStats,
        string? raycastArg,
        bool printReport,
        bool indent,
        Stopwatch sw)
    {
        outputPath ??= Path.Combine(dirPath, Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + "_assembly.glb");

        var assembly = NwcAssembly.LoadFromDirectory(dirPath);

        if (outputPath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
        {
            assembly.SaveGlb(outputPath, worldCoords);
        }
        else if (outputPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
        {
            assembly.SaveObj(outputPath, worldCoords);
        }
        else
        {
            assembly.SaveJson(outputPath, new NwcJsonExportOptions { Indented = indent });
        }

        if (!string.IsNullOrEmpty(explicitGlbPath)) assembly.SaveGlb(explicitGlbPath, worldCoords);
        if (!string.IsNullOrEmpty(explicitObjPath)) assembly.SaveObj(explicitObjPath, worldCoords);

        if (!string.IsNullOrEmpty(raycastArg))
        {
            ExecuteRaycastTest(assembly.SpatialIndex, raycastArg);
        }

        IReadOnlyList<NwcClash>? clashes = null;
        if (checkClashes)
        {
            clashes = assembly.FindClashes();
        }

        sw.Stop();

        if (printReport)
        {
            Console.Error.WriteLine("=== NwcIO Assembly Report ===");
            Console.Error.WriteLine($"Assembly:         {assembly.Name}");
            Console.Error.WriteLine($"Models:           {assembly.Models.Count}");
            Console.Error.WriteLine($"Total Meshes:     {assembly.AllMeshes.Count}");
            Console.Error.WriteLine($"Total Materials:  {assembly.AllMaterials.Count}");
            Console.Error.WriteLine($"Bounding Box:     {assembly.BoundingBox}");
            if (bvhStats)
            {
                var idx = assembly.SpatialIndex;
                Console.Error.WriteLine($"BVH Nodes:        {idx.NodeCount} ({idx.LeafCount} leaves, max depth {idx.MaxDepth})");
            }
            if (clashes is not null)
            {
                Console.Error.WriteLine($"Clashes Detected: {clashes.Count}");
            }
            Console.Error.WriteLine($"Elapsed:          {sw.ElapsedMilliseconds} ms");
            Console.Error.WriteLine($"Output:           {outputPath}");
        }
        else
        {
            Console.WriteLine(FormattableString.Invariant(
                $"Assembly '{assembly.Name}': {assembly.Models.Count} models, {assembly.AllMeshes.Count} meshes, {assembly.AllMaterials.Count} materials exported to '{outputPath}' in {sw.ElapsedMilliseconds} ms."));
        }

        return 0;
    }

    private static int ProcessNwfAssembly(
        string nwfPath,
        string? outputPath,
        string? explicitGlbPath,
        string? explicitObjPath,
        bool worldCoords,
        bool checkClashes,
        bool bvhStats,
        string? raycastArg,
        bool printReport,
        bool indent,
        Stopwatch sw)
    {
        outputPath ??= Path.ChangeExtension(nwfPath, ".glb");
        var assembly = NwcAssembly.OpenNwf(nwfPath);

        if (outputPath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
        {
            assembly.SaveGlb(outputPath, worldCoords);
        }
        else if (outputPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
        {
            assembly.SaveObj(outputPath, worldCoords);
        }
        else
        {
            assembly.SaveJson(outputPath, new NwcJsonExportOptions { Indented = indent });
        }

        if (!string.IsNullOrEmpty(explicitGlbPath)) assembly.SaveGlb(explicitGlbPath, worldCoords);
        if (!string.IsNullOrEmpty(explicitObjPath)) assembly.SaveObj(explicitObjPath, worldCoords);

        if (!string.IsNullOrEmpty(raycastArg))
        {
            ExecuteRaycastTest(assembly.SpatialIndex, raycastArg);
        }

        IReadOnlyList<NwcClash>? clashes = null;
        if (checkClashes)
        {
            clashes = assembly.FindClashes();
        }

        sw.Stop();

        if (printReport)
        {
            Console.Error.WriteLine("=== NwcIO NWF Assembly Report ===");
            Console.Error.WriteLine($"NWF File:         {nwfPath}");
            Console.Error.WriteLine($"Loaded Models:    {assembly.Models.Count}");
            Console.Error.WriteLine($"Total Meshes:     {assembly.AllMeshes.Count}");
            Console.Error.WriteLine($"Bounding Box:     {assembly.BoundingBox}");
            if (clashes is not null)
            {
                Console.Error.WriteLine($"Clashes Detected: {clashes.Count}");
            }
            Console.Error.WriteLine($"Elapsed:          {sw.ElapsedMilliseconds} ms");
            Console.Error.WriteLine($"Output:           {outputPath}");
        }
        else
        {
            Console.WriteLine(FormattableString.Invariant(
                $"NWF Assembly '{assembly.Name}': {assembly.Models.Count} models, {assembly.AllMeshes.Count} meshes exported to '{outputPath}' in {sw.ElapsedMilliseconds} ms."));
        }

        return 0;
    }

    private static void ExecuteRaycastTest(NwcSpatialIndex index, string coords)
    {
        var parts = coords.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6 ||
            !double.TryParse(parts[0], CultureInfo.InvariantCulture, out double ox) ||
            !double.TryParse(parts[1], CultureInfo.InvariantCulture, out double oy) ||
            !double.TryParse(parts[2], CultureInfo.InvariantCulture, out double oz) ||
            !double.TryParse(parts[3], CultureInfo.InvariantCulture, out double dx) ||
            !double.TryParse(parts[4], CultureInfo.InvariantCulture, out double dy) ||
            !double.TryParse(parts[5], CultureInfo.InvariantCulture, out double dz))
        {
            Console.Error.WriteLine($"Warning: Invalid raycast format '{coords}'. Expected: ox,oy,oz,dx,dy,dz");
            return;
        }

        var origin = new Vector3d(ox, oy, oz);
        var dir = new Vector3d(dx, dy, dz);

        if (index.Raycast(origin, dir, out var hit))
        {
            Console.WriteLine(FormattableString.Invariant(
                $"Raycast HIT at distance {hit.Distance:F3}: Point={hit.Point}, Normal={hit.Normal}, MeshId={hit.Mesh.Id}, Triangle={hit.TriangleIndex}"));
        }
        else
        {
            Console.WriteLine("Raycast MISS: no intersecting surface found along the ray.");
        }
    }

    private static void EnsureParentDirectory(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static int CountNodes(NwcNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        int count = 1;
        foreach (var child in node.Children)
        {
            count += CountNodes(child);
        }

        return count;
    }
}
