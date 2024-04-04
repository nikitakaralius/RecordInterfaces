using RecordInterfaces.Abstractions;

namespace RecordInterfaces.Sample;

[RecordInterface]
public partial interface IUser
{
    Guid Id { get; }
    string FirstName { get; }
    string LastName { get; }
    string? MiddleName { get; }
    int Age { get; }
}

[ImplementsRecordInterface]
public sealed partial record DefaultUser : IUser
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public int Age { get; init; }
}
