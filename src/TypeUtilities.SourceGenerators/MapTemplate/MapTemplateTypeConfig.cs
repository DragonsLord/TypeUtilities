using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Models;

namespace TypeUtilities.SourceGenerators.MapTemplate;

internal class MapTemplateConfig
{
    public string TemplateName { get; }

    public string MemberDeclarationFormat { get; }

    public bool IncludeBaseTypes { get; set; }
    public MemberAccessibilityFlags MemberAccessibilitySelection { get; }
    public MemberScopeFlags MemberScopeSelection { get; }
    public MemberKindFlags MemberKindSelection { get; }

    private MapTemplateConfig(
        string templateName,
        string memberDeclarationFormat,
        bool includeBaseTypes,
        MemberAccessibilityFlags memberAccessibilitySelection,
        MemberScopeFlags memberScopeSelection,
        MemberKindFlags memberKindSelection)
    {
        TemplateName = templateName;
        MemberDeclarationFormat = memberDeclarationFormat;
        IncludeBaseTypes = includeBaseTypes;
        MemberAccessibilitySelection = memberAccessibilitySelection;
        MemberScopeSelection = memberScopeSelection;
        MemberKindSelection = memberKindSelection;
    }

    public IEnumerable<ISymbol> GetMembers(ITypeSymbol sourceType)
    {
        return sourceType
            .GetExplicitMembers(IncludeBaseTypes)
            .FilterAccessibility(MemberAccessibilitySelection)
            .FilterScope(MemberScopeSelection)
            .FilterKind(MemberKindSelection);
    }

    public static ISyntaxResult<MapTemplateConfig> Create(INamedTypeSymbol templateTypeSymbol)
    {
        var attributeData = templateTypeSymbol.GetAttributeData<MapTemplateAttribute>();

        if (attributeData is null)
            // TODO: add diagnostic
            return SyntaxResult.Skip<MapTemplateConfig>();

        return SyntaxResult.Ok(Create(templateTypeSymbol.Name, attributeData));
    }

    public static MapTemplateConfig Create(string templateName, AttributeData attributeData)
    {
        var namedArgs = attributeData.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var memberDeclarationFormat = namedArgs.GetParamValue(nameof(MapAttribute.MemberDeclarationFormat), MemberDeclarationFormats.Source);

        var includeBaseTypes = namedArgs.GetParamValue(nameof(MapAttribute.IncludeBaseTypes), false);
        var memberAccessibilitySelection = namedArgs.GetEnumParam(nameof(MapAttribute.MemberAccessibilitySelection), MemberAccessibilityFlags.Public);
        var memberScopeSelection = namedArgs.GetEnumParam(nameof(MapAttribute.MemberScopeSelection), MemberScopeFlags.Instance);
        var memberKindSelection = namedArgs.GetEnumParam(nameof(MapAttribute.MemberKindSelection), MemberKindFlags.AnyProperty);

        return new MapTemplateConfig(
            templateName,
            memberDeclarationFormat,
            includeBaseTypes,
            memberAccessibilitySelection,
            memberScopeSelection,
            memberKindSelection);
    }
}
