using System.Text.Json;
using NSubstitute;
using PrintGrid.Modules.Admin.Application.QcKnowledge;
using PrintGrid.Modules.Admin.Domain.Entities;
using PrintGrid.Modules.Admin.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Application.Commands.JobLifecycle;
using PrintGrid.Modules.Scheduling.Domain.Enums;

namespace PrintGrid.UnitTests.Admin;

public sealed class QcKnowledgeTests
{
    [Fact]
    public void ChecklistVersion_IsActiveUntilSuperseded()
    {
        var version = InspectionChecklistVersion.Create(
            " standard ", 2, "[]", Guid.NewGuid(), new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc));

        version.QualityGradeCode.Should().Be("STANDARD");
        version.VersionNumber.Should().Be(2);
        version.IsActive.Should().BeTrue();

        version.Supersede();

        version.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ChecklistValidator_RejectsDuplicateCodesAndDisplayOrders()
    {
        var command = new CreateInspectionChecklistVersionCommand("STANDARD", [
            Item("DIMENSION", 1), Item("dimension", 1)
        ], Guid.NewGuid());

        var result = new CreateInspectionChecklistVersionValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("Mã tiêu chí"));
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("Thứ tự"));
    }

    [Fact]
    public async Task CreateChecklist_RejectsAnUnknownOrDeprecatedQualityGrade()
    {
        var repository = Substitute.For<IQcKnowledgeRepository>();
        repository.QualityGradeExistsAsync("UNKNOWN", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new QcKnowledgeHandlers(repository);

        var result = await handler.Handle(new CreateInspectionChecklistVersionCommand(
            "unknown", [Item("SURFACE", 1)], Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("quality_grade_not_found");
        await repository.DidNotReceive().AddChecklistVersionAsync(
            Arg.Any<InspectionChecklistVersion>(), Arg.Any<InspectionChecklistVersion?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateChecklist_UsesTheNextVersionAndNormalizesItems()
    {
        var repository = Substitute.For<IQcKnowledgeRepository>();
        repository.QualityGradeExistsAsync("STANDARD", Arg.Any<CancellationToken>()).Returns(true);
        repository.GetNextChecklistVersionAsync("STANDARD", Arg.Any<CancellationToken>()).Returns(3);
        var handler = new QcKnowledgeHandlers(repository);

        var result = await handler.Handle(new CreateInspectionChecklistVersionCommand(
            "standard", [new(" surface ", " Bề mặt ", " Không rỗ ", true, 1)], Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VersionNumber.Should().Be(3);
        result.Value.Items.Single().Code.Should().Be("SURFACE");
        await repository.Received(1).AddChecklistVersionAsync(
            Arg.Is<InspectionChecklistVersion>(x => x.VersionNumber == 3),
            Arg.Is<InspectionChecklistVersion?>(x => x == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DefectUpdate_PreservesStableCodeAndIdentity()
    {
        var actor = Guid.NewGuid();
        var defect = DefectTaxonomyItem.Create(" layer_failure ", "Tách lớp", null, "Lab", false, actor);
        var id = defect.Id;

        defect.Update("Tách lớp nghiêm trọng", "Mô tả mới", "Hub", true, Guid.NewGuid());

        defect.Id.Should().Be(id);
        defect.Code.Should().Be("LAYER_FAILURE");
        defect.Name.Should().Be("Tách lớp nghiêm trọng");
        defect.DefaultFaultAttribution.Should().Be("Hub");
        defect.IsDeprecated.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDefect_BlocksACodeReferencedByInspectionHistory()
    {
        var repository = Substitute.For<IQcKnowledgeRepository>();
        repository.GetDefectAsync("WARPING", Arg.Any<CancellationToken>()).Returns(
            DefectTaxonomyItem.Create("WARPING", "Cong vênh", null, "Lab", false, Guid.NewGuid()));
        repository.IsDefectInUseAsync("WARPING", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new QcKnowledgeHandlers(repository);

        var result = await handler.Handle(new DeleteDefectTaxonomyItemCommand("warping"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("defect_in_use");
        await repository.DidNotReceive().DeleteDefectAsync(Arg.Any<DefectTaxonomyItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void InspectionValidator_RequiresChecklistVersion()
    {
        var command = new InspectJobCommand(Guid.NewGuid(), true,
            [new ChecklistItemResult("Bề mặt", "Pass", ItemCode: "SURFACE")], ["qc/photo.jpg"]);

        var result = new InspectJobCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(InspectJobCommand.ChecklistVersionId));
    }

    [Fact]
    public void FailedInspectionValidator_RequiresDefectCode()
    {
        var command = new InspectJobCommand(Guid.NewGuid(), false,
            [new ChecklistItemResult("Bề mặt", "Fail", ItemCode: "SURFACE")], ["qc/photo.jpg"],
            FaultAttribution.Lab, ChecklistVersionId: Guid.NewGuid());

        var result = new InspectJobCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(InspectJobCommand.DefectCode));
    }

    private static InspectionChecklistItemDto Item(string code, int order) =>
        new(code, "Tên tiêu chí", "Điều kiện đạt", true, order);
}
