using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Infrastructure.Shared.Slicing;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Infrastructure.Slicing;
using PrintGrid.Modules.Scheduling.Infrastructure.Slicing;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Customer;

public sealed class MeshImportQuoteTests
{
    [Fact]
    public void Obj_triangulates_polygons_and_resolves_negative_texture_normal_indices()
    {
        using var source = Obj("f -4/1/1 -3/2/1 -2/3/1 -1/4/1 # quad");
        var triangles = QuoteMesh.Read(source, ".obj").Single();
        triangles.Should().HaveCount(2);
        triangles[0].Should().Be(new QuoteMesh.Triangle(Vector3.Zero, new(10, 0, 0), new(0, 20, 0)));
        triangles[1].Should().Be(new QuoteMesh.Triangle(Vector3.Zero, new(0, 20, 0), new(0, 0, 30)));
        source.CanRead.Should().BeTrue();
    }

    [Theory]
    [InlineData("f 0 2 3")]
    [InlineData("f 1 2 5")]
    [InlineData("f -5 -2 -1")]
    [InlineData("f 1 2")]
    public void Obj_rejects_invalid_faces(string face)
    {
        using var source = Obj(face);
        var action = () => QuoteMesh.Read(source, "OBJ");
        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public async Task Glb_analysis_and_quote_apply_parent_transforms_and_instances_in_default_scene()
    {
        using var source = Glb(root => root["nodes"] = JsonNode.Parse("""
            [{"scale":[2,3,4],"children":[1,2]},
             {"mesh":0,"translation":[10,0,0]},
             {"mesh":0,"translation":[40,0,0]},
             {"mesh":0,"translation":[1000000,0,0]}]
            """));
        var triangles = QuoteMesh.Read(source, "GLB").Single();
        triangles.Should().HaveCount(8);
        Bounds(triangles).Should().Be(new Vector3(80, 60, 120));
        triangles.Select(t => t.A.X).Min().Should().Be(20);
        source.Position = 0;
        var analysis = await new PrintSlicingService().AnalyzeAsync(source, "GLB");
        analysis.IsValid.Should().BeTrue(analysis.ErrorMessage);
        (analysis.WidthMm, analysis.DepthMm, analysis.HeightMm).Should().Be((80m, 60m, 120m));
        analysis.FaceCount.Should().Be(8);
    }

    [Fact]
    public void Glb_bakes_joint_and_inverse_bind_transforms_in_authored_pose_without_playing_animation()
    {
        using var source = Glb(root => {
            root["nodes"] = JsonNode.Parse("""
                [{"children":[1,2]}, {"mesh":0,"skin":0},
                 {"scale":[2,2,2],"translation":[7,8,9]}]
                """);
            // Deliberately irrelevant malformed animation data must not block static printing.
            root["animations"] = JsonNode.Parse("""[{"samplers":[{"input":999,"output":999}],"channels":[]}]""");
        }, skinned: true);
        var triangles = QuoteMesh.Read(source, "GLB").Single();
        triangles[0].A.Should().Be(new Vector3(3, 2, 1));
        Bounds(triangles).Should().Be(new Vector3(20, 40, 60));
        source.CanRead.Should().BeTrue();
    }

    [Fact]
    public void Glb_metres_have_the_same_millimetre_dimensions_in_analysis_and_quote()
    {
        using var source = Glb(vertexScale: .001f);
        var size = Bounds(QuoteMesh.Read(source, "GLB").Single());
        size.X.Should().BeApproximately(10, .0001f);
        size.Y.Should().BeApproximately(20, .0001f);
        size.Z.Should().BeApproximately(30, .0001f);
    }

    [Fact]
    public void Glb_rejects_out_of_range_indices()
    {
        using var source = Glb(invalidIndex: true);
        var action = () => QuoteMesh.Read(source, "GLB");
        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Glb_does_not_read_external_buffers()
    {
        using var source = Glb(root => root["buffers"]![0]!["uri"] = "file:///nonexistent/private.bin");
        var action = () => QuoteMesh.Read(source, "GLB");
        action.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("GLB")]
    [InlineData("OBJ")]
    public async Task Supported_upload_formats_reach_real_slicer_build_plate_validation(string format)
    {
        var storage = Substitute.For<IFileStorage>();
        storage.DownloadAsync("models", "part", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(format == "GLB" ? Glb() : Obj("f 1 3 2\nf 1 2 4\nf 2 3 4\nf 3 1 4")));
        var registry = Substitute.For<ICuraProfileRegistry>();
        registry.ResolveAsync(Arg.Any<Guid>(), "PLA", Arg.Any<CancellationToken>(), null)
            .Returns(new RegisteredCuraSettings("""{"global":{"machine_width":1,"machine_depth":1,"machine_height":1}}""", 4, 6000, .08m));
        var slicer = new CuraQuoteSlicer(storage, Options.Create(new CuraQuoteOptions { ExecutablePath = "unused" }),
            NullLogger<CuraQuoteSlicer>.Instance, registry, Options.Create(new ModelUploadOptions()));
        if (format == "GLB")
        {
            var envelope = await slicer.GetEnvelopeAsync("models/part", format, CancellationToken.None);
            envelope.Should().Be(new QuoteModelEnvelope(10, 20, 30));
        }
        var request = new QuoteSliceRequest("models/part", format, 1m, "PLA", ["WHITE"], 1.24m, .2m, 20, 1, Guid.NewGuid());
        var action = () => slicer.SliceAsync(request, CancellationToken.None);
        (await action.Should().ThrowAsync<InvalidDataException>()).Which.Message.Should().Contain("vượt khổ máy");
        // The GLB envelope and slicer use the same cached geometry, including legacy uploads.
        await storage.Received(1).DownloadAsync("models", "part", Arg.Any<CancellationToken>());
        var multiColor = () => slicer.SliceAsync(request with { Colors = ["WHITE", "BLUE"] }, CancellationToken.None);
        (await multiColor.Should().ThrowAsync<InvalidDataException>()).Which.Message.Should().Contain("3MF");
    }

    private static MemoryStream Obj(string faces) => new(Encoding.UTF8.GetBytes("v 0 0 0\nv 10 0 0\nv 0 20 0\nv 0 0 30\n" + faces));
    private static Vector3 Bounds(IEnumerable<QuoteMesh.Triangle> mesh)
    {
        var points = mesh.SelectMany(t => new[] { t.A, t.B, t.C }).ToList();
        return new(points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y), points.Max(p => p.Z) - points.Min(p => p.Z));
    }

    private static MemoryStream Glb(Action<JsonObject>? edit = null, bool skinned = false, float vertexScale = 1, bool invalidIndex = false)
    {
        using var binary = new MemoryStream();
        using (var writer = new BinaryWriter(binary, Encoding.UTF8, leaveOpen: true))
        {
            foreach (var p in new[] { Vector3.Zero, new(10, 0, 0), new(0, 20, 0), new(0, 0, 30) })
            { writer.Write(p.X * vertexScale); writer.Write(p.Y * vertexScale); writer.Write(p.Z * vertexScale); }
            foreach (var i in new[] { 0, 2, 1, 0, 1, 3, 1, 2, 3, 2, 0, invalidIndex ? 99 : 3 }) writer.Write((ushort)i);
            if (skinned)
            {
                for (var i = 0; i < 16; i++) writer.Write((ushort)0);
                for (var i = 0; i < 16; i++) writer.Write(i % 4 == 0 ? 1f : 0f);
                foreach (var value in new[] { 1f, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -2, -3, -4, 1 }) writer.Write(value);
            }
        }
        var root = JsonNode.Parse("""
            {"asset":{"version":"2.0"},"scene":0,"scenes":[{"nodes":[0]}],"nodes":[{"mesh":0}],
             "meshes":[{"primitives":[{"attributes":{"POSITION":0},"indices":1}]}],
             "buffers":[{"byteLength":72}],
             "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":48},{"buffer":0,"byteOffset":48,"byteLength":24}],
             "accessors":[{"bufferView":0,"componentType":5126,"count":4,"type":"VEC3","min":[0,0,0],"max":[10,20,30]},
                          {"bufferView":1,"componentType":5123,"count":12,"type":"SCALAR"}]}
            """)!.AsObject();
        root["accessors"]![0]!["max"] = new JsonArray(JsonValue.Create(10 * vertexScale), JsonValue.Create(20 * vertexScale), JsonValue.Create(30 * vertexScale));
        if (skinned)
        {
            root["buffers"]![0]!["byteLength"] = (int)binary.Length;
            var views = root["bufferViews"]!.AsArray();
            foreach (var json in new[] { "{\"buffer\":0,\"byteOffset\":72,\"byteLength\":32}", "{\"buffer\":0,\"byteOffset\":104,\"byteLength\":64}", "{\"buffer\":0,\"byteOffset\":168,\"byteLength\":64}" }) views.Add(JsonNode.Parse(json));
            var accessors = root["accessors"]!.AsArray();
            foreach (var json in new[] { "{\"bufferView\":2,\"componentType\":5123,\"count\":4,\"type\":\"VEC4\"}", "{\"bufferView\":3,\"componentType\":5126,\"count\":4,\"type\":\"VEC4\"}", "{\"bufferView\":4,\"componentType\":5126,\"count\":1,\"type\":\"MAT4\"}" }) accessors.Add(JsonNode.Parse(json));
            var attributes = root["meshes"]![0]!["primitives"]![0]!["attributes"]!;
            attributes["JOINTS_0"] = 2; attributes["WEIGHTS_0"] = 3;
            root["skins"] = JsonNode.Parse("""[{"joints":[2],"inverseBindMatrices":4,"skeleton":2}]""");
        }
        edit?.Invoke(root);
        var jsonBytes = Encoding.UTF8.GetBytes(root.ToJsonString());
        var padded = (jsonBytes.Length + 3) / 4 * 4;
        var output = new MemoryStream();
        using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(0x46546C67); writer.Write(2); writer.Write(12 + 8 + padded + 8 + (int)binary.Length);
            writer.Write(padded); writer.Write(0x4E4F534A); writer.Write(jsonBytes);
            for (var i = jsonBytes.Length; i < padded; i++) writer.Write((byte)' ');
            writer.Write((int)binary.Length); writer.Write(0x004E4942); writer.Write(binary.ToArray());
        }
        output.Position = 0;
        return output;
    }
}
