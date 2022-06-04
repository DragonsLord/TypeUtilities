﻿namespace TypeUtilities;

// TODO: add interface support
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class OmitAttribute : Attribute
{
    public Type SourceType { get; }
    public string[] Fields { get; }

    public bool IncludeBaseTypes { get; set; } = false;

    public OmitAttribute(Type type, params string[] fields)
    {
        SourceType = type;
        Fields = fields;
    }
}
