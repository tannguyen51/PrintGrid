using System.Buffers.Binary;
using System.Text.Json;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

/// <summary>
/// Parses binary glTF (.glb) into the same <see cref="MeshStats"/> contract as the
/// STL/OBJ/3MF parsers: bounding box (mm), enclosed volume (cm³) via signed
/// tetrahedra, face/vertex counts, watertight/manifold flags. Pure C#, no glTF library.
///
/// GLB layout: 12-byte header (magic "glTF", version 2, total length) followed by
/// chunks — a JSON chunk describing the scene and a BIN chunk holding the buffers.
/// Only TRIANGLES primitives (mode 4) contribute; other modes are skipped rather than
/// rejected, so a file mixing a printable mesh with lines/points still analyses.
///
/// Units: glTF has no unit, and authoring tools (Blender, three.js) most often work in
/// metres while this pipeline is millimetre-based. A printable part is never smaller
/// than a millimetre, so a bounding box whose longest side is under 1 is read as metres
/// and scaled by 1000 — the alternative silently quotes a 20 mm part as 0.02 mm.
/// </summary>
internal static class GlbMeshParser
{
    private const int HeaderSize = 12;
    private const int ChunkHeaderSize = 8;
    private const uint GlbMagic = 0x46546C67;      // "glTF" little-endian
    private const uint JsonChunkType = 0x4E4F534A; // "JSON"
    private const uint BinChunkType = 0x004E4942;  // "BIN\0"

    private const int ComponentTypeFloat = 5126;
    private const int ComponentTypeUnsignedByte = 5121;
    private const int ComponentTypeUnsignedShort = 5123;
    private const int ComponentTypeUnsignedInt = 5125;
    private const int ModeTriangles = 4;

    public static MeshStats Parse(Stream stream)
    {
        // Deliberately NOT disposed: the caller owns this stream (ModelFileInspector resets
        // Position after the call).
        var buffered = new BufferedStream(stream, 64 * 1024);
        var (json, binary) = ReadContainer(buffered);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("meshes", out var meshes) || meshes.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("glTF contains no meshes");
        if (!root.TryGetProperty("accessors", out var accessors))
            throw new InvalidDataException("glTF contains no accessors");
        if (!root.TryGetProperty("bufferViews", out var bufferViews))
            throw new InvalidDataException("glTF contains no bufferViews");

        var vertices = new List<(double X, double Y, double Z)>();
        var triangles = new List<(int A, int B, int C)>();

        foreach (var mesh in meshes.EnumerateArray())
        {
            if (!mesh.TryGetProperty("primitives", out var primitives) || primitives.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var primitive in primitives.EnumerateArray())
            {
                var mode = primitive.TryGetProperty("mode", out var modeElement) ? modeElement.GetInt32() : ModeTriangles;
                if (mode != ModeTriangles) continue;

                if (!primitive.TryGetProperty("attributes", out var attributes) ||
                    !attributes.TryGetProperty("POSITION", out var positionElement))
                    throw new InvalidDataException("glTF primitive has no POSITION attribute");

                var firstVertex = vertices.Count;
                ReadPositions(accessors, bufferViews, binary, positionElement.GetInt32(), vertices);
                var vertexCount = vertices.Count - firstVertex;
                if (vertexCount == 0) continue;

                if (primitive.TryGetProperty("indices", out var indicesElement))
                {
                    var indices = ReadIndices(accessors, bufferViews, binary, indicesElement.GetInt32());
                    for (var i = 0; i + 2 < indices.Count; i += 3)
                    {
                        var a = firstVertex + (int)indices[i];
                        var b = firstVertex + (int)indices[i + 1];
                        var c = firstVertex + (int)indices[i + 2];
                        if (a >= vertices.Count || b >= vertices.Count || c >= vertices.Count)
                            throw new InvalidDataException("glTF triangle references a missing vertex");
                        triangles.Add((a, b, c));
                    }
                }
                else
                {
                    // Non-indexed primitive: vertices arrive as consecutive triples.
                    for (var i = 0; i + 2 < vertexCount; i += 3)
                        triangles.Add((firstVertex + i, firstVertex + i + 1, firstVertex + i + 2));
                }
            }
        }

        if (vertices.Count == 0 || triangles.Count == 0)
            throw new InvalidDataException("glTF model contains no printable mesh");

        return Summarise(vertices, triangles);
    }

