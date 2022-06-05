using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Map;

internal class MapTypeConfig
{
    public INamedTypeSymbol Source { get; }
    public INamedTypeSymbol Target { get; }
    public bool IncludeBaseTypes { get; set; } //TODO: remove in favior of member selection
    public string MemberDeclarationFormat { get; set; }
    public MemberSelectionFlags MemberSelection { get; set; }

    private MapTypeConfig(INamedTypeSymbol source, INamedTypeSymbol target, bool includeBaseTypes, string memberDeclarationFormat, MemberSelectionFlags memberSelectionFlags)
    {
        Source = source;
        Target = target;
        IncludeBaseTypes = includeBaseTypes;
        MemberDeclarationFormat = memberDeclarationFormat;
        MemberSelection = memberSelectionFlags;
    }

    protected MapTypeConfig(MapTypeConfig config)
    {
        Source = config.Source;
        Target = config.Target;
        IncludeBaseTypes = config.IncludeBaseTypes;
        MemberDeclarationFormat = config.MemberDeclarationFormat;
        MemberSelection = config.MemberSelection;
    }

    public virtual IEnumerable<ISymbol> GetMembers()
        => GetMembers(MemberSelections.DeclaredInstanceProperties);

    public IEnumerable<ISymbol> GetMembers(MemberSelectionFlags defaultSelection)
    {
        // TODO: apply selection
        return Source
            .GetExplicitMembers(IncludeBaseTypes)
            .Where(m => m is IPropertySymbol || m is IFieldSymbol);
    }

    public static MapTypeConfig? Create(INamedTypeSymbol targetTypeSymbol)
    {
        var attributeData = targetTypeSymbol.GetAttributeData<MapAttribute>();

        if (attributeData is null)
            return null;

        return Create(targetTypeSymbol, attributeData);
    }

    public static MapTypeConfig? Create(INamedTypeSymbol targetTypeSymbol, AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length == 0)
            return null;

        if (attributeData.ConstructorArguments[0].Value is not INamedTypeSymbol sourceTypeSymbol)
            return null;

        var namedArgs = attributeData.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var includeBaseTypes = namedArgs.GetParamValue(nameof(MapAttribute.IncludeBaseTypes), false);
        var memberDeclarationFormat = namedArgs.GetParamValue(nameof(MapAttribute.MemberDeclarationFormat), MemberDeclarationFormats.Source);
        var memberSelectionFlags = namedArgs.GetParamValue(nameof(MapAttribute.MemberSelection), MemberSelectionFlags.Default);

        return new MapTypeConfig(sourceTypeSymbol, targetTypeSymbol, includeBaseTypes, memberDeclarationFormat, memberSelectionFlags);
    }
}
