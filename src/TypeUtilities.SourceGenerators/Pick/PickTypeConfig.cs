﻿using Microsoft.CodeAnalysis;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Pick;

internal class PickTypeConfig
{
    public INamedTypeSymbol Source { get; }
    public INamedTypeSymbol Target { get; }
    public string[] Fields { get; }
    public bool IncludeBaseTypes { get; set; }

    private PickTypeConfig(INamedTypeSymbol source, INamedTypeSymbol target, string[] fields, bool includeBaseTypes)
    {
        Source = source;
        Target = target;
        Fields = fields;
        IncludeBaseTypes = includeBaseTypes;
    }

    public static PickTypeConfig? Create(INamedTypeSymbol targetTypeSymbol)
    {
        // TODO: reference actual attribute?
        var attributeData = targetTypeSymbol.GetAttributeData("TypeUtilities.PickAttribute");

        if (attributeData is null)
            return null;

        // TODO: move to a separate step?
        if (attributeData.ConstructorArguments.Length == 0)
            return null;

        var sourceTypeSymbol = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol; // return null if null :)

        if (sourceTypeSymbol is null)
            return null;

        var fields = attributeData.ConstructorArguments.GetParamsArrayAt<string>(1);

        var namedArgs = attributeData.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var includeBaseTypes = namedArgs.GetParamValue("IncludeBaseTypes", true);
        fields = namedArgs.GetParamValues("Fields", fields);

        return new PickTypeConfig(sourceTypeSymbol, targetTypeSymbol, fields, includeBaseTypes);
    }
}
