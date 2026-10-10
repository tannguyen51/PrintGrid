using FluentValidation;

namespace PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;

public class CreateReprintRequestCommandValidator : AbstractValidator<CreateReprintRequestCommand>
{
    public static readonly string[] AllowedReasons =
        ["dimensional_inaccuracy", "surface_defect", "wrong_material_or_color", "damaged_in_transit", "missing_parts", "other"];

    public CreateReprintRequestCommandValidator()
    {
        RuleFor(x => x.Reason).Must(reason => AllowedReasons.Contains(reason?.Trim().ToLowerInvariant()))
            .WithMessage("Reason is not supported.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Photos).NotEmpty().Must(x => x.Count <= 5)
            .WithMessage("Provide between 1 and 5 photos.");
        RuleForEach(x => x.Photos).NotEmpty().Must(IsSupportedPhoto)
            .WithMessage("Photos must be JPEG, PNG or WebP data URLs and no larger than 5 MB each.");
    }

    private static bool IsSupportedPhoto(string value)
    {
        var prefixes = new[] { "data:image/jpeg;base64,", "data:image/png;base64,", "data:image/webp;base64," };
        var prefix = prefixes.FirstOrDefault(value.StartsWith);
        if (prefix is null) return false;
        var payload = value[prefix.Length..];
        if (payload.Length > 7_000_000) return false;
        try { return Convert.FromBase64String(payload).Length <= 5 * 1024 * 1024; }
        catch (FormatException) { return false; }
    }
}
