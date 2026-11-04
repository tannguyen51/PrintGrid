using NSubstitute;
using PrintGrid.Modules.Customer.Application.Models;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using ModelsEntity = PrintGrid.Modules.Customer.Domain.Entities.Model;

namespace PrintGrid.UnitTests.Customer;

public class ModelModerationTests
{
    private static ModelsEntity NewModel(string? storageKey = "printgrid-models/c/m.stl", long sizeBytes = 1024)
        => ModelsEntity.Create(
            Guid.NewGuid(), "Bình hoa", null, "vase.stl", "STL", sizeBytes,
            tags: null, storageKey: storageKey, sha256: "ABCDEF0123456789");

    [Fact]
    public void New_model_starts_pending()
    {
        NewModel().ReviewStatus.Should().Be(ModelReviewStatus.Pending);
    }

    [Fact]
    public void Approve_records_reviewer_time_and_the_reviewed_hash()
    {
        var model = NewModel();
        var reviewer = Guid.NewGuid();
        var at = new DateTime(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);

        model.Approve(reviewer, at, "ok");

        model.ReviewStatus.Should().Be(ModelReviewStatus.Approved);
        model.ReviewedBy.Should().Be(reviewer);
        model.ReviewedAtUtc.Should().Be(at);
        model.ReviewedSha256.Should().Be(model.Sha256);
        model.ReviewReason.Should().Be("ok");
    }

    [Fact]
    public void Reject_requires_a_reason()
    {
        var model = NewModel();

        var act = () => model.Reject(Guid.NewGuid(), DateTime.UtcNow, "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reject_marks_the_model_and_keeps_the_reason()
    {
        var model = NewModel();

        model.Reject(Guid.NewGuid(), DateTime.UtcNow, "Nội dung không phù hợp");

        model.ReviewStatus.Should().Be(ModelReviewStatus.Rejected);
        model.ReviewReason.Should().Be("Nội dung không phù hợp");
        model.ReviewedSha256.Should().BeNull();
    }

    [Fact]
    public void Renaming_or_retagging_does_not_reset_an_approval()
    {
        var model = NewModel();
        model.Approve(Guid.NewGuid(), DateTime.UtcNow);

        model.UpdateDetails("Tên mới", "mô tả", "vase.stl", "STL", 1024, new[] { "pla" });

        model.ReviewStatus.Should().Be(ModelReviewStatus.Approved);
    }

    [Fact]
    public void Changing_file_identity_resets_the_approval()
    {
        var model = NewModel();
        model.Approve(Guid.NewGuid(), DateTime.UtcNow);

        model.UpdateDetails("Bình hoa", null, "vase.stl", "STL", 2048, null);

        model.ReviewStatus.Should().Be(ModelReviewStatus.Pending);
        model.ReviewedBy.Should().BeNull();
    }

    [Fact]
    public async Task Approve_handler_refuses_a_model_without_a_stored_file()
    {
        var models = Substitute.For<IModelRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var model = NewModel(storageKey: null);
        models.GetByIdAsync(model.Id, Arg.Any<CancellationToken>()).Returns(model);

        var handler = new ApproveModelCommandHandler(models, unitOfWork);
        var result = await handler.Handle(
            new ApproveModelCommand(model.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("conflict");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_handler_persists_the_rejection()
    {
        var models = Substitute.For<IModelRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var model = NewModel();
        models.GetByIdAsync(model.Id, Arg.Any<CancellationToken>()).Returns(model);

        var handler = new RejectModelCommandHandler(models, unitOfWork);
        var result = await handler.Handle(
            new RejectModelCommand(model.Id, Guid.NewGuid(), "Bản quyền"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        model.ReviewStatus.Should().Be(ModelReviewStatus.Rejected);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
