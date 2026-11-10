using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using NwcIO.Model;

namespace NwcIO.Internal.Export;

/// <summary>
/// Exports spatial clashes detected between federated models to buildingSMART BIM Collaboration Format
/// (BCF 2.1 zip archive) with viewpoint cameras and highlighted clashing components.
/// </summary>
internal static class BcfExporter
{
    private static readonly XmlWriterSettings XmlSettings = new()
    {
        Indent = true,
        Encoding = new UTF8Encoding(false),
        OmitXmlDeclaration = false,
    };

    /// <summary>
    /// Writes the clash list as a BCF 2.1 (.bcfzip) file to disk.
    /// </summary>
    public static void WriteFile(IReadOnlyList<NwcClash> clashes, string path, string? assemblyName = null)
    {
        ArgumentNullException.ThrowIfNull(clashes);
        ArgumentNullException.ThrowIfNull(path);

        string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var fs = File.Create(path);
        Write(clashes, fs, assemblyName);
    }

    /// <summary>
    /// Writes the clash list as a BCF 2.1 (.bcfzip) archive into the provided stream.
    /// </summary>
    public static void Write(IReadOnlyList<NwcClash> clashes, Stream stream, string? assemblyName = null)
    {
        ArgumentNullException.ThrowIfNull(clashes);
        ArgumentNullException.ThrowIfNull(stream);

        assemblyName = string.IsNullOrWhiteSpace(assemblyName) ? "FederatedAssembly" : assemblyName;
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        // 1. Root bcf.version
        var versionEntry = archive.CreateEntry("bcf.version", CompressionLevel.Optimal);
        using (var writer = new StreamWriter(versionEntry.Open(), new UTF8Encoding(false)))
        {
            writer.WriteLine("""<?xml version="1.0" encoding="UTF-8"?>""");
            writer.WriteLine("""<Version VersionId="2.1" DetailedVersion="2.1" />""");
        }

        // 2. Per-clash topic folder
        for (int i = 0; i < clashes.Count; i++)
        {
            var clash = clashes[i];
            string topicGuid = clash.Id.ToString("D");
            string viewpointGuid = Guid.NewGuid().ToString("D");

            // markup.bcf
            var markupEntry = archive.CreateEntry($"{topicGuid}/markup.bcf", CompressionLevel.Optimal);
            using (var s = markupEntry.Open())
            using (var xml = XmlWriter.Create(s, XmlSettings))
            {
                WriteMarkupXml(xml, clash, i + 1, topicGuid, viewpointGuid, assemblyName, timestamp);
            }

            // viewpoint.bcfv
            var viewpointEntry = archive.CreateEntry($"{topicGuid}/viewpoint.bcfv", CompressionLevel.Optimal);
            using (var s = viewpointEntry.Open())
            using (var xml = XmlWriter.Create(s, XmlSettings))
            {
                WriteViewpointXml(xml, clash, viewpointGuid);
            }
        }
    }

    private static void WriteMarkupXml(
        XmlWriter xml,
        NwcClash clash,
        int index,
        string topicGuid,
        string viewpointGuid,
        string assemblyName,
        string timestamp)
    {
        xml.WriteStartDocument();
        xml.WriteStartElement("Markup");
        xml.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
        xml.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");

        // Header
        xml.WriteStartElement("Header");
        xml.WriteStartElement("File");
        xml.WriteAttributeString("IfcProject", "0$W1402280000000000000");
        xml.WriteAttributeString("IsExternal", "true");
        xml.WriteElementString("Filename", assemblyName);
        xml.WriteElementString("Date", timestamp);
        xml.WriteEndElement(); // File
        xml.WriteEndElement(); // Header

        // Topic
        xml.WriteStartElement("Topic");
        xml.WriteAttributeString("Guid", topicGuid);
        xml.WriteAttributeString("TopicType", "Clash");
        xml.WriteAttributeString("TopicStatus", "Active");

        xml.WriteElementString("Title", clash.Title);
        xml.WriteElementString("Priority", "Normal");
        xml.WriteElementString("Index", index.ToString(CultureInfo.InvariantCulture));
        xml.WriteElementString("CreationDate", timestamp);
        xml.WriteElementString("CreationAuthor", "NwcIO");

        string desc = FormattableString.Invariant(
            $"Geometric clash detected between {clash.ModelAName} (Mesh #{clash.MeshA.Id}, {clash.IfcGuidA}) and {clash.ModelBName} (Mesh #{clash.MeshB.Id}, {clash.IfcGuidB}). Overlap volume: {clash.Volume:F4} m³ at center ({clash.Center.X:F3}, {clash.Center.Y:F3}, {clash.Center.Z:F3}).");
        xml.WriteElementString("Description", desc);
        xml.WriteEndElement(); // Topic

        // Viewpoints
        xml.WriteStartElement("Viewpoints");
        xml.WriteAttributeString("Guid", viewpointGuid);
        xml.WriteElementString("Viewpoint", "viewpoint.bcfv");
        xml.WriteEndElement(); // Viewpoints

        xml.WriteEndElement(); // Markup
        xml.WriteEndDocument();
    }

