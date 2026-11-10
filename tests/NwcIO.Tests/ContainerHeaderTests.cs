using NwcIO.Internal.Container;
using Xunit;

namespace NwcIO.Tests;

public class ContainerHeaderTests
{
    [Fact]
    public void Parse_Lichunk008Header_ParsesTokensCorrectly()
    {
        var raw = System.Text.Encoding.ASCII.GetBytes("#LcUStream-V1.1 binary double release C lichunk-008\nremaining");
        var header = LcUStreamHeader.Parse(raw, 0);

        Assert.Equal(0, header.FileOffset);
        Assert.Equal(52, header.HeaderLength);
        Assert.Equal("LcUStream-V1.1", header.Format);
        Assert.Equal("C", header.Encoding);
        Assert.Equal("lichunk-008", header.Marker);
        Assert.Null(header.InternalVersion);
        Assert.Equal(5, header.Tokens.Count);
    }

    [Fact]
    public void Parse_NavisworksVersionHeader_ParsesVersionCorrectly()
    {
        var raw = System.Text.Encoding.ASCII.GetBytes("#LcUStream-V1.1 binary double release utf8 navisworks:448 lizlib\n");
        var header = LcUStreamHeader.Parse(raw, 0);

        Assert.Equal(0, header.FileOffset);
        Assert.Equal("utf8", header.Encoding);
        Assert.Equal("lizlib", header.Marker);
        Assert.Equal(448, header.InternalVersion);
    }

    [Fact]
    public void Parse_HeaderNotTerminated_ThrowsInvalidDataException()
    {
        var raw = System.Text.Encoding.ASCII.GetBytes("#LcUStream-V1.1 without newline");
        Assert.Throws<InvalidDataException>(() => LcUStreamHeader.Parse(raw, 0));
    }

    [Fact]
    public void Parse_InvalidMagic_ThrowsInvalidDataException()
    {
        var raw = System.Text.Encoding.ASCII.GetBytes("#NotLcUStream line\n");
        Assert.Throws<InvalidDataException>(() => LcUStreamHeader.Parse(raw, 0));
    }
}
