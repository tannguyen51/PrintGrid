using NSubstitute;
using PrintGrid.Modules.Customer.Application.Models;
using PrintGrid.Modules.Customer.Domain.Repositories;
using ModelsEntity = PrintGrid.Modules.Customer.Domain.Entities.Model;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Customer;

public class ModelCrudTests
{
    private readonly IModelRepository _models = Substitute.For<IModelRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public async Task Create_returns_a_dto_and_persists()
    {
        var handler = new CreateModelCommandHandler(_models, _unitOfWork);

        var result = await handler.Handle(
            new CreateModelCommand(CustomerId, "Bình hoa", "Một chiếc bình", "vase.stl", "STL", 2048, new[] { "trang-tri", "pla" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Bình hoa");
        result.Value.FileFormat.Should().Be("STL");
        result.Value.Tags.Should().Contain("pla");
        await _models.Received(1).AddAsync(Arg.Any<ModelsEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Create_normalizes_format_to_uppercase_and_deduplicates_tags()
    {
        var model = ModelsEntity.Create(CustomerId, "x", null, "a.obj", "obj", 1, new[] { "Pla", "Pla", "", "PETG" });

        model.FileFormat.Should().Be("OBJ");
        model.Tags.Should().BeEquivalentTo(new[] { "Pla", "PETG" });
    }

    [Fact]
    public async Task Update_edits_only_a_model_owned_by_the_customer()
    {
        var existing = ModelsEntity.Create(CustomerId, "Old", null, "a.stl", "stl", 1, null);
        _models.GetByIdForCustomerAsync(CustomerId, existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new UpdateModelCommandHandler(_models, _unitOfWork);

        var result = await handler.Handle(
            new UpdateModelCommand(CustomerId, existing.Id, "New name", "desc", "a.stl", "stl", 2, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New name");
        _models.Received(1).Update(existing);
    }

    [Fact]
    public async Task Update_returns_not_found_when_the_model_belongs_to_someone_else()
    {
        _models.GetByIdForCustomerAsync(CustomerId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ModelsEntity?)null);
        var handler = new UpdateModelCommandHandler(_models, _unitOfWork);

        var result = await handler.Handle(
            new UpdateModelCommand(CustomerId, Guid.NewGuid(), "x", null, "a.stl", "stl", 1, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
        _models.DidNotReceive().Update(Arg.Any<ModelsEntity>());
    }

    [Fact]
    public async Task Delete_removes_a_owned_model()
    {
        var existing = ModelsEntity.Create(CustomerId, "x", null, "a.stl", "stl", 1, null);
        _models.GetByIdForCustomerAsync(CustomerId, existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new DeleteModelCommandHandler(_models, _unitOfWork);

        var result = await handler.Handle(new DeleteModelCommand(CustomerId, existing.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _models.Received(1).Remove(existing);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_returns_not_found_for_foreign_model()
    {
        _models.GetByIdForCustomerAsync(CustomerId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ModelsEntity?)null);
        var handler = new DeleteModelCommandHandler(_models, _unitOfWork);

        var result = await handler.Handle(new DeleteModelCommand(CustomerId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetModels_applies_search_and_maps_tags()
    {
        var m1 = ModelsEntity.Create(CustomerId, "Bình hoa", "decor", "vase.stl", "stl", 1, new[] { "pla" });
        var m2 = ModelsEntity.Create(CustomerId, "Tượng", null, "fig.obj", "obj", 2, null);
        _models.GetForCustomerAsync(CustomerId, "bình", Arg.Any<CancellationToken>())
            .Returns(new[] { m1, m2 });
        var handler = new GetModelsQueryHandler(_models);

        var result = await handler.Handle(new GetModelsQuery(CustomerId, "bình"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.First().Tags.Should().Contain("pla");
    }
}