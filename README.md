<p align="center">
  <img src="docs/images/nwcio-banner.png" alt="NwcIO" width="640">
</p>

# NwcIO

**NwcIO** is a fast, 100% managed .NET library and CLI tool that reads Autodesk Navisworks files (`.nwc`, `.nwd`, `.nwf`) directly from disk **without requiring Autodesk Navisworks or the Navisworks API to be installed**, and exports 3D geometry, materials, scene hierarchies, and properties to **Industry Foundation Classes (IFC2x3 Coordination View)**, **glTF 2.0 Binary (GLB)**, **Wavefront OBJ**, and **JSON**.

Built clean-room following the architectural principles of [mahach666/RvtIO](https://github.com/mahach666/RvtIO): zero native dependencies, zero external libraries, strict typing, high performance, and cross-platform compatibility.

---

## What It Does

- **Container Decompression**: Direct byte-stream parsing of `#LcUStream-V1.1` containers, continuous zlib decompression (`lichunk-008`, `lizlib`), and checksum gap framing without third-party libraries.
- **Industry Foundation Classes (IFC2x3)**: Pure managed exporter generating ISO 10303-21 STEP physical files compliant with IFC2x3 Coordination View V2.0 (`IfcFacetedBrep`, `IfcClosedShell`, `IfcFace`, `IfcPolyLoop`, `IfcCartesianPoint`), spatial hierarchy (`IfcProject`, `IfcSite`, `IfcBuilding`, `IfcBuildingStorey`), element mapping (`IfcWall`, `IfcSlab`, `IfcColumn`, `IfcBeam`, `IfcReinforcingBar`, etc.), surface styles (`IfcSurfaceStyle`, `IfcColourRgb`), property sets (`IfcPropertySet`, `IfcPropertySingleValue`), and buildingSMART 22-character GUID encoding.
- **Hierarchical Scene Graph**: Reconstructs Navisworks selection tree (Model &rarr; Layers &rarr; Categories &rarr; Families &rarr; Types &rarr; Composite Elements &rarr; Geometry) with bound parameters and GUIDs.
- **3D Spatial Transforms**: Decodes instance poses (double-precision translation vectors, unit rotation quaternions, local AABB bounding boxes) and generates analytic $4\times 4$ affine transformation matrices.
- **3D Polygon Meshes**: Reads geometry chunk streams via TOC (Table of Contents), unpacks IEEE 754 vertices, converts `TRIANGLE_STRIP` buffers with alternating normal windings, and transforms vertices to world coordinates.
- **Materials & RGBA Colors**: Extracts surface materials and color triples (`LcRevitElement`, `revit_Color`), assigns standard BIM category palettes (concrete, rebar, insulation), and computes vertex/face normal vectors for PBR rendering.
- **Binary glTF 2.0 (GLB) Exporter**: Generates specification-compliant `.glb` files with 12-byte header, 4-byte padded JSON chunk (`0x20`), 4-byte padded binary chunk (`0x00`), vertex positions, normals, indices, and PBR materials.
- **Wavefront OBJ Exporter**: Streams 3D meshes to `.obj` with 1-based indexing in local or project world coordinates.
- **Spatial Acceleration Index (BVH)**: Balanced Bounding Volume Hierarchy over all meshes for sub-millisecond range queries (`QueryBox`) and double-precision Möller–Trumbore raycasting (`Raycast`).
- **Federated Multi-Model Assemblies (.nwf)**: Aggregates multiple discipline models into a unified coordination coordinate system, reads `.nwf` external references, and detects spatial clashes (`FindClashes`).
- **Saved Viewpoints & Cameras**: Extracts user saved viewpoints and camera views (`NwcCamera`, `NwcSavedViewpoint`) from stream tag 517 (double-precision eye coordinates, unit rotation quaternions, viewing direction, up vector, focal distance, FOV, and aspect ratio). Automatically embeds viewpoints as standard `cameras` and `nodes` in glTF 2.0 (GLB), outputs them in JSON, and exports to buildingSMART BCF 2.1 zip archives (`SaveViewpointsBcf`).
- **buildingSMART BCF 2.1 Collaboration**: Pure managed exporter generating standard BIM Collaboration Format (`.bcfzip`) archives with topic folders, `markup.bcf`, viewpoints (`viewpoint.bcfv`), perspective cameras focused on clash centers, and colored component highlights (`IfcGuid`).
- **Interactive HTML Clash Reports**: Standalone, offline HTML clash detection report with a discipline cross-clash matrix ($M_i \times M_j$ frequency), KPI summary cards, live search/filtering, and sortable clash details table.
- **Embedded DB Cache**: Decodes the internal `LcOpDBCache` virtual table directory (offsets, sizes, flags).

---

## Install

Install the command line tool:

```sh
dotnet tool install --global NwcIO.Cli
```

Add the library to a .NET project:

```sh
dotnet add package NwcIO
```

The library targets **.NET 8** and **.NET 10**. It is 100% managed code with zero native dependencies, running seamlessly on Windows, Linux, and macOS.

---

## Command Line (`nwcio`)

### Export to Industry Foundation Classes (IFC2x3)

```sh
# Export directly to IFC (Coordination View V2.0, Faceted BRep, Materials, Property Sets)
nwcio model.nwc model.ifc

# Export with summary report to stderr
nwcio model.nwc model.ifc --report
```

### Export to Binary glTF 2.0 (GLB)

```sh
# Export directly to GLB (with meshes, normals, materials, and colors)
nwcio model.nwc model.glb

# Export with summary report to stderr
nwcio model.nwc model.glb --report
```

### Export to Wavefront OBJ

```sh
# Export 3D meshes to OBJ in world coordinate space
nwcio model.nwc model.obj

# Export in local coordinate space
nwcio model.nwc model.obj --local-coords
```

### Export to Structured JSON

```sh
# Export full scene tree, properties, transforms, GUIDs, materials, and meshes
nwcio model.nwc model.json

# Compact (non-indented) JSON
nwcio model.nwc model.json --no-indent

# Include raw container stream catalog (offsets, lengths, stream kinds)
nwcio model.nwc model.json --catalog
```

### Multi-Model Federated Assemblies & Clash Detection

```sh
# Load all .nwc models from a folder into a single federated assembly GLB
nwcio ./BuildingK4/ ./BuildingK4_assembly.glb --report

# Run cross-model geometric clash detection across contributing models
nwcio ./BuildingK4/ ./BuildingK4_assembly.glb --clashes --report

# Export detected clashes directly to buildingSMART BCF 2.1 zip archive
nwcio ./BuildingK4/ ./clashes.bcfzip

# Export interactive, self-contained HTML clash report with matrix
nwcio ./BuildingK4/ ./clash_report.html

# List all saved viewpoints and cameras to stdout
nwcio model.nwc --viewpoints

# Export saved viewpoints to buildingSMART BCF 2.1 zip archive
nwcio model.nwc --viewpoints-bcf ./viewpoints.bcfzip

# Simultaneous multi-format export
nwcio ./BuildingK4/ ./assembly.glb --bcf ./clashes.bcfzip --clash-html ./report.html --report

# Inspect Bounding Volume Hierarchy (BVH) spatial index statistics
nwcio model.nwc model.glb --bvh-stats --report

# Test 3D raycast against scene geometry (originX, originY, originZ, dirX, dirY, dirZ)
nwcio model.nwc --raycast "0,0,100,0,0,-1"
```

### Exit Codes
- `0` — Success
- `1` — Usage error (missing or invalid arguments)
- `2` — File or directory not found
- `3` — Data format error or parsing failure

---

## Library Usage (C#)

### Reading a Navisworks Document

```csharp
using NwcIO;

// Open document (parses container, properties, GUIDs, transforms, meshes, materials)
var doc = NwcDocument.Open("model.nwc");

Console.WriteLine($"Source RVT:   {doc.FileInfo.SourcePath}");
Console.WriteLine($"Plugin:       {doc.FileInfo.LoaderPlugin}");
Console.WriteLine($"Meshes:       {doc.Meshes.Count}");
Console.WriteLine($"Materials:    {doc.Materials.Count}");
Console.WriteLine($"GUIDs:        {doc.Guids.Count}");
Console.WriteLine($"Transforms:   {doc.Transforms.Count}");

// Export directly to IFC, GLB, OBJ, or JSON
doc.SaveIfc("model.ifc");
doc.SaveGlb("model.glb", worldCoordinates: true);
doc.SaveObj("model.obj", worldCoordinates: true);
doc.SaveJson("model.json");
```

### 3D Raycasting with BVH Spatial Index

```csharp
// Spatial index is built lazily on first access and cached
var spatial = doc.SpatialIndex;

// Fast range query for meshes inside a bounding box
var hits = spatial.QueryBox(new BoundingBox3d(new(0, 0, 0), new(10, 10, 5)));

// Raycast against all polygon triangles in the scene
var origin = new Vector3d(5, 5, 20);
var direction = new Vector3d(0, 0, -1);

if (spatial.Raycast(origin, direction, out var hit))
{
    Console.WriteLine($"Hit mesh #{hit.Mesh.Id} at distance {hit.Distance:F2} m");
    Console.WriteLine($"Intersection point: {hit.Point}");
    Console.WriteLine($"Surface normal:     {hit.Normal}");
}
```

### Multi-Model Federated Coordination & Clash Detection

```csharp
using NwcIO.Model;

// Load an entire folder of models into a federated assembly
var assembly = NwcAssembly.LoadFromDirectory(@"C:\Projects\Campus", searchPattern: "*.nwc");

Console.WriteLine($"Models:   {assembly.Models.Count}");
Console.WriteLine($"Meshes:   {assembly.AllMeshes.Count}");
Console.WriteLine($"Bounds:   {assembly.BoundingBox}");

// Detect geometric clashes between elements from different models
var clashes = assembly.FindClashes(tolerance: 0.001);
foreach (var clash in clashes)
{
    Console.WriteLine($"Clash between {clash.ModelA.FileInfo.SourcePath} and {clash.ModelB.FileInfo.SourcePath}");
    Console.WriteLine($"Overlap volume: {clash.IntersectionBox}");
}

// Export the aggregated multi-discipline model to a single GLB or IFC file
assembly.SaveIfc("Campus_Coordination.ifc");
assembly.SaveGlb("Campus_Coordination.glb");

// Export clashes to standard buildingSMART BCF 2.1 archive (.bcfzip)
assembly.SaveBcf("Campus_Clashes.bcfzip", clashes);

// Export interactive, standalone HTML clash report with discipline matrix
assembly.SaveClashReportHtml("Campus_ClashReport.html", clashes);
```

### Saved Viewpoints & Cameras

```csharp
// Inspect saved viewpoints and camera parameters
foreach (var vp in doc.SavedViewpoints)
{
    var cam = vp.Camera;
    Console.WriteLine($"Viewpoint: {vp.Name} ({cam.Projection})");
    Console.WriteLine($"  Eye:        {cam.Position}");
    Console.WriteLine($"  Direction:  {cam.Direction}");
    Console.WriteLine($"  Target:     {cam.Target}");
    Console.WriteLine($"  Focal dist: {cam.FocalDistance:F2} m, FOV: {cam.FieldOfView:F1}°");
}

// Export saved viewpoints directly to buildingSMART BCF 2.1 zip archive
doc.SaveViewpointsBcf("viewpoints.bcfzip");
assembly.SaveViewpointsBcf("assembly_viewpoints.bcfzip");
```

---

## Performance

Tested on real industrial BIM projects (8 buildings, sample BIM project):

| Metric | Single Model (~250 KB) | Federated Assembly (~3 MB) | Multi-Building Campus (~10 MB) |
|---|---|---|---|
| **File size** | 250 KB `.nwc` | 2.8 MB (3 files) | ~10 MB (8 buildings) |
| **Meshes** | 573 | 2 861 | 20 000+ |
| **Vertices** | 19 930 | 95 000+ | 550 000+ |
| **Triangles** | 18 784 | 88 000+ | 520 000+ |
| **Parsing time** | **98 ms** | **572 ms** | **< 1.5 s** |
| **Export output** | 971 KB `.glb` | 5.01 MB `.glb` | ~30 MB `.glb` |
| **BVH Build** | < 2 ms | < 6 ms | < 25 ms |

---

## License

MIT &copy; 2026 [mahach666](https://github.com/mahach666)
