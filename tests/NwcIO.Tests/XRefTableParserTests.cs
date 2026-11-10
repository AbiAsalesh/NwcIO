using System.Text;
using NwcIO.Internal.Parsing;
using NwcIO.Model;
using Xunit;

namespace NwcIO.Tests;

public class XRefTableParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsXRefTable()
    {
        const string json = """
            {"Type":"XRefTable","Version":1,"SavedFilename":"S:\\Projects\\Building.nwc","Table":[{"OriginalPath":"C:/Textures/concrete.png","RemappedPath":"D:/Textures/concrete.png"}]}
            """;

        // Prepend 4 bytes of arbitrary header prefix as observed in container streams
        byte[] raw = new byte[4 + Encoding.UTF8.GetByteCount(json)];
        Encoding.UTF8.GetBytes(json, 0, json.Length, raw, 4);

        var table = XRefTableParser.Parse(raw);

        Assert.NotNull(table);
        Assert.Equal("XRefTable", table.Type);
        Assert.Equal(1, table.Version);
        Assert.Equal(@"S:\Projects\Building.nwc", table.SavedFilename);
        Assert.Single(table.References);

        var xref = table.References[0];
        Assert.Equal("C:/Textures/concrete.png", xref.OriginalPath);
        Assert.Equal("D:/Textures/concrete.png", xref.RemappedPath);
        Assert.Equal("D:/Textures/concrete.png", xref.EffectivePath);
    }

    [Fact]
    public void Parse_EmptyTable_ReturnsEmptyList()
    {
        const string json = """{"Type":"XRefTable","Version":1,"SavedFilename":"file.nwc","Table":[]}""";
        byte[] data = Encoding.UTF8.GetBytes(json);

        var table = XRefTableParser.Parse(data);

        Assert.NotNull(table);
        Assert.Empty(table.References);
    }

    [Fact]
    public void Parse_InvalidData_ReturnsNull()
    {
        byte[] garbage = [1, 2, 3, 4, 5];
        var table = XRefTableParser.Parse(garbage);
        Assert.Null(table);
    }
}
