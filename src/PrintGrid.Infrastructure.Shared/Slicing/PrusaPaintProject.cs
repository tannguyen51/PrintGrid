using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Infrastructure.Shared.Slicing;

/// <summary>Exports only validated geometry and region assignments, never uploaded print scripts or presets.</summary>
public static class PrusaPaintProject
{
    private const string Core = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
    private const string PaintNs = "http://schemas.slic3r.org/3mf/2017/06";
    public sealed record Plate(string Id, string Path, decimal WidthMm, decimal DepthMm, decimal HeightMm, int[] Tools, decimal RequiredBedWidthMm);
    public sealed record Project(IReadOnlyList<Plate> Plates, IReadOnlyList<ColorGroupAssignment> Assignments);

    public static (decimal Width, decimal Depth, decimal Height) Envelope(ThreeMfPackage.NativeDocument document)
    {
        var bounds = document.Meshes.GroupBy(m => m.Plate).Select(g => Bounds(g, 1m)).ToList();
        return ((decimal)bounds.Max(b => b.Max.X - b.Min.X), (decimal)bounds.Max(b => b.Max.Y - b.Min.Y),
            (decimal)bounds.Max(b => b.Max.Z - b.Min.Z));
    }

    public static Project Write(ThreeMfPackage.NativeDocument document, IReadOnlyList<string> colors,
        IReadOnlyList<ColorGroupAssignment>? assignments, string directory, decimal scale,
        decimal bedWidth, decimal bedDepth, decimal bedHeight, decimal towerWidth = 20,
        Func<decimal, decimal, decimal, bool>? fits = null)
    {
        if (scale <= 0 || bedWidth <= 0 || bedDepth <= 0 || bedHeight <= 0 || towerWidth <= 0)
            throw new InvalidDataException("Kích thước in không hợp lệ.");
        var source = document.GroupIds.Order(StringComparer.Ordinal).ToDictionary(id => id, id => document.Palette.GetValueOrDefault(id), StringComparer.Ordinal);
        if (assignments is null)
        {
            if (colors.Count != 1 && colors.Count != source.Count)
                throw new InvalidDataException("Số màu in không khớp số nhóm màu trong file.");
            assignments = source.Keys.Select((id, i) => new ColorGroupAssignment(id, colors[colors.Count == 1 ? 0 : i])).ToList();
        }
        var frozen = ColorGroupMapping.Resolve(source, assignments, colors);
        var targets = colors.Select(MaterialColorCodes.Normalize).ToArray();
        var byId = frozen.ToDictionary(a => a.GroupId, a => Array.IndexOf(targets, a.ColorCode) + 1, StringComparer.Ordinal);
        var cache = new Dictionary<(string, int), (string Paint, int[] Tools)>();
        (string Paint, int[] Tools) Painting(ThreeMfPackage.NativeFace face, int defaultSlot)
        {
            if (string.IsNullOrEmpty(face.Paint))
            {
                var tool = byId[face.GroupId];
                return (ThreeMfPaint.Remap("", tool, n => n), [tool]);
            }
            if (cache.TryGetValue((face.Paint, defaultSlot), out var cached)) return cached;
            var used = new HashSet<int>();
            var paint = ThreeMfPaint.Remap(face.Paint, defaultSlot, slot =>
            {
                var id = $"filament:{slot:D8}";
                if (!byId.TryGetValue(id, out var target)) throw new InvalidDataException("Nhóm màu trong file chưa được gán màu in.");
                used.Add(target); return target;
            });
            var value = (paint, used.Order().ToArray());
            if (cache.Count < 8192) cache[(face.Paint, defaultSlot)] = value;
            return value;
        }
        var plates = new List<Plate>();
        foreach (var plate in document.Meshes.GroupBy(m => m.Plate).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var tools = new HashSet<int>();
            foreach (var part in plate)
            foreach (var face in part.Faces)
                tools.UnionWith(Painting(face, part.DefaultSlot).Tools);
            var activeTools = tools.Order().ToArray();
            var localTools = activeTools.Select((tool, i) => (Tool: tool, Local: i + 1))
                .ToDictionary(pair => pair.Tool, pair => pair.Local);
            var needsRemap = activeTools.Where((tool, i) => tool != i + 1).Any();
            var localPaint = new Dictionary<string, string>(StringComparer.Ordinal);
            string LocalPaint(string paint)
            {
                if (!needsRemap) return paint;
                if (localPaint.TryGetValue(paint, out var cached)) return cached;
                var mapped = ThreeMfPaint.Remap(paint, 1, tool => localTools[tool]);
                if (localPaint.Count < 8192) localPaint[paint] = mapped;
                return mapped;
            }
            var reserve = tools.Count > 1 ? towerWidth + 10 : 0;
            var bounds = Bounds(plate, scale);
            var size = bounds.Max - bounds.Min;
            if (size.X <= 0 || size.Y <= 0 || size.Z <= 0 || size.X + (float)reserve > (float)bedWidth
                || size.Y > (float)bedDepth || size.Z > (float)bedHeight
                || fits is not null && !fits((decimal)size.X + reserve, (decimal)size.Y, (decimal)size.Z))
                throw new InvalidDataException($"Bàn in {plate.Key} vượt khổ máy sau khi chừa chỗ đổi màu. Hãy chọn kích thước nhỏ hơn.");
            var offset = new Vector3((float)(bedWidth + reserve) / 2 - (bounds.Min.X + bounds.Max.X) / 2,
                (float)bedDepth / 2 - (bounds.Min.Y + bounds.Max.Y) / 2, -bounds.Min.Z);
            var path = Path.Combine(directory, $"plate-{plates.Count + 1}.3mf");
            using var output = File.Create(path);
            using var archive = new ZipArchive(output, ZipArchiveMode.Create);
            WriteText(archive, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"model\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodel+xml\"/></Types>");
            WriteText(archive, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Target=\"/3D/3dmodel.model\" Id=\"rel0\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\"/></Relationships>");
            var metadata = new XElement("config");
            using (var entry = archive.CreateEntry("3D/3dmodel.model", CompressionLevel.Fastest).Open())
            using (var xml = XmlWriter.Create(entry, new XmlWriterSettings { Encoding = new UTF8Encoding(false) }))
            {
                xml.WriteStartElement("model", Core); xml.WriteAttributeString("unit", "millimeter");
                xml.WriteAttributeString("xmlns", "slic3rpe", null, PaintNs);
                Meta(xml, "slic3rpe:Version3mf", "1"); Meta(xml, "slic3rpe:MmPaintingVersion", "1");
                xml.WriteStartElement("resources", Core);
                var objectId = 0;
                foreach (var root in plate.GroupBy(p => p.RootInstance))
                {
                    objectId++;
                    var objectMeta = new XElement("object", new XAttribute("id", objectId)); metadata.Add(objectMeta);
                    xml.WriteStartElement("object", Core); xml.WriteAttributeString("id", objectId.ToString(CultureInfo.InvariantCulture));
                    xml.WriteAttributeString("type", "model"); xml.WriteStartElement("mesh", Core);
                    xml.WriteStartElement("vertices", Core);
                    var maps = new List<(ThreeMfPackage.NativeMesh Mesh, int[] Indices)>();
                    var vertexCount = 0;
                    foreach (var part in root)
                    {
                        var indices = new int[part.Vertices.Length]; Array.Fill(indices, -1);
                        foreach (var face in part.Faces) { indices[face.A] = 0; indices[face.B] = 0; indices[face.C] = 0; }
                        for (var i = 0; i < indices.Length; i++)
                        {
                            if (indices[i] < 0) continue;
                            indices[i] = vertexCount++;
                            var v = part.Vertices[i] * (float)scale + offset;
                            xml.WriteStartElement("vertex", Core);
                            xml.WriteAttributeString("x", v.X.ToString("R", CultureInfo.InvariantCulture));
                            xml.WriteAttributeString("y", v.Y.ToString("R", CultureInfo.InvariantCulture));
                            xml.WriteAttributeString("z", v.Z.ToString("R", CultureInfo.InvariantCulture)); xml.WriteEndElement();
                        }
                        maps.Add((part, indices));
                    }
                    xml.WriteEndElement(); xml.WriteStartElement("triangles", Core);
                    var faceCount = 0;
                    foreach (var (part, indices) in maps)
                    {
                        var first = faceCount;
                        var defaultTool = localTools[Painting(part.Faces[0], part.DefaultSlot).Tools[0]];
                        foreach (var face in part.Faces)
                        {
                            var paint = Painting(face, part.DefaultSlot).Paint;
                            xml.WriteStartElement("triangle", Core);
                            xml.WriteAttributeString("v1", indices[face.A].ToString(CultureInfo.InvariantCulture));
                            xml.WriteAttributeString("v2", indices[face.B].ToString(CultureInfo.InvariantCulture));
                            xml.WriteAttributeString("v3", indices[face.C].ToString(CultureInfo.InvariantCulture));
                            if (activeTools.Length > 1)
                                xml.WriteAttributeString("slic3rpe", "mmu_segmentation", PaintNs, LocalPaint(paint));
                            xml.WriteEndElement();
                            faceCount++;
                        }
                        objectMeta.Add(new XElement("volume", new XAttribute("firstid", first), new XAttribute("lastid", faceCount - 1),
                            new XElement("metadata", new XAttribute("type", "volume"), new XAttribute("key", "extruder"), new XAttribute("value", defaultTool))));
                    }
                    xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndElement();
                }
                xml.WriteEndElement(); xml.WriteStartElement("build", Core);
                for (var i = 1; i <= objectId; i++)
                { xml.WriteStartElement("item", Core); xml.WriteAttributeString("objectid", i.ToString(CultureInfo.InvariantCulture)); xml.WriteEndElement(); }
                xml.WriteEndElement(); xml.WriteEndElement();
            }
            WriteText(archive, "Metadata/Slic3r_PE_model.config", metadata.ToString(SaveOptions.DisableFormatting));
            plates.Add(new(plate.Key, path, (decimal)size.X, (decimal)size.Y, (decimal)size.Z, activeTools, (decimal)size.X + reserve));
        }
        return new(plates, frozen);
    }

    private static (Vector3 Min, Vector3 Max) Bounds(IEnumerable<ThreeMfPackage.NativeMesh> meshes, decimal scale)
    {
        var min = new Vector3(float.PositiveInfinity); var max = new Vector3(float.NegativeInfinity);
        foreach (var mesh in meshes)
        foreach (var face in mesh.Faces)
        {
            min = Vector3.Min(min, Vector3.Min(mesh.Vertices[face.A], Vector3.Min(mesh.Vertices[face.B], mesh.Vertices[face.C])));
            max = Vector3.Max(max, Vector3.Max(mesh.Vertices[face.A], Vector3.Max(mesh.Vertices[face.B], mesh.Vertices[face.C])));
        }
        return (min * (float)scale, max * (float)scale);
    }
    private static void WriteText(ZipArchive zip, string name, string text)
    { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); writer.Write(text); }
    private static void Meta(XmlWriter xml, string name, string value)
    { xml.WriteStartElement("metadata", Core); xml.WriteAttributeString("name", name); xml.WriteString(value); xml.WriteEndElement(); }
}
