namespace NwcIO;

/// <summary>
/// Length units supported for IFC export.
/// </summary>
public enum NwcIfcLengthUnit
{
    /// <summary>
    /// Length unit is millimetres (IFCSIUNIT(*,.LENGTHUNIT.,.MILLI.,.METRE.)).
    /// Default for Navisworks and Revit metric models.
    /// </summary>
    Millimetre,

    /// <summary>
    /// Length unit is metres (IFCSIUNIT(*,.LENGTHUNIT.,$,.METRE.)).
    /// Vertices are scaled by 0.001 if source is in millimetres.
    /// </summary>
    Metre,
}

/// <summary>
/// Options controlling IFC (Industry Foundation Classes) export.
/// Generates IFC2x3 Coordination View V2.0 (ISO 10303-21 STEP physical file format).
/// </summary>
public sealed class NwcIfcExportOptions
{
    /// <summary>Default export configuration.</summary>
    public static NwcIfcExportOptions Default { get; } = new();

    /// <summary>IFC Schema version string (default: "IFC2X3").</summary>
    public string Schema { get; set; } = "IFC2X3";

    /// <summary>Length unit for coordinates in the IFC model (default: Millimetre).</summary>
    public NwcIfcLengthUnit LengthUnit { get; set; } = NwcIfcLengthUnit.Millimetre;

    /// <summary>Convert millimetre source coordinates to metres when <see cref="LengthUnit"/> is Metre (default: true).</summary>
    public bool ConvertMmToMetres { get; set; } = true;

    /// <summary>Export surface materials and RGB colors as IfcSurfaceStyle / IfcStyledItem (default: true).</summary>
    public bool ExportMaterials { get; set; } = true;

    /// <summary>Export properties and metadata as IfcPropertySet and IfcPropertySingleValue (default: true).</summary>
    public bool ExportProperties { get; set; } = true;

    /// <summary>Export geometry in world coordinates applying 3D transformations (default: true).</summary>
    public bool WorldCoordinates { get; set; } = true;

    /// <summary>Share IfcCartesianPoint instances per mesh using mesh index triplets to minimize file size (default: true).</summary>
    public bool SharedVertices { get; set; } = true;

    /// <summary>Custom project name for IfcProject (defaults to document or assembly name).</summary>
    public string? ProjectName { get; set; }

    /// <summary>Custom building name for IfcBuilding (default: "Default Building").</summary>
    public string? BuildingName { get; set; }

    /// <summary>Custom storey name for IfcBuildingStorey (default: "Level 1").</summary>
    public string? StoreyName { get; set; }

    /// <summary>Author string for the STEP header (default: "mahach666").</summary>
    public string Author { get; set; } = "mahach666";

    /// <summary>Organization string for the STEP header (default: "NwcIO").</summary>
    public string Organization { get; set; } = "NwcIO";

    /// <summary>Application name for the STEP header (default: "NwcIO").</summary>
    public string Application { get; set; } = "NwcIO";
}
