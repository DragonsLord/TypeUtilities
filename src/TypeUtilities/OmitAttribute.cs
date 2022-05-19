namespace TypeUtilities;

// TODO: add interface support
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class OmitAttribute : Attribute
{
    public Type SourceType { get; set; }
    public string[] Fields { get; set; } = Array.Empty<string>();

    public bool IncludeBaseTypes { get; set; } = true;

    public OmitAttribute(Type type, params string[] fields)
    {
        SourceType = type;
        Fields = fields;
    }
}
