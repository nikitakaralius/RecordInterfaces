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
