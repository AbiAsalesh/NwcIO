using System.Buffers.Binary;
using System.Text;
using NwcIO.Internal.Container;
using NwcIO.Model.TimeLiner;

namespace NwcIO.Internal.Parsing;

internal static class TimeLinerParser
{
    public static NwcTimeLinerSchedule? Parse(LcRawStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var data = stream.Data;
        if (data.Length < 8) return null;

        // Binary header [100][106][count][length][csv bytes]
        if (BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0)) == 100 &&
            BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4)) == 106 &&
            data.Length >= 16)
        {
            int csvLen = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(12));
            if (csvLen > 0 && 16 + csvLen <= data.Length)
            {
                using var ms = new MemoryStream(data, 16, csvLen, writable: false);
                return NwcTimeLinerCsvSerializer.Read(ms);
            }
        }

        // Direct CSV payload
        try
        {
            using var ms = new MemoryStream(data, writable: false);
            return NwcTimeLinerCsvSerializer.Read(ms);
        }
        catch
        {
            return null;
        }
    }
}
