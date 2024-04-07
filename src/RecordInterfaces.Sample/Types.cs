using RecordInterfaces.Abstractions;

namespace RecordInterfaces.Sample;

[ExtendedRecordInterface]
public interface IUser
{
    string FirstName { get; }

    string? LastName { get; }
}

[ExtendedRecordInterfaceImplementation]
public sealed record DefaultUser : IUser
{
    public string FirstName { get; init; } = string.Empty;
    public string? LastName { get; init; }
}
