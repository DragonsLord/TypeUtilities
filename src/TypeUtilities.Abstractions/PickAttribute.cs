namespace TypeUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class PickAttribute : MapAttribute
{
    public string[] Fields { get; }

    public PickAttribute(Type type, params string[] fields) : base(type)
    {
        Fields = fields;
    }
}
