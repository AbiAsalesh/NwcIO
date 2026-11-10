namespace NwcIO.Model;

/// <summary>
/// Represents a spatial clash (geometric intersection or overlap) detected between elements
/// from different models within a federated BIM assembly.
/// </summary>
/// <param name="ModelA">First contributing model.</param>
/// <param name="MeshA">Clashing mesh from the first model.</param>
/// <param name="ModelB">Second contributing model.</param>
/// <param name="MeshB">Clashing mesh from the second model.</param>
/// <param name="IntersectionBox">Axis-aligned bounding box of the geometric overlap volume.</param>
public sealed record NwcClash(
    NwcDocument ModelA,
    NwcMesh MeshA,
    NwcDocument ModelB,
    NwcMesh MeshB,
    BoundingBox3d IntersectionBox);
