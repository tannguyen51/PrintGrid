namespace PrintGrid.SharedKernel.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string entity, object id) =>
        new("not_found", $"{entity} with id '{id}' was not found");

    public static Error Validation(string message) => new("validation_error", message);

    public static Error Conflict(string message) => new("conflict", message);

    public static Error Forbidden(string message) => new("forbidden", message);

    public override string ToString() => $"{Code}: {Message}";
}