    private static MeshStats Summarise(List<(double X, double Y, double Z)> vertices, List<(int A, int B, int C)> triangles)
    {
        var min = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
        var max = new[] { double.MinValue, double.MinValue, double.MinValue };
        foreach (var vertex in vertices)
        {
            min[0] = Math.Min(min[0], vertex.X); max[0] = Math.Max(max[0], vertex.X);
            min[1] = Math.Min(min[1], vertex.Y); max[1] = Math.Max(max[1], vertex.Y);
            min[2] = Math.Min(min[2], vertex.Z); max[2] = Math.Max(max[2], vertex.Z);
        }

        var width = max[0] - min[0];
        var depth = max[1] - min[1];
        var height = max[2] - min[2];
        var scale = Math.Max(width, Math.Max(depth, height)) is > 0 and < 1 ? 1000.0 : 1.0;

        double signedVolume = 0, absoluteVolume = 0;
        var edges = new Dictionary<(double, double, double, double, double, double), int>();
        foreach (var (a, b, c) in triangles)
        {
            var p = vertices[a];
            var q = vertices[b];
            var r = vertices[c];

            var volume = (p.X * (q.Y * r.Z - q.Z * r.Y)
                - p.Y * (q.X * r.Z - q.Z * r.X)
                + p.Z * (q.X * r.Y - q.Y * r.X)) / 6.0;
            signedVolume += volume;
            absoluteVolume += Math.Abs(volume);

            CountEdge(edges, p, q);
            CountEdge(edges, q, r);
            CountEdge(edges, r, p);
        }

        var closed = Math.Abs(signedVolume) > 1e-6;
        var volumeMm3 = closed ? Math.Abs(signedVolume) : absoluteVolume;

        return new MeshStats(
            WidthMm: width * scale,
            DepthMm: depth * scale,
            HeightMm: height * scale,
            VolumeCm3: volumeMm3 * scale * scale * scale / 1000.0,
            VertexCount: vertices.Count,
            FaceCount: triangles.Count,
            IsWatertight: edges.Count > 0 && edges.Values.All(count => count == 2),
            IsManifold: edges.Values.All(count => count <= 2));
    }

    /// <summary>
    /// Edges are keyed by rounded position, not by index: glTF exporters routinely split
    /// vertices per face (no shared indices), and index-keyed edges would then report
    /// every closed mesh as open. Rounded to 0.1 µm, matching the STL parser's semantics.
    /// </summary>
    private static void CountEdge(
        Dictionary<(double, double, double, double, double, double), int> edges,
        (double X, double Y, double Z) a,
        (double X, double Y, double Z) b)
    {
        var p = (Math.Round(a.X, 4), Math.Round(a.Y, 4), Math.Round(a.Z, 4));
        var q = (Math.Round(b.X, 4), Math.Round(b.Y, 4), Math.Round(b.Z, 4));
        var key = p.CompareTo(q) <= 0 ? (p.Item1, p.Item2, p.Item3, q.Item1, q.Item2, q.Item3)
                                      : (q.Item1, q.Item2, q.Item3, p.Item1, p.Item2, p.Item3);
        edges[key] = edges.GetValueOrDefault(key) + 1;
    }

    private static (byte[] Json, byte[] Binary) ReadContainer(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        ReadExact(stream, header);

        if (BinaryPrimitives.ReadUInt32LittleEndian(header) != GlbMagic)
            throw new InvalidDataException("Not a binary glTF container (glTF magic missing)");

        var version = BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
        if (version != 2)
            throw new InvalidDataException($"Unsupported glTF version {version}");

        var declaredLength = BinaryPrimitives.ReadUInt32LittleEndian(header[8..]);
        if (declaredLength < HeaderSize)
            throw new InvalidDataException("glTF header declares an impossible length");

        byte[]? json = null;
        byte[]? binary = null;
        long consumed = HeaderSize;
        Span<byte> chunkHeader = stackalloc byte[ChunkHeaderSize];

        while (consumed + ChunkHeaderSize <= declaredLength)
        {
            ReadExact(stream, chunkHeader);
            var chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader);
            var chunkType = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader[4..]);
            if (chunkLength == 0 || consumed + ChunkHeaderSize + chunkLength > declaredLength)
                throw new InvalidDataException("glTF chunk length runs past the declared file size");

            var data = new byte[chunkLength];
            ReadExact(stream, data);
            consumed += ChunkHeaderSize + chunkLength;

            switch (chunkType)
            {
                case JsonChunkType:
                    json ??= data;
                    break;
                case BinChunkType:
                    binary ??= data;
                    break;
            }

