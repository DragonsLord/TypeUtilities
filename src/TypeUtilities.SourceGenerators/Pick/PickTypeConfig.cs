﻿using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Pick;

internal class PickTypeConfig
{
    public INamedTypeSymbol Source { get; }
    public INamedTypeSymbol Target { get; }
    public string[] Fields { get; }
    public bool IncludeBaseTypes { get; set; }
    public string MemberDeclarationFormat { get; set; }

    private PickTypeConfig(INamedTypeSymbol source, INamedTypeSymbol target, string[] fields, bool includeBaseTypes, string memberDeclarationFormat)
    {
        Source = source;
        Target = target;
        Fields = fields;
        IncludeBaseTypes = includeBaseTypes;
        MemberDeclarationFormat = memberDeclarationFormat;
    }

    public static PickTypeConfig? Create(INamedTypeSymbol targetTypeSymbol)
    {
        var attributeData = targetTypeSymbol.GetAttributeData<PickAttribute>();

        if (attributeData is null)
            return null;

        if (attributeData.ConstructorArguments.Length == 0)
            return null;

        var sourceTypeSymbol = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol; // return null if null :)

        if (sourceTypeSymbol is null)
            return null;

        var fields = attributeData.ConstructorArguments.GetParamsArrayAt<string>(1);

        var namedArgs = attributeData.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var includeBaseTypes = namedArgs.GetParamValue(nameof(OmitAttribute.IncludeBaseTypes), false);
        var memberDeclarationFormat = namedArgs.GetParamValue(nameof(OmitAttribute.MemberDeclarationFormat), MemberDeclarationFormats.Source);

        return new PickTypeConfig(sourceTypeSymbol, targetTypeSymbol, fields, includeBaseTypes, memberDeclarationFormat);
    }
}
