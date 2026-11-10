# Changelog

All notable changes to the `NwcIO` library and CLI are documented in this file.

## [0.1.0] - 2026-11-10

### Initial Public Release

- **Pure Managed Navisworks Binary Engine**:
  - Full clean-room parser for Autodesk Navisworks binary streams (`.nwc`, `.nwd`, `.nwf`): `LcOStream`, `LcRawStream`, and compressed chunk hierarchies via internal Deflate decompression.
  - Complete hierarchical scene graph extraction (models, layers, partition nodes, composite instances, geometries).
  - Comprehensive parameter and property set extraction (`NwcPropertySet`, `NwcProperty`).
  - Target modern **.NET 8.0** and **.NET 10.0** with zero native dependencies or external libraries.
- **Advanced Multi-Format Exporters**:
  - **buildingSMART IFC (IFC2x3 & IFC4)**:
    - IFC2x3 via `IfcFacetedBrep` geometry representation.
    - IFC4 via compact modern tessellation: `IfcTriangulatedFaceSet` and `IfcCartesianPointList3D` under representation type `'Tessellation'` (>60% smaller file sizes, sub-second export).
    - IFC Type Products: automatic generation and association of `IfcTypeProduct` entities (`IfcWallType`, `IfcSlabType`, `IfcColumnType`, `IfcBeamType`, `IfcPipeSegmentType`, etc.) via `IfcRelDefinesByType`.
    - Semantic entity classification mapping Navisworks objects to standard IFC classes.
  - **glTF 2.0 Binary (GLB)**:
    - Interleaved binary buffers with PBR material definitions, per-element metadata, and embedded viewpoints / cameras.
  - **Wavefront OBJ**:
    - Direct export of textured geometry (`.obj` + `.mtl`).
  - **Structured JSON**:
    - Export of complete scene hierarchy, bounding boxes, property sets, clash test results, and camera definitions.
- **BCF 2.1 & Clash Reports**:
  - BuildingSMART compliant BIM Collaboration Format 2.1 zip export (`.bcfzip` / `.bcf`).
  - Interactive offline standalone single-file HTML clash matrix reports with responsive tables, severity badges, and 3D preview metadata.
- **Saved Viewpoints & 3D Cameras**:
  - Reverse-engineered Navisworks viewpoint stream format (stream tag 517).
  - Perspective and Orthographic camera models, focal distance, field of view, aspect ratio, camera look rotation math.
  - Automatic translation of viewpoints into glTF camera nodes, BCF viewpoints, and JSON.
- **Spatial Indexing & Clash Detection**:
  - Surface Area Heuristic (SAH) Bounding Volume Hierarchy (BVH) spatial index.
  - Fast AABB vs AABB, triangle-triangle, and raycasting acceleration for clash detection between federated models (`NwcAssembly`).
- **Cross-Platform CLI Tool (`nwcio`)**:
  - Available as a .NET Tool and executable CLI for `info`, `export`, `clash`, and `view` commands.
