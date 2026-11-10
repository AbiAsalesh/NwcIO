using System.Buffers.Binary;
using System.Text;
using NwcIO.Internal.Parsing;
using Xunit;

namespace NwcIO.Tests;

public class PropertyRecordScannerTests
{
    [Fact]
    public void Scan_Tag52And53Records_DecodesCorrectly()
    {
        using var ms = new MemoryStream();

        // 1. Tag 53 node record
        WriteInt32(ms, 101);
        WriteInt32(ms, 53);
        WriteString(ms, "Layer_Ground");

        // 2. Tag 52 property record with string value
        WriteInt32(ms, 102);
        WriteInt32(ms, 52);
        WriteString(ms, "PropertyName");
        WriteString(ms, "LcRevitPropertyElementName");
        WriteInt32(ms, 4); // string type
        WriteString(ms, "Wall_Type_A");

        // 3. Tag 52 numeric property record
        WriteInt32(ms, 103);
        WriteInt32(ms, 52);
        WriteString(ms, "CategoryId");
        WriteString(ms, "LcRevitPropertyElementCategoryId");
        WriteInt32(ms, 2); // int32 type
        WriteInt32(ms, -2000011);

        var scanner = new PropertyRecordScanner(ms.ToArray());
        var records = scanner.Scan();

        Assert.Equal(3, records.Count);

        Assert.Equal(101, records[0].FirstInt);
        Assert.Equal(53, records[0].Tag);
        Assert.Equal("Layer_Ground", records[0].Name);
        Assert.Equal("Node", records[0].Class);

        Assert.Equal(102, records[1].FirstInt);
        Assert.Equal(52, records[1].Tag);
        Assert.Equal("PropertyName", records[1].Name);
        Assert.Equal("LcRevitPropertyElementName", records[1].Class);
        Assert.Equal(4, records[1].ValueType);
        Assert.Equal("Wall_Type_A", records[1].Value);

        Assert.Equal(103, records[2].FirstInt);
        Assert.Equal(52, records[2].Tag);
        Assert.Equal("CategoryId", records[2].Name);
        Assert.Equal(2, records[2].ValueType);
        Assert.Equal("-2000011", records[2].Value);
    }

    private static void WriteInt32(MemoryStream ms, int value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buf, value);
        ms.Write(buf);
    }

    private static void WriteString(MemoryStream ms, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        WriteInt32(ms, bytes.Length);
        ms.Write(bytes);
        int pad = (4 - (bytes.Length & 3)) & 3;
        for (int i = 0; i < pad; i++)
        {
            ms.WriteByte(0);
        }
    }
}
