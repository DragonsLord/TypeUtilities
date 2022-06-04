namespace TypeUtilities;

// TODO: add interface support
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class PickAttribute : Attribute
{
    public Type SourceType { get; }
    public string[] Fields { get; }

    public bool IncludeBaseTypes { get; set; } = false;

    public PickAttribute(Type type, params string[] fields)
    {
        SourceType = type;
        Fields = fields;
    }
}
