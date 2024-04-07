
using System.Runtime.CompilerServices;
using RecordInterfaces.Sample;

// IUser user = new DefaultUser
// {
//     FirstName = "Old first name",
//     LastName = "Old last name",
//     MiddleName = "Old last name"
// };
//
// user = user.With(
//     firstName: "New first name",
//     lastName: "New last name");
//
// Console.WriteLine(user);

IRecord test = new Record1 {Value = 100};
var result = test.With(value: 200);
Console.WriteLine(result);

public interface IRecord
{
    int Value { get; }
}

public record Record1 : IRecord
{
    public int Value { get; init; }
}

public record Record2 : IRecord
{
    public int Value { get; init; }
}

public static class RecordInterfaceExtensions
{
    public static IRecord With(this IRecord @this, int value)
    {
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "With")]
        static extern IRecord InvokeRecord1With(
            Record1Extensions extensions,
            IRecord @this,
            int value);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "With")]
        static extern IRecord InvokeRecord2With(
            Record2Extensions extensions,
            IRecord @this,
            int value);

        return @this switch
        {
            Record1 => InvokeRecord1With(null, @this, value),
            Record2 => InvokeRecord2With(null, @this, value),
            _      => throw new NotImplementedException()
        };
    }
}

file class Record1Extensions
{
    public static IRecord With(IRecord record, int value)
    {
        var @this = (Record1) record;

        return @this with
        {
            Value = value
        };
    }
}

file class Record2Extensions
{
    public static IRecord With(IRecord record, int value)
    {
        var @this = (Record2) record;

        return @this with
        {
            Value = value
        };
    }
}

