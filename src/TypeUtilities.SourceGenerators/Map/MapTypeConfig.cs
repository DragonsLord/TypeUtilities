using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Map;

public static class MemberSelectionExtensions
{
    public static IEnumerable<Accessibility> GetSelectedAccessibility(this MemberSelectionFlags flags)
    {
        if (flags.HasFlag(MemberSelectionFlags.Public))
            yield return Accessibility.Public;

        if (flags.HasFlag(MemberSelectionFlags.Private))
            yield return Accessibility.Private;

        if (flags.HasFlag(MemberSelectionFlags.Protected))
            yield return Accessibility.Protected;
    }

    public static Func<ISymbol, bool> GetPropertyFilter(this MemberSelectionFlags flags)
    {
        if (flags.HasFlag(MemberSelectionFlags.GetProperty | MemberSelectionFlags.SetProperty))
        {
            return x => x is IPropertySymbol;
        }

        if (flags.HasFlag(MemberSelectionFlags.GetProperty))
        {
            return x => x is IPropertySymbol propertySymbol && propertySymbol.GetMethod is not null;
        }

        if (flags.HasFlag(MemberSelectionFlags.SetProperty))
        {
            return x => x is IPropertySymbol propertySymbol && propertySymbol.SetMethod is not null;
        }

        if (flags.HasFlag(MemberSelectionFlags.GetSetProperty))
        {
            return x => x is IPropertySymbol propertySymbol && propertySymbol.GetMethod is not null && propertySymbol.SetMethod is not null;
        }

        return x => false;
    }

    public static Func<ISymbol, bool> GetFieldFilter(this MemberSelectionFlags flags)
    {
        if (flags.HasFlag(MemberSelectionFlags.WritableField))
        {
            return x => x is IFieldSymbol;
        }

        if (flags.HasFlag(MemberSelectionFlags.ReadonlyField))
        {
            return x => x is IFieldSymbol field && field.IsReadOnly;
        }

        return x => false;
    }
}

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
        var selection = MemberSelection is MemberSelectionFlags.Default ? defaultSelection : MemberSelection;

        var selectedMembers = Enumerable.Empty<ISymbol>();

        if (selection.HasFlag(MemberSelectionFlags.Declared))
        {
            selectedMembers = Source.GetExplicitMembers();
        }

        if (selection.HasFlag(MemberSelectionFlags.Inherited))
        {
            selectedMembers = selectedMembers.Concat(Source.GetBaseTypes().SelectMany(t => t.GetExplicitMembers()));
        }

        if (!selection.HasFlag(MemberSelectionFlags.AnyAccessibility))
        {
            var accessibility = MemberSelection.GetSelectedAccessibility().ToList();
            selectedMembers = selectedMembers.Where(x => accessibility.Contains(x.DeclaredAccessibility));
        }

        if (!selection.HasFlag(MemberSelectionFlags.AnyScope))
        {
            // Because we have only on of 2 options here, it's enough to check for static
            // Side Effect: will return instance props when both Static and Instance flags are missing
            var isStatic = MemberSelection.HasFlag(MemberSelectionFlags.Static);
            selectedMembers = selectedMembers.Where(x => x.IsStatic == isStatic);
        }

        var propertyFilter = selection.GetPropertyFilter();
        var fieldFilter = selection.GetFieldFilter();

        return selectedMembers
            .Where(m => propertyFilter(m) || fieldFilter(m));
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