    private static void WriteViewpointXml(XmlWriter xml, NwcClash clash, string viewpointGuid)
    {
        xml.WriteStartDocument();
        xml.WriteStartElement("VisualizationInfo");
        xml.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
        xml.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
        xml.WriteAttributeString("Guid", viewpointGuid);

        // Components: Selection and Coloring
        xml.WriteStartElement("Components");

        xml.WriteStartElement("Selection");
        xml.WriteStartElement("Component");
        xml.WriteAttributeString("IfcGuid", clash.IfcGuidA);
        xml.WriteEndElement();
        xml.WriteStartElement("Component");
        xml.WriteAttributeString("IfcGuid", clash.IfcGuidB);
        xml.WriteEndElement();
        xml.WriteEndElement(); // Selection

        xml.WriteStartElement("Coloring");

        // Color Component A in Rose/Red (#E11D48)
        xml.WriteStartElement("Color");
        xml.WriteAttributeString("Color", "E11D48");
        xml.WriteStartElement("Component");
        xml.WriteAttributeString("IfcGuid", clash.IfcGuidA);
        xml.WriteEndElement();
        xml.WriteEndElement();

        // Color Component B in Azure/Blue (#2563EB)
        xml.WriteStartElement("Color");
        xml.WriteAttributeString("Color", "2563EB");
        xml.WriteStartElement("Component");
        xml.WriteAttributeString("IfcGuid", clash.IfcGuidB);
        xml.WriteEndElement();
        xml.WriteEndElement();

        xml.WriteEndElement(); // Coloring
        xml.WriteEndElement(); // Components

        // Camera Calculation
        var center = clash.Center;
        var size = clash.IntersectionBox.Size;
        double maxDim = Math.Max(size.X, Math.Max(size.Y, size.Z));
        double viewDistance = maxDim > 0.05 ? Math.Max(3.0, maxDim * 4.0) : 5.0;

        var offset = new Vector3d(1.0, -1.0, 0.75).Normalize() * viewDistance;
        var eye = center + offset;
        var dir = (center - eye).Normalize();
        var up = Math.Abs(dir.Z) > 0.98 ? new Vector3d(0, 1, 0) : new Vector3d(0, 0, 1);

        xml.WriteStartElement("PerspectiveCamera");

        xml.WriteStartElement("CameraViewPoint");
        xml.WriteElementString("X", eye.X.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteElementString("Y", eye.Y.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteElementString("Z", eye.Z.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteEndElement();

        xml.WriteStartElement("CameraDirection");
        xml.WriteElementString("X", dir.X.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteElementString("Y", dir.Y.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteElementString("Z", dir.Z.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteEndElement();

        xml.WriteStartElement("CameraUpVector");
        xml.WriteElementString("X", up.X.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteElementString("Y", up.Y.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteElementString("Z", up.Z.ToString("F4", CultureInfo.InvariantCulture));
        xml.WriteEndElement();

        xml.WriteElementString("FieldOfView", "60");

        xml.WriteEndElement(); // PerspectiveCamera

        xml.WriteEndElement(); // VisualizationInfo
        xml.WriteEndDocument();
    }
}
