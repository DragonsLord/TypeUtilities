using TypeUtilities.Abstractions;

namespace TypeUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class MapAttribute : Attribute
{
    public Type SourceType { get; }

    public string MemberDeclarationFormat { get; set; } = MemberDeclarationFormats.Source;

    public bool IncludeBaseTypes { get; set; } = false;
    public MemberAccessibilityFlags MemberAccessibilitySelection { get; set; } = MemberAccessibilityFlags.Public;
    public MemberScopeFlags MemberScopeSelection { get; set; } = MemberScopeFlags.Instance;
    public MemberKindFlags MemberKindSelection { get; set; } = MemberKindFlags.AnyProperty;

    public MapAttribute(Type type)
    {
        SourceType = type;
    }
}
