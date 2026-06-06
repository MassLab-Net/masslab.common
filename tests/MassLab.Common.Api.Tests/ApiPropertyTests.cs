using MassLab.Common.Api.Models;

namespace MassLab.Common.Api.Tests;

public class ApiPropertyTests
{
    [Property(MaxTest = 100)]
    public bool Result_success_preserves_value(FsCheck.NonNull<string> value)
    {
        // Feature: dotnet-clean-architecture-api, Property: Result<T> success preserves payload value.
        var result = Result<string>.Success(value.Get);

        return result.IsSuccess && result.Value == value.Get && result.Error == string.Empty;
    }
}
