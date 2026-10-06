using PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;

namespace PrintGrid.UnitTests.Customer;

public class ReprintRequestTests
{
    private const string TinyPng = "data:image/png;base64,iVBORw0KGgo=";

    [Fact]
    public void New_request_starts_under_review_and_preserves_evidence()
    {
        var request = ReprintRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "surface_defect", "Rỗ mặt in", [TinyPng]);

        request.Status.Should().Be(ReprintRequestStatus.UnderReview);
        request.Photos.Should().ContainSingle(TinyPng);
        request.Description.Should().Be("Rỗ mặt in");
    }

    [Fact]
    public void Validator_requires_supported_reason_description_and_photo()
    {
        var validator = new CreateReprintRequestCommandValidator();
        var invalid = new CreateReprintRequestCommand(Guid.NewGuid(), Guid.NewGuid(), "unknown", "", []);
        var valid = invalid with { Reason = "surface_defect", Description = "Rỗ mặt in", Photos = [TinyPng] };

        validator.Validate(invalid).IsValid.Should().BeFalse();
        validator.Validate(valid).IsValid.Should().BeTrue();
    }
}
