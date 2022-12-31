using DemoApp.Enums;
using DemoApp.MapTemplateDemo;
using TypeUtilities;
using TypeUtilities.Abstractions;

using static TypeUtilities.Abstractions.MemberDeclarationFormats;

namespace DemoApp;

public static class Program
{
    public static void Main(string[] args)
    {
        Base source = new Base { BaseType = CustomType.First };
        var fromTemplate = MapTemplate.Map(source);
        var val = new OmittedType();
        var props = typeof(BasicallyMap).GetMembers().Select(p => $"{p.DeclaringType?.Name} {p.Name}").ToArray();
        Console.WriteLine(string.Join(", ", props));
    }
}

public record Rec(string Name);

public class Base
{
    public CustomType BaseType { get; set; }
}

public class SourceType : Base
{
    public Guid Id { get; init; }
    public int Value { get; private set; }
    public DateTime Created { get; set; }

    public static CustomType SrcType => CustomType.First;

    public readonly int idField = 1;

    public int anotherField = 1;
}

[Pick(typeof(SourceType), "Id", nameof(SourceType.BaseType), IncludeBaseTypes = true)]
public partial class TargetType
{
    public double AdditionalValue { get; set; }
}

[Omit(typeof(SourceType), "Value", MemberDeclarationFormat = PublicGetSetProp)]
public partial class OmittedType
{
    public int MyProperty { get; set; }
}

[Map(typeof(SourceType),
    MemberDeclarationFormat = $"{Tokens.Accessibility} string Mapped{Tokens.Name}{Tokens.Accessors}",
    MemberScopeSelection = MemberScopeFlags.Any,
    MemberKindSelection = MemberKindFlags.ReadonlyProperty | MemberKindFlags.WritableField)]
public partial class BasicallyMap
{
}


[Map(typeof(SourceType))]
public partial class SimpleMap
{
}

[Map(typeof(SourceType),
    MemberDeclarationFormat = $"{Tokens.Accessibility} string Mapped{Tokens.Name}{Tokens.Accessors}",
    MemberKindSelection = MemberKindFlags.ReadonlyProperty)]
public partial class AdvancedMap
{
}