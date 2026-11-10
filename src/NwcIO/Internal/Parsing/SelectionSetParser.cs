using System.Buffers.Binary;
using System.Text;
using NwcIO.Internal.Container;
using NwcIO.Model.Sets;

namespace NwcIO.Internal.Parsing;

internal static class SelectionSetParser
{
    public static IReadOnlyList<NwcSetItem> Parse(LcRawStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var data = stream.Data;
        if (data.Length < 8) return Array.Empty<NwcSetItem>();

        // Binary header [100][105][count][length][xml bytes]
        if (BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0)) == 100 &&
            BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4)) == 105 &&
            data.Length >= 16)
        {
            int xmlLen = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(12));
            if (xmlLen > 0 && 16 + xmlLen <= data.Length)
            {
                using var ms = new MemoryStream(data, 16, xmlLen, writable: false);
                return NwcXmlSetSerializer.Read(ms);
            }
        }

        // Direct XML payload
        try
        {
            using var ms = new MemoryStream(data, writable: false);
            return NwcXmlSetSerializer.Read(ms);
        }
        catch
        {
            return Array.Empty<NwcSetItem>();
        }
    }
}
