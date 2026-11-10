using System.Globalization;

namespace NwcIO.Model;

/// <summary>
/// An RGBA color with normalized floating-point components [0.0 .. 1.0].
/// </summary>
public readonly record struct RgbaColor(float R, float G, float B, float A = 1.0f)
{
    public static RgbaColor White { get; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public static RgbaColor Black { get; } = new(0.0f, 0.0f, 0.0f, 1.0f);
    public static RgbaColor Gray { get; } = new(0.5f, 0.5f, 0.5f, 1.0f);
    public static RgbaColor Concrete { get; } = new(0.72f, 0.72f, 0.72f, 1.0f);
    public static RgbaColor Steel { get; } = new(0.60f, 0.65f, 0.70f, 1.0f);
    public static RgbaColor Rebar { get; } = new(0.78f, 0.27f, 0.20f, 1.0f);
    public static RgbaColor Insulation { get; } = new(0.90f, 0.85f, 0.51f, 1.0f);
    public static RgbaColor Glass { get; } = new(0.60f, 0.80f, 0.90f, 0.50f);

    public byte RByte => (byte)Math.Clamp((int)Math.Round(R * 255.0f), 0, 255);
    public byte GByte => (byte)Math.Clamp((int)Math.Round(G * 255.0f), 0, 255);
    public byte BByte => (byte)Math.Clamp((int)Math.Round(B * 255.0f), 0, 255);
    public byte AByte => (byte)Math.Clamp((int)Math.Round(A * 255.0f), 0, 255);

    /// <summary>
    /// Parses an RGBA color from Revit comma-separated "255, 128, 0" strings,
    /// hex strings "#RRGGBB" / "#RRGGBBAA", or normalized float triples.
    /// </summary>
    public static bool TryParse(string? input, out RgbaColor color)
    {
        color = White;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        ReadOnlySpan<char> span = input.AsSpan().Trim();

        // 1. Hex format: #RRGGBB or #RRGGBBAA
        if (span.StartsWith("#", StringComparison.Ordinal))
        {
            span = span[1..];
            if (span.Length == 6)
            {
                if (byte.TryParse(span[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
                    byte.TryParse(span[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
                    byte.TryParse(span[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                {
                    color = new RgbaColor(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
                    return true;
                }
            }
            else if (span.Length == 8)
            {
                if (byte.TryParse(span[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
                    byte.TryParse(span[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
                    byte.TryParse(span[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b) &&
                    byte.TryParse(span[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte a))
                {
                    color = new RgbaColor(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
                    return true;
                }
            }

            return false;
        }

        // 2. Comma-separated or space-separated numbers: "255, 128, 0"
        var parts = input.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
            {
                float a = 1.0f;
                if (parts.Length >= 4 && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedA))
                {
                    a = parsedA > 1.0f ? parsedA / 255.0f : parsedA;
                }

                // If values are 0..255 integer range
                if (r > 1.0f || g > 1.0f || b > 1.0f)
                {
                    r /= 255.0f;
                    g /= 255.0f;
                    b /= 255.0f;
                }

                color = new RgbaColor(
                    Math.Clamp(r, 0.0f, 1.0f),
                    Math.Clamp(g, 0.0f, 1.0f),
                    Math.Clamp(b, 0.0f, 1.0f),
                    Math.Clamp(a, 0.0f, 1.0f));
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns CSS hex string format, e.g. "#FF8000" or "#FF8000FF".</summary>
    public string ToHex(bool includeAlpha = false) =>
        includeAlpha
            ? string.Create(CultureInfo.InvariantCulture, $"#{RByte:X2}{GByte:X2}{BByte:X2}{AByte:X2}")
            : string.Create(CultureInfo.InvariantCulture, $"#{RByte:X2}{GByte:X2}{BByte:X2}");

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"rgba({RByte}, {GByte}, {BByte}, {A:F2})");
}
