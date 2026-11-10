namespace NwcIO.Model;

/// <summary>
/// A hierarchical item in the Navisworks scene graph (model item, layer, collection,
/// category, family, type or element instance).
/// </summary>
public sealed class NwcNode
{
    public required int Id { get; init; }

    public required string Name { get; set; }

    public required string Class { get; set; }

    public int Tag { get; set; }

    public string? Value { get; set; }

    /// <summary>Parent node in the scene tree, or null for root nodes.</summary>
    public NwcNode? Parent { get; set; }

    /// <summary>Child nodes nested under this item.</summary>
    public List<NwcNode> Children { get; } = [];

    /// <summary>Properties attached directly to this node.</summary>
    public List<NwcPropertyRecord> Properties { get; } = [];

    /// <summary>Element unique identifier (GUID) if assigned.</summary>
    public Guid? ElementGuid { get; set; }

    /// <summary>3D spatial transformation (pose and bounding box) if present.</summary>
    public NwcTransform? Transform { get; set; }

    public override string ToString() => $"{Name} ({Class}) [Children: {Children.Count}, Props: {Properties.Count}]";
}
