using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.DesignRequests;

public sealed class CreateDesignRequestCommandValidator : AbstractValidator<CreateDesignRequestCommand>
{
    public CreateDesignRequestCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200)
            .WithMessage("Tiêu đề thiết kế không được để trống và tối đa 200 ký tự.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000)
            .WithMessage("Mô tả thiết kế không được để trống và tối đa 4000 ký tự.");
        RuleFor(x => x.ReferenceImageKeys)
            .NotNull()
            .Must(keys => keys.Count <= 5)
            .WithMessage("Chỉ được đính kèm tối đa 5 ảnh tham chiếu.");
        RuleForEach(x => x.ReferenceImageKeys).NotEmpty().MaximumLength(512);
    }
}
