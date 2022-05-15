using DemoApp.Enums;
using TypeUtilities;

namespace DemoApp;

public static class Program
{
    public static void Main(string[] args)
    {
        // var val = new TargetType().SrcType;
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

    public CustomType SrcType => CustomType.First;
}

[Pick(typeof(SourceType), "Id", nameof(Base.BaseType),
    IncludeBaseTypes = false)]
public partial class TargetType
{
    public double AdditionalValue { get; set; }
}