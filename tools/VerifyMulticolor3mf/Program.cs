using System.IO.Compression;
using System.Text.Json;
using System.Xml;
using PrintGrid.Infrastructure.Shared.Slicing;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using PrintGrid.SharedKernel.ValueObjects;

if (args.Length == 0)
    throw new ArgumentException("Pass one or more local 3MF sample paths.");

foreach (var path in args)
{
    await using var source = File.OpenRead(path);
    var geometry = await new PrintSlicingService().AnalyzeAsync(source, "3MF");
    Console.WriteLine(JsonSerializer.Serialize(new { File = Path.GetFileName(path), Geometry = geometry }));
    // Match the upload pipeline: topology warnings are advisory; native slicing
    // determines printability and must be checked separately from this export check.
    if (!geometry.IsValid)
        throw new InvalidDataException($"Invalid source geometry: {Path.GetFileName(path)}");
    source.Position = 0;
    var document = ThreeMfPackage.ReadNative(source);
    var groups = document.GroupIds.Order(StringComparer.Ordinal).ToArray();
    if (groups.Length != 2) throw new InvalidDataException("Expected a two-color sample.");
    string[] colors = ["WHITE", "BLUE"];
    var assignments = groups.Select((id, i) => new ColorGroupAssignment(id, colors[i])).ToArray();
    var directory = Directory.CreateTempSubdirectory("printgrid-3mf-verification-");
    try
    {
        var project = PrusaPaintProject.Write(document, colors, assignments, directory.FullName,
            1, 320, 320, 400);
        if (project.Plates.Count != document.PlateCount ||
            !project.Assignments.Select(a => (a.GroupId, a.ColorCode))
                .SequenceEqual(assignments.Select(a => (a.GroupId, a.ColorCode))))
            throw new InvalidDataException("Plate count or group assignments changed during export.");
        var plates = project.Plates.Select(plate =>
        {
            using var zip = ZipFile.OpenRead(plate.Path);
            using var mesh = zip.GetEntry("3D/3dmodel.model")!.Open();
            using var xml = XmlReader.Create(mesh, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            var faces = 0;
            var painted = false;
            while (xml.Read())
            {
                if (xml.NodeType != XmlNodeType.Element || xml.LocalName != "triangle") continue;
                faces++;
                painted |= xml.GetAttribute("mmu_segmentation", "http://schemas.slic3r.org/3mf/2017/06") is not null;
            }
            var originals = document.Meshes.Where(m => m.Plate == plate.Id).Sum(m => m.Faces.Length);
            if (faces != originals)
                throw new InvalidDataException("Export changed the number of original faces.");
            if (plate.Tools.Length == 1 && painted)
                throw new InvalidDataException("Single-color plate retained unnecessary painting.");
            return new { plate.Id, plate.WidthMm, plate.DepthMm, plate.HeightMm,
                plate.RequiredBedWidthMm, plate.Tools, OriginalFaces = originals };
        }).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new { File = Path.GetFileName(path),
            geometry.IsWatertight, geometry.IsManifold, Envelope = PrusaPaintProject.Envelope(document).ToString(),
            document.HasSurfacePainting, Groups = groups, Plates = plates }));
    }
    finally { directory.Delete(recursive: true); }
}
