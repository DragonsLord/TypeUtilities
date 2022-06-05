namespace TypeUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class OmitAttribute : MapAttribute
{
    public string[] Fields { get; }

    public OmitAttribute(Type type, params string[] fields) : base(type)
    {
        Fields = fields;
    }
}
