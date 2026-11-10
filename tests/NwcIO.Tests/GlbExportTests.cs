using System.Buffers.Binary;
using System.Text.Json;
using NwcIO.Internal.Export;
using NwcIO.Model;
using Xunit;

namespace NwcIO.Tests;

public class GlbExportTests
{
    [Fact]
    public void GlbExporter_SingleMesh_ProducesValidGlbContainer()
    {
        var vertices = new[]
        {
            new Vector3d(0, 0, 0),
            new Vector3d(10, 0, 0),
            new Vector3d(0, 10, 0),
        };

        var material = new NwcMaterial
        {
            Id = 1,
            Name = "TestMaterial",
            Color = new RgbaColor(1.0f, 0.5f, 0.0f, 1.0f),
            Metallic = 0.2f,
            Roughness = 0.8f,
        };

        var mesh = new NwcMesh
        {
            Id = 0,
            Name = "Triangle",
            Vertices = vertices,
            Indices = [0, 1, 2],
            Material = material,
        };

        using var ms = new MemoryStream();
        GlbExporter.Write([mesh], [material], ms, worldCoordinates: false);
        byte[] glb = ms.ToArray();

        // 1. Verify 12-byte header
        Assert.True(glb.Length >= 12);
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(0));
        uint version = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(4));
        uint totalLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(8));

        Assert.Equal(0x46546C67u, magic); // "glTF"
        Assert.Equal(2u, version);
        Assert.Equal((uint)glb.Length, totalLength);

        // 2. Verify Chunk 0 (JSON)
        uint jsonChunkLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12));
        uint jsonChunkType = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(16));

        Assert.Equal(0x4E4F534Au, jsonChunkType); // "JSON"
        Assert.True(jsonChunkLength % 4 == 0, "JSON chunk must be 4-byte aligned");

        byte[] jsonBytes = glb.AsSpan(20, (int)jsonChunkLength).ToArray();
        string json = System.Text.Encoding.UTF8.GetString(jsonBytes).Trim();

        using var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        // Verify glTF structure
        Assert.True(root.TryGetProperty("asset", out var assetEl));
        Assert.Equal("2.0", assetEl.GetProperty("version").GetString());
        Assert.True(root.TryGetProperty("scene", out var sceneEl));
        Assert.Equal(0, sceneEl.GetInt32());
        Assert.True(root.TryGetProperty("meshes", out var meshesEl));
        Assert.Equal(1, meshesEl.GetArrayLength());
        Assert.True(root.TryGetProperty("materials", out var materialsEl));
        Assert.Equal(1, materialsEl.GetArrayLength());
        Assert.True(root.TryGetProperty("accessors", out var accessorsEl));
        Assert.True(root.TryGetProperty("bufferViews", out var bufferViewsEl));
        Assert.True(root.TryGetProperty("buffers", out var buffersEl));

        // 3. Verify Chunk 1 (BIN)
        int binChunkOffset = 20 + (int)jsonChunkLength;
        Assert.True(glb.Length >= binChunkOffset + 8);
        uint binChunkLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(binChunkOffset));
        uint binChunkType = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(binChunkOffset + 4));

        Assert.Equal(0x004E4942u, binChunkType); // "BIN\0"
        Assert.True(binChunkLength % 4 == 0, "BIN chunk must be 4-byte aligned");
        Assert.Equal((uint)(glb.Length - (binChunkOffset + 8)), binChunkLength);
    }
}
