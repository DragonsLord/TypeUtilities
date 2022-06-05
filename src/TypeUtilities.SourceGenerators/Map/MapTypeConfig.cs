using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Pick;

internal class MapTypeConfig
{
    public INamedTypeSymbol Source { get; }
    public INamedTypeSymbol Target { get; }
    public bool IncludeBaseTypes { get; set; }
    public string MemberDeclarationFormat { get; set; }

    private MapTypeConfig(INamedTypeSymbol source, INamedTypeSymbol target, bool includeBaseTypes, string memberDeclarationFormat)
    {
        Source = source;
        Target = target;
        IncludeBaseTypes = includeBaseTypes;
        MemberDeclarationFormat = memberDeclarationFormat;
    }

    public static MapTypeConfig? Create(INamedTypeSymbol targetTypeSymbol)
    {
        var attributeData = targetTypeSymbol.GetAttributeData<MapAttribute>();

        if (attributeData is null)
            return null;

        if (attributeData.ConstructorArguments.Length == 0)
            return null;

        var sourceTypeSymbol = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol; // return null if null :)

        if (sourceTypeSymbol is null)
            return null;

        var namedArgs = attributeData.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var includeBaseTypes = namedArgs.GetParamValue(nameof(MapAttribute.IncludeBaseTypes), false);
        var memberDeclarationFormat = namedArgs.GetParamValue(nameof(MapAttribute.MemberDeclarationFormat), MemberDeclarationFormats.Source);

        return new MapTypeConfig(sourceTypeSymbol, targetTypeSymbol, includeBaseTypes, memberDeclarationFormat);
    }
}
