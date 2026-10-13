using System.Text.Json;
using NSubstitute;
using PrintGrid.Modules.Admin.Application.Configuration;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;

namespace PrintGrid.UnitTests.Admin;

public sealed class SharedCatalogTests
{
    [Fact]
    public void CatalogItem_NormalizesValuesAndCanBeSuperseded()
    {
        var item = SharedCatalogItem.Create(
            " Material ", " pla-plus ", 2, " PLA Plus ", "  Bền hơn  ", 145_000m,
            true, "{\"density\":1.24}", false, Guid.NewGuid());

        item.CatalogType.Should().Be(SharedCatalogTypes.Material);
        item.Code.Should().Be("PLA-PLUS");
        item.Name.Should().Be("PLA Plus");
        item.Description.Should().Be("Bền hơn");
        item.Revision.Should().Be(2);
        item.IsCurrent.Should().BeTrue();

        item.Supersede();

        item.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void Validator_RejectsUnknownTypeInvalidCodeAndNegativeRate()
    {
        var command = new SaveSharedCatalogItemCommand(
            "Unknown", "mã có khoảng trắng", "", null, -1m, false,
            JsonDocument.Parse("{}").RootElement.Clone(), false, Guid.Empty);

        var result = new SaveSharedCatalogItemValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(x => x.PropertyName).Should().Contain([
            nameof(SaveSharedCatalogItemCommand.CatalogType),
            nameof(SaveSharedCatalogItemCommand.Code),
            nameof(SaveSharedCatalogItemCommand.Name),
            nameof(SaveSharedCatalogItemCommand.UnitRate),
            nameof(SaveSharedCatalogItemCommand.ActorId)
        ]);
    }

    [Fact]
    public async Task SaveHandler_CreatesANewRevisionAndKeepsStructuredProperties()
    {
        var repository = Substitute.For<IAdminConfigurationRepository>();
        var previous = Item("BLUE", 1);
        repository.GetCurrentCatalogItemAsync(SharedCatalogTypes.Color, "BLUE", Arg.Any<CancellationToken>())
            .Returns(previous);
        var properties = JsonDocument.Parse("{\"hex\":\"#0066FF\"}").RootElement.Clone();
        var handler = new SharedCatalogHandlers(repository);

        var result = await handler.Handle(new SaveSharedCatalogItemCommand(
            "color", " blue ", "Xanh dương", "Màu tiêu chuẩn", null, false,
            properties, false, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revision.Should().Be(2);
        result.Value.Code.Should().Be("BLUE");
        result.Value.CatalogType.Should().Be(SharedCatalogTypes.Color);
        result.Value.Properties.GetProperty("hex").GetString().Should().Be("#0066FF");
        await repository.Received(1).AddCatalogRevisionAsync(
            Arg.Is<SharedCatalogItem>(x => x.Revision == 2 && x.Code == "BLUE"),
            previous,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteHandler_BlocksItemsWithRevisionHistory()
    {
        var repository = Substitute.For<IAdminConfigurationRepository>();
        repository.GetCurrentCatalogItemAsync(SharedCatalogTypes.Material, "PLA", Arg.Any<CancellationToken>())
            .Returns(Item("PLA", 2, SharedCatalogTypes.Material));
        var handler = new SharedCatalogHandlers(repository);

        var result = await handler.Handle(
            new DeleteSharedCatalogItemCommand("material", "pla"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("catalog_item_in_use");
        await repository.DidNotReceive().DeleteCatalogItemAsync(
            Arg.Any<SharedCatalogItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteHandler_BlocksAFirstRevisionThatIsAlreadyReferenced()
    {
        var repository = Substitute.For<IAdminConfigurationRepository>();
        var item = Item("PLA", 1, SharedCatalogTypes.Material);
        repository.GetCurrentCatalogItemAsync(SharedCatalogTypes.Material, "PLA", Arg.Any<CancellationToken>())
            .Returns(item);
        repository.IsCatalogItemInUseAsync(SharedCatalogTypes.Material, "PLA", Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new SharedCatalogHandlers(repository);

        var result = await handler.Handle(
            new DeleteSharedCatalogItemCommand(SharedCatalogTypes.Material, "PLA"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("catalog_item_in_use");
        await repository.DidNotReceive().DeleteCatalogItemAsync(
            Arg.Any<SharedCatalogItem>(), Arg.Any<CancellationToken>());
    }

    private static SharedCatalogItem Item(string code, int revision, string type = SharedCatalogTypes.Color) =>
        SharedCatalogItem.Create(type, code, revision, code, null, null, false, "{}", false, Guid.NewGuid());
}
