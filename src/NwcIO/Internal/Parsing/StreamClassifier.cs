using System.Buffers.Binary;
using NwcIO.Internal.Container;

namespace NwcIO.Internal.Parsing;

/// <summary>
/// Assigns a <see cref="NwcStreamKind"/> to a decompressed stream based on its leading bytes.
/// Classification is best-effort: unrecognized streams stay <see cref="NwcStreamKind.Unknown"/>
/// rather than being guessed into a wrong category.
/// </summary>
public static class StreamClassifier
{
    public static void ClassifyAll(IReadOnlyList<LcRawStream> streams)
    {
        foreach (var stream in streams)
        {
            stream.Kind = Classify(stream);
        }
    }

    public static NwcStreamKind Classify(LcRawStream stream)
    {
        var data = stream.Data;
        if (data.Length < 8)
        {
            return NwcStreamKind.Unknown;
        }

        // Partition header: starts with int64 7, then a length-prefixed source path.
        if (BinaryPrimitives.ReadInt64LittleEndian(data) == 7 && LooksLikeLengthPrefixedText(data, 8))
        {
            return NwcStreamKind.PartitionHeader;
        }

        // Scene tree / property records: starts with [100][86] or contains LcRevit signatures.
        if ((ReadInt32(data, 0) == 100 && ReadInt32(data, 4) == 86) ||
            ContainsAscii(data, 0, Math.Min(data.Length, 256), "LcRevit"))
        {
            return NwcStreamKind.SceneTree;
        }

        // Transforms: [n][100][13].
        if (data.Length >= 12 && ReadInt32(data, 4) == 100 && ReadInt32(data, 8) == 13)
        {
            return NwcStreamKind.Transforms;
        }

        // GUID table: [a][count] followed by exactly count 16-byte GUIDs.
        if (data.Length >= 8 && (data.Length - 8) % 16 == 0)
        {
            int count = ReadInt32(data, 4);
            if (count > 0 && count == (data.Length - 8) / 16)
            {
                return NwcStreamKind.Guids;
            }
        }

        // XRefTable JSON: a length-prefixed JSON document.
        if (ContainsAscii(data, 0, Math.Min(data.Length, 128), "{\"Type\":"))
        {
            return NwcStreamKind.XRefJson;
        }

        if (ContainsAscii(data, 0, Math.Min(data.Length, 64), "LcOpDBCache"))
        {
            return NwcStreamKind.DbCache;
        }

        // TOC: [int32 x][int32 count][count x int32 compressed sizes...].
        if (data.Length >= 16)
        {
            int count = ReadInt32(data, 4);
            if (count is > 0 and <= 1_000_000 && data.Length >= 8 + 4L * count && data.Length < 8 + 4L * count * 8)
            {
                bool plausible = true;
                for (int i = 0; i < count; i++)
                {
                    int size = ReadInt32(data, 8 + 4 * i);
                    if (size is <= 0 or > 512 * 1024 * 1024)
                    {
                        plausible = false;
                        break;
                    }
                }

                if (plausible)
                {
                    return NwcStreamKind.Toc;
                }
            }
        }

        return NwcStreamKind.Unknown;
    }

    private static int ReadInt32(byte[] data, int offset) =>
        offset + 4 <= data.Length ? BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset)) : 0;

    private static bool LooksLikeLengthPrefixedText(byte[] data, int offset)
    {
        if (offset + 4 > data.Length)
        {
            return false;
        }

        int len = ReadInt32(data, offset);
        if (len is <= 0 or > 8192 || offset + 4 + len > data.Length)
        {
            return false;
        }

        // Path-ish text: UTF-8, mostly printable.
        for (int i = 0; i < Math.Min(len, 64); i++)
        {
            byte b = data[offset + 4 + i];
            if (b < 0x20 && b != 0x0A)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsAscii(byte[] data, int start, int end, string needle)
    {
        if (end + needle.Length > data.Length)
        {
            end = data.Length - needle.Length;
        }

        for (int i = start; i <= end; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (data[i + j] != (byte)needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }
}
