using TypeUtilities.Abstractions;

namespace TypeUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class MapAttribute : Attribute
{
    public Type SourceType { get; }

    public bool IncludeBaseTypes { get; set; } = false;
    public string MemberDeclarationFormat { get; set; } = MemberDeclarationFormats.Source;

    public MapAttribute(Type type)
    {
        SourceType = type;
    }
}
