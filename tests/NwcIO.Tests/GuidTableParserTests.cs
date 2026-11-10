using System.Buffers.Binary;
using NwcIO.Internal.Container;
using NwcIO.Internal.Parsing;
using Xunit;

namespace NwcIO.Tests;

public class GuidTableParserTests
{
    [Fact]
    public void TryParse_ValidGuidTable_ReturnsGuids()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        var data = new byte[8 + 32];
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0), 1);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), 2);
        guid1.TryWriteBytes(data.AsSpan(8, 16));
        guid2.TryWriteBytes(data.AsSpan(24, 16));

        var stream = new LcRawStream
        {
            Index = 0,
            FileOffset = 0,
            CompressedLength = 10,
            GapBefore = 0,
            Data = data,
            SectionIndex = 0,
        };

        bool ok = GuidTableParser.TryParse(stream, out var guids);

        Assert.True(ok);
        Assert.Equal(2, guids.Count);
        Assert.Equal(guid1, guids[0]);
        Assert.Equal(guid2, guids[1]);
    }

    [Fact]
    public void TryParse_MalformedLength_ReturnsFalse()
    {
        var data = new byte[15]; // not 8 + 16*N
        var stream = new LcRawStream
        {
            Index = 0,
            FileOffset = 0,
            CompressedLength = 10,
            GapBefore = 0,
            Data = data,
            SectionIndex = 0,
        };

        bool ok = GuidTableParser.TryParse(stream, out var guids);

        Assert.False(ok);
        Assert.Empty(guids);
    }
}
