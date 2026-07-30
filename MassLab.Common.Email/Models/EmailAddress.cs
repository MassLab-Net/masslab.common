namespace MassLab.Common.Email.Models;

public sealed record EmailAddress(string Address, string? DisplayName = null)
{
    public override string ToString() => string.IsNullOrWhiteSpace(DisplayName) ? Address : $"{DisplayName} <{Address}>";
}
