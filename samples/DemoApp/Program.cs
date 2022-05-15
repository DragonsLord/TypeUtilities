using DemoApp.Enums;
using TypeUtilities;

namespace DemoApp;

//Console.WriteLine("Hello World");
public static class Program
{
    public static void Main(string[] args)
    {
        //var val = new TargetType().Id;
        var props = typeof(TargetType).GetProperties().Select(p => $"{p.PropertyType.Name} {p.Name}").ToArray();
        Console.WriteLine(string.Join(", ", props));
    }
}

public class Base
{
    public CustomType BaseType { get; set; }
}

public class SourceType : Base
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }

    public CustomType SrcType => CustomType.Second;
}

[Pick(typeof(SourceType), "Id", nameof(SourceType.SrcType))]
public partial class TargetType
{
    public int MyProperty { get; set; }
    public CustomType NonTrivial { get; set; }
}