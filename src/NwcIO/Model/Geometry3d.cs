namespace NwcIO.Model;

/// <summary>Represents a 3D vector with double-precision components.</summary>
public readonly record struct Vector3d(double X, double Y, double Z)
{
    public static Vector3d Zero { get; } = new(0, 0, 0);

    public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
}

/// <summary>Represents a unit rotation quaternion with double-precision components (X, Y, Z, W).</summary>
public readonly record struct Quaterniond(double X, double Y, double Z, double W)
{
    public static Quaterniond Identity { get; } = new(0, 0, 0, 1);

    /// <summary>Returns true if this quaternion is approximately normalized.</summary>
    public bool IsNormalized(double epsilon = 1e-4) =>
        Math.Abs((X * X + Y * Y + Z * Z + W * W) - 1.0) < epsilon;

    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4}, {W:F4})";
}

/// <summary>An axis-aligned 3D bounding box defined by minimum and maximum extents.</summary>
public readonly record struct BoundingBox3d(Vector3d Min, Vector3d Max)
{
    public Vector3d Center => new((Min.X + Max.X) * 0.5, (Min.Y + Max.Y) * 0.5, (Min.Z + Max.Z) * 0.5);

    public Vector3d Size => new(Max.X - Min.X, Max.Y - Min.Y, Max.Z - Min.Z);

    public override string ToString() => $"[Min: {Min}, Max: {Max}]";
}
