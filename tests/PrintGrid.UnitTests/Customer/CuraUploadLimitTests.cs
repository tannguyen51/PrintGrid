using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.Modules.Customer.Infrastructure.Slicing;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Customer;

public sealed class CuraUploadLimitTests
{
    [Theory]
    [InlineData(51 * 1024 * 1024, 100 * 1024 * 1024, false)]
    [InlineData(100 * 1024 * 1024, 100 * 1024 * 1024, false)]
    [InlineData(100 * 1024 * 1024 + 1, 100 * 1024 * 1024, true)]
    [InlineData(1024, 1024, false)]
    [InlineData(1025, 1024, true)]
    public async Task Slicer_enforces_the_shared_upload_limit_before_mesh_processing(int bytes, int limit, bool rejected)
    {
        // A valid ASCII triangle padded with whitespace, larger than the tiny build plate.
        // Reaching build-plate validation proves the size gate accepted the whole file,
        // without invoking an external Cura executable.
        var data = new byte[bytes];
        Array.Fill(data, (byte)'\n');
        Encoding.ASCII.GetBytes("solid test\nfacet normal 0 0 1\nouter loop\nvertex 0 0 0\nvertex 20 0 0\nvertex 0 20 0\nendloop\nendfacet\nendsolid test\n").CopyTo(data, 0);
        await using var source = new MemoryStream(data, writable: false);
        var storage = Substitute.For<IFileStorage>();
        storage.DownloadAsync("models", "test.stl", Arg.Any<CancellationToken>()).Returns(source);
        var registry = Substitute.For<ICuraProfileRegistry>();
        registry.ResolveAsync(Arg.Any<Guid>(), "PLA", Arg.Any<CancellationToken>(), null)
            .Returns(new RegisteredCuraSettings("""{"global":{"machine_width":1,"machine_depth":1,"machine_height":1}}""", 1, 100m, 0m));
        var slicer = new CuraQuoteSlicer(storage,
            Options.Create(new CuraQuoteOptions { ExecutablePath = "unused" }),
            NullLogger<CuraQuoteSlicer>.Instance, registry,
            Options.Create(new ModelUploadOptions { MaxFileBytes = limit }));

        var action = () => slicer.SliceAsync(new QuoteSliceRequest("models/test.stl", "STL", 1m,
            "PLA", ["WHITE"], 1.24m, 0.2m, 20, 1, Guid.NewGuid()), CancellationToken.None);

        var error = await action.Should().ThrowAsync<InvalidDataException>();
        error.Which.Message.Should().Contain(rejected ? "File vượt giới hạn" : "vượt khổ máy");
    }
}
