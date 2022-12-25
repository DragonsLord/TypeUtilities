﻿using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Map;

internal class MapTypeConfig
{
    public INamedTypeSymbol Source { get; }
    public INamedTypeSymbol Target { get; }

    public string MemberDeclarationFormat { get; }

    public bool IncludeBaseTypes { get; set; }
    public MemberAccessibilityFlags MemberAccessibilitySelection { get; }
    public MemberScopeFlags MemberScopeSelection { get; }
    public MemberKindFlags MemberKindSelection { get; }

    private MapTypeConfig(
        INamedTypeSymbol source,
        INamedTypeSymbol target,
        string memberDeclarationFormat,
        bool includeBaseTypes,
        MemberAccessibilityFlags memberAccessibilitySelection,
        MemberScopeFlags memberScopeSelection,
        MemberKindFlags memberKindSelection)
    {
        Source = source;
        Target = target;
        MemberDeclarationFormat = memberDeclarationFormat;
        IncludeBaseTypes = includeBaseTypes;
        MemberAccessibilitySelection = memberAccessibilitySelection;
        MemberScopeSelection = memberScopeSelection;
        MemberKindSelection = memberKindSelection;
    }

    protected MapTypeConfig(MapTypeConfig config)
    {
        Source = config.Source;
        Target = config.Target;
        MemberDeclarationFormat = config.MemberDeclarationFormat;
        IncludeBaseTypes = config.IncludeBaseTypes;
        MemberAccessibilitySelection = config.MemberAccessibilitySelection;
        MemberScopeSelection = config.MemberScopeSelection;
        MemberKindSelection = config.MemberKindSelection;
    }

    public virtual IEnumerable<ISymbol> GetMembers(SourceProductionContext context, Location attributeLocation)
    {
        return Source
            .GetExplicitMembers(IncludeBaseTypes)
            .FilterAccessibility(MemberAccessibilitySelection)
            .FilterScope(MemberScopeSelection)
            .FilterKind(MemberKindSelection);
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

        var memberDeclarationFormat = namedArgs.GetParamValue(nameof(MapAttribute.MemberDeclarationFormat), MemberDeclarationFormats.Source);

        var includeBaseTypes = namedArgs.GetParamValue(nameof(MapAttribute.IncludeBaseTypes), false);
        var memberAccessibilitySelection = namedArgs.GetEnumParam(nameof(MapAttribute.MemberAccessibilitySelection), MemberAccessibilityFlags.Public);
        var memberScopeSelection = namedArgs.GetEnumParam(nameof(MapAttribute.MemberScopeSelection), MemberScopeFlags.Instance);
        var memberKindSelection = namedArgs.GetEnumParam(nameof(MapAttribute.MemberKindSelection), MemberKindFlags.AnyProperty);

        return new MapTypeConfig(
            sourceTypeSymbol,
            targetTypeSymbol,
            memberDeclarationFormat,
            includeBaseTypes,
            memberAccessibilitySelection,
            memberScopeSelection,
            memberKindSelection);
    }
}
