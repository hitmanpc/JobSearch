using Microsoft.Extensions.Options;

namespace JobSearch.Application.Services;

public sealed class RemotiveJobImportOptions
{
    public string? RemotiveCategory { get; init; }

    public string? RemotiveSearchText { get; init; }

    public int? RemotiveLimit { get; init; }

    public void Validate()
    {
        if (RemotiveLimit is <= 0)
        {
            throw new InvalidOperationException("JobImport:RemotiveLimit must be a positive integer when configured.");
        }
    }
}

public sealed class RemotiveJobImportOptionsValidator : IValidateOptions<RemotiveJobImportOptions>
{
    public ValidateOptionsResult Validate(string? name, RemotiveJobImportOptions options)
    {
        try
        {
            options.Validate();
        }
        catch (InvalidOperationException exception)
        {
            return ValidateOptionsResult.Fail(exception.Message);
        }

        return ValidateOptionsResult.Success;
    }
}
