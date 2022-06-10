using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Map;

public static class MemberSelectionExtensions
{
    public static IEnumerable<ISymbol> FilterAccessibility(this IEnumerable<ISymbol> members, MemberAccessibilityFlags accessibilityFlags)
    {
        if (accessibilityFlags.HasFlag(MemberAccessibilityFlags.Any))
            return members;

        var accessibilities = ListSelectedAccessibility().ToList();
        return members.Where(x => accessibilities.Contains(x.DeclaredAccessibility));

        IEnumerable<Accessibility> ListSelectedAccessibility()
        {
            if (accessibilityFlags.HasFlag(MemberAccessibilityFlags.Public))
                yield return Accessibility.Public;

            if (accessibilityFlags.HasFlag(MemberAccessibilityFlags.Private))
                yield return Accessibility.Private;

            if (accessibilityFlags.HasFlag(MemberAccessibilityFlags.Protected))
                yield return Accessibility.Protected;
        }
    }

    public static IEnumerable<ISymbol> FilterScope(this IEnumerable<ISymbol> members, MemberScopeFlags scopeFlags)
    {
        if (scopeFlags.HasFlag(MemberScopeFlags.Any))
            return members;

        // Because we have only on of 2 options here, it's enough to check for static
        // Side Effect: will return instance props when both Static and Instance flags are missing
        var isStatic = scopeFlags.HasFlag(MemberScopeFlags.Static);
        return members.Where(x => x.IsStatic == isStatic);
    }

    public static IEnumerable<ISymbol> FilterKind(this IEnumerable<ISymbol> members, MemberKindFlags scopeFlags)
    {
        var filterProperty = GetPropertyFilter();
        var filterField = GetFieldFilter();
        return members.Where(x => filterProperty(x) || filterField(x));

        Func<ISymbol, bool> GetPropertyFilter()
        {
            if (scopeFlags.HasFlag(MemberKindFlags.AnyProperty))
                return x => x is IPropertySymbol;

            if (scopeFlags.HasFlag(MemberKindFlags.GetProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.GetMethod is not null;

            if (scopeFlags.HasFlag(MemberKindFlags.SetProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.SetMethod is not null;

            if (scopeFlags.HasFlag(MemberKindFlags.ReadonlyProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.GetMethod is not null && propertySymbol.SetMethod is null;

            if (scopeFlags.HasFlag(MemberKindFlags.WriteonlyProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.SetMethod is not null && propertySymbol.GetMethod is null;

            if (scopeFlags.HasFlag(MemberKindFlags.GetSetProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.GetMethod is not null && propertySymbol.SetMethod is not null;

            return x => false;
        }

        Func<ISymbol, bool> GetFieldFilter()
        {
            if (scopeFlags.HasFlag(MemberKindFlags.WritableField))
            {
                return x => x is IFieldSymbol;
            }

            if (scopeFlags.HasFlag(MemberKindFlags.ReadonlyField))
            {
                return x => x is IFieldSymbol field && field.IsReadOnly;
            }

            return x => false;
        }
    }
}

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

    public virtual IEnumerable<ISymbol> GetMembers()
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
