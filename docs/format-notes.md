# Navisworks File Format Notes (.nwc / .nwd)

This document records reverse-engineering findings obtained from hex and stream analysis of real Autodesk Navisworks cache files (`.nwc`, version 448 / Navisworks 2021–2024 era) and clean-room study of format specifications.

## 1. High-Level Container: `#LcUStream`

Navisworks cache (`.nwc`) and document (`.nwd`) files are containerized streams introduced by ASCII header lines.

### 1.1 Section Header Syntax

Each section begins at an offset with an ASCII text line terminated by `\n` (`0x0A`):

```text
#LcUStream-V1.1 binary double release C lichunk-008
#LcUStream-V1.1 binary double release utf8 navisworks:448 lizlib
```

Key tokens:
- **Format identifier**: `#LcUStream-V1.1`
- **Double precision**: `double` (floating-point numbers in geometry and matrices are 64-bit doubles)
- **Character encoding**: `C` (8-bit locale-dependent ASCII) or `utf8`
- **Internal version**: `navisworks:448` (format version identifier, e.g. 448 in Navisworks 2021–2024)
- **Chunk marker**: `lichunk-008` (chunked container layout) or `lizlib` (raw zlib stream block)

A single `.nwc` file often contains multiple `#LcUStream` sections (e.g. partition 0 for model data, partition 1 for xrefs/caches).

### 1.2 Stream Structure

Following each section header is a series of back-to-back zlib compressed streams (RFC 1950, magic `0x78 0x01`, `0x78 0x5E`, `0x78 0x9C`, or `0x78 0xDA`).

Between some streams are framing blocks:
- 40-byte framing/hash blocks: a 20-byte or 40-byte checksum / hash before major partition boundaries.
- Trailing gap: optional footer bytes (e.g. SQLite DB cache metadata).

## 2. Decompressed Stream Classification

Once inflated, streams serve dedicated roles identified by their signature prefixes.

### 2.1 Partition Header (`PartitionHeader`)
- **Signature**: `[int64 7]` followed by length-prefixed strings.
- **Fields**:
  - `int64`: marker `7`
  - `int32 len` + UTF-8 string: full path of originating source document (e.g., `S:\...\Building_Structural.rvt`)
  - `int32 -1`
  - `int64`: modification timestamp as Unix epoch seconds
  - `int64`: source file size in bytes
  - `int32 len` + UTF-8 string: loader plugin ID (e.g., `LcNwcLoaderPlugin:lcldrevit`)
  - Subsequent length-prefixed UTF-8 strings: loader export configuration options.

### 2.2 Scene Tree & Property Stream (`SceneTree`)
- **Signatures**: starts with `[int32 100][int32 86]` or contains `LcRevit` hierarchy records.
- **Record Types**:
  - **Hierarchy Node (Tag 53 / `0x35`)**:
    - Layout: `[int32 id][int32 53][int32 nameLen][name string][padding to 4B]`
    - Represents scene grouping: levels (`<Нет уровня>`), categories (`Несущая арматура`), families (`Арматурный стержень`), types (`мп_А500 Ø10`).
  - **Property / Element Record (Tag 52 / `0x34`)**:
    - Layout:
      ```
      [int32 id]
      [int32 52]
      [int32 nameLen][UTF-8 name][4B padding]
      [int32 classLen][UTF-8 class][4B padding]
      [int32 valueType]
      [type-specific value payload]
      ```
    - **Value Types observed**:
      - `0`: Structural element / child count (e.g., `35` properties on this element)
      - `1, 2, 3`: 32-bit integers / booleans / Revit BuiltInCategory IDs (e.g., `-2009000` for `OST_Rebar`, Element IDs like `4917666`)
      - `4`: Length-prefixed UTF-8 string (padded to 4 bytes), e.g. `"мп_А500 Ø10 : Форма О_1"`
      - `5, 7`: 64-bit IEEE 754 double (coordinates, lengths, angles)
      - `8`: Embedded element reference `[int32 refId][int32 52][refName][refClass]`, e.g. `RebarShape "О_1", #4930621` of class `LcRevitElement`

### 2.3 GUID Table (`Guids`)
- **Signature**: `[int32 ?][int32 count]` where `(streamLength - 8) == count * 16`.
- **Payload**: sequential array of 16-byte raw little-endian GUIDs corresponding to element unique IDs.

### 2.4 Transforms (`Transforms`)
- **Signature**: `[int32 n][int32 100][int32 13]`
- **Payload**: 4x4 affine transformation matrices (`double[16]`) for model items and instances.

### 2.5 Table of Contents (`Toc`)
- **Signature**: `[int32 x][int32 count][count * int32 compressedLengths]`
- Catalogs chunk sizes and offsets within the file.

### 2.6 DB Cache (`DbCache`) & XRef JSON (`XRefJson`)
- **DbCache**: Header `LcOpDBCache`, contains embedded metadata and product cache.
- **XRef JSON**: UTF-8 JSON snippet starting with `{"Type":"XRefTable", ...}` storing external cross-references.
