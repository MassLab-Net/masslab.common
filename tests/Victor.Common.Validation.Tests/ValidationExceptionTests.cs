using FluentValidation.Results;
using Victor.Common.Validation.Exceptions;

namespace Victor.Common.Validation.Tests;

public class ValidationExceptionTests
{
    [Fact]
    public void Constructor_groups_errors_by_property()
    {
        var exception = new ValidationException(
        [
            new ValidationFailure("Name", "required"),
            new ValidationFailure("Name", "too short"),
            new ValidationFailure("Price", "positive")
        ]);

        exception.Errors["Name"].Should().BeEquivalentTo("required", "too short");
        exception.Errors["Price"].Should().ContainSingle().Which.Should().Be("positive");
    }
}
