using PrintGrid.Infrastructure.Shared.Slicing;

namespace PrintGrid.UnitTests.Customer;

public sealed class QuoteMeshLimitTests
{
    [Fact]
    public void Binary_stl_at_the_100_MiB_triangle_boundary_is_accepted()
    {
        using var source = BinaryStl(QuoteMesh.MaxTriangles);

        var groups = QuoteMesh.Read(source, "STL");

        groups.Should().ContainSingle();
        groups[0].Count.Should().Be(2_097_150);
    }

    [Fact]
    public void Binary_stl_above_the_triangle_limit_is_rejected()
    {
        using var source = BinaryStl(QuoteMesh.MaxTriangles + 1);

        var action = () => QuoteMesh.Read(source, "STL");

        action.Should().Throw<InvalidDataException>().WithMessage("*2097150*");
    }

    private static MemoryStream BinaryStl(int triangles)
    {
        var source = new MemoryStream();
        source.SetLength(84L + 50L * triangles);
        source.Position = 80;
        using var writer = new BinaryWriter(source, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write((uint)triangles);
        source.Position = 0;
        return source;
    }
}
