using Microsoft.CodeAnalysis;
using RecordInterfaces.Abstractions;

namespace RecordInterfaces.Sample;

[RecordInterface]
public partial interface IUser
{
    string FirstName { get; }
    string LastName { get; }
    string? MiddleName { get; }
}

[ImplementsRecordInterface]
public sealed partial record DefaultUser : IUser
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }
}
