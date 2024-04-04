
using RecordInterfaces.Sample;

IUser user = new DefaultUser
{
    FirstName = "Old first name",
    LastName = "Old last name",
    MiddleName = "Old last name"
};

user = user.With(firstName: "New first name");

Console.WriteLine(user);

