using System.Text.Json;
using Xunit;

namespace NwcIO.Tests;

public class RealFixtureTests
{
    private static readonly string? FixturePath = ResolveFixturePath();

    private static string? ResolveFixturePath()
    {
        var env = Environment.GetEnvironmentVariable("NWCIO_TEST_FILES");
        if (!string.IsNullOrEmpty(env) && File.Exists(env))
        {
            return env;
        }

        const string defaultPath = @"sample_building.nwc";
        return File.Exists(defaultPath) ? defaultPath : null;
    }

    [Fact]
    public void Open_RealNwcFixture_ParsesHeaderAndRecords()
    {
        if (FixturePath is null)
        {
            return; // Skip on machines without the sample files
        }

        var doc = NwcDocument.Open(FixturePath);

        Assert.NotNull(doc.FileInfo);
        Assert.NotNull(doc.FileInfo.SourcePath);
        Assert.Contains(".rvt", doc.FileInfo.SourcePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("LcNwcLoaderPlugin:lcldrevit", doc.FileInfo.LoaderPlugin);
        Assert.NotNull(doc.FileInfo.SourceTimestampUtc);

        Assert.NotEmpty(doc.Guids);
        Assert.True(doc.Guids.Count > 1000);

        Assert.NotEmpty(doc.PropertyRecords);
        Assert.True(doc.PropertyRecords.Count > 500);

        Assert.NotEmpty(doc.Transforms);
        Assert.True(doc.Transforms.Count > 1000);
        var firstTransform = doc.Transforms[0];
        Assert.True(firstTransform.Translation.X > 0);
        Assert.True(firstTransform.Rotation.IsNormalized());
        Assert.Equal(16, firstTransform.ToMatrix4x4().Length);

        Assert.NotNull(doc.RootNode);
        Assert.NotEmpty(doc.RootNode.Children);

        Assert.NotEmpty(doc.Meshes);
        Assert.True(doc.Meshes.Count > 500);
        var firstMesh = doc.Meshes[0];
        Assert.True(firstMesh.Vertices.Count >= 3);
        Assert.True(firstMesh.TriangleCount >= 1);
        Assert.NotNull(firstMesh.BoundingBox);

        // Verify OBJ export
        using var sw = new StringWriter();
        doc.SaveObj(sw, worldCoordinates: true);
        string obj = sw.ToString();
        Assert.NotEmpty(obj);
        Assert.Contains("o Mesh_0", obj);
        Assert.Contains("v ", obj);
        Assert.Contains("f ", obj);

        // Verify JSON export
        string json = doc.ToJson(new NwcJsonExportOptions
        {
            Indented = false,
            IncludeTransforms = true,
            IncludeSceneTree = true,
            IncludeMeshes = true,
            IncludeStreamCatalog = true,
        });
        Assert.NotEmpty(json);

        using var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        Assert.True(root.TryGetProperty("file", out var fileEl));
        Assert.True(fileEl.TryGetProperty("sourcePath", out _));
        Assert.True(root.TryGetProperty("propertyRecordCount", out var countEl));
        Assert.Equal(doc.PropertyRecords.Count, countEl.GetInt32());
        Assert.True(root.TryGetProperty("guids", out var guidsEl));
        Assert.Equal(doc.Guids.Count, guidsEl.GetArrayLength());
        Assert.True(root.TryGetProperty("transforms", out var transformsEl));
        Assert.Equal(doc.Transforms.Count, transformsEl.GetArrayLength());
        Assert.True(root.TryGetProperty("meshCount", out var meshCountEl));
        Assert.Equal(doc.Meshes.Count, meshCountEl.GetInt32());
        Assert.True(root.TryGetProperty("meshes", out var meshesEl));
        Assert.Equal(doc.Meshes.Count, meshesEl.GetArrayLength());
        Assert.True(root.TryGetProperty("sceneTree", out var sceneTreeEl));
        Assert.True(sceneTreeEl.TryGetProperty("children", out _));
        Assert.True(root.TryGetProperty("container", out var containerEl));
        Assert.True(containerEl.TryGetProperty("streams", out _));
    }
}