            // Chunks are padded to a 4-byte boundary; the padding is outside chunkLength.
            var padding = (int)((4 - (chunkLength % 4)) % 4);
            if (padding > 0)
            {
                ReadExact(stream, new byte[padding]);
                consumed += padding;
            }
        }

        if (json is null)
            throw new InvalidDataException("glTF container has no JSON chunk");

        return (json, binary ?? []);
    }

    private static void ReadPositions(
        JsonElement accessors,
        JsonElement bufferViews,
        byte[] binary,
        int accessorIndex,
        List<(double X, double Y, double Z)> vertices)
    {
        var accessor = ElementAt(accessors, accessorIndex, "accessor");
        var type = accessor.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
        if (type != "VEC3")
            throw new InvalidDataException($"glTF POSITION accessor must be VEC3 (found {type ?? "none"})");

        var componentType = accessor.GetProperty("componentType").GetInt32();
        if (componentType != ComponentTypeFloat)
            throw new InvalidDataException("glTF POSITION accessor must use float components");

        var (start, stride, count) = Locate(accessor, bufferViews, binary, 12);

        for (var i = 0; i < count; i++)
        {
            var offset = start + i * stride;
            vertices.Add((
                BinaryPrimitives.ReadSingleLittleEndian(binary.AsSpan(offset, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(binary.AsSpan(offset + 4, 4)),
                BinaryPrimitives.ReadSingleLittleEndian(binary.AsSpan(offset + 8, 4))));
        }
    }

    private static List<uint> ReadIndices(JsonElement accessors, JsonElement bufferViews, byte[] binary, int accessorIndex)
    {
        var accessor = ElementAt(accessors, accessorIndex, "accessor");
        var type = accessor.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
        if (type != "SCALAR")
            throw new InvalidDataException($"glTF index accessor must be SCALAR (found {type ?? "none"})");

        var componentType = accessor.GetProperty("componentType").GetInt32();
        var elementSize = componentType switch
        {
            ComponentTypeUnsignedByte => 1,
            ComponentTypeUnsignedShort => 2,
            ComponentTypeUnsignedInt => 4,
            _ => throw new InvalidDataException($"glTF index accessor uses unsupported component type {componentType}")
        };

        var (start, stride, count) = Locate(accessor, bufferViews, binary, elementSize);
        var indices = new List<uint>(count);

        for (var i = 0; i < count; i++)
        {
            var offset = start + i * stride;
            indices.Add(componentType switch
            {
                ComponentTypeUnsignedByte => binary[offset],
                ComponentTypeUnsignedShort => BinaryPrimitives.ReadUInt16LittleEndian(binary.AsSpan(offset, 2)),
                _ => BinaryPrimitives.ReadUInt32LittleEndian(binary.AsSpan(offset, 4))
            });
        }

        return indices;
    }

    /// <summary>Resolves accessor + bufferView offsets into the BIN chunk and bounds-checks them.</summary>
    private static (int Start, int Stride, int Count) Locate(JsonElement accessor, JsonElement bufferViews, byte[] binary, int elementSize)
    {
        var count = accessor.GetProperty("count").GetInt32();
        if (count < 0)
            throw new InvalidDataException("glTF accessor declares a negative count");

        var view = ElementAt(bufferViews, accessor.GetProperty("bufferView").GetInt32(), "bufferView");
        var stride = view.TryGetProperty("byteStride", out var strideElement) ? strideElement.GetInt32() : elementSize;
        if (stride < elementSize)
            throw new InvalidDataException("glTF bufferView byteStride is smaller than one element");

        var start = (view.TryGetProperty("byteOffset", out var viewOffset) ? viewOffset.GetInt32() : 0)
            + (accessor.TryGetProperty("byteOffset", out var accessorOffset) ? accessorOffset.GetInt32() : 0);

        if (start < 0 || count > 0 && (long)start + (long)stride * (count - 1) + elementSize > binary.Length)
            throw new InvalidDataException("glTF accessor reads past the binary chunk");

        return (start, stride, count);
    }

    private static JsonElement ElementAt(JsonElement array, int index, string what)
    {
        if (index < 0 || index >= array.GetArrayLength())
            throw new InvalidDataException($"glTF {what} index {index} is out of range");
        return array[index];
    }

    private static void ReadExact(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read <= 0) throw new EndOfStreamException("Unexpected end of GLB stream");
            total += read;
        }
    }
}