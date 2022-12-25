using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;

namespace TypeUtilities.SourceGenerators.Helpers;

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

    public static IEnumerable<ISymbol> FilterKind(this IEnumerable<ISymbol> members, MemberKindFlags kindFlags)
    {
        var filterProperty = GetPropertyFilter();
        var filterField = GetFieldFilter();
        return members.Where(x => filterProperty(x) || filterField(x));

        Func<ISymbol, bool> GetPropertyFilter()
        {
            if (kindFlags.HasFlag(MemberKindFlags.AnyProperty))
                return x => x is IPropertySymbol;

            if (kindFlags.HasFlag(MemberKindFlags.GetProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.GetMethod is not null;

            if (kindFlags.HasFlag(MemberKindFlags.SetProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.SetMethod is not null;

            if (kindFlags.HasFlag(MemberKindFlags.ReadonlyProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.GetMethod is not null && propertySymbol.SetMethod is null;

            if (kindFlags.HasFlag(MemberKindFlags.WriteonlyProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.SetMethod is not null && propertySymbol.GetMethod is null;

            if (kindFlags.HasFlag(MemberKindFlags.GetSetProperty))
                return x => x is IPropertySymbol propertySymbol && propertySymbol.GetMethod is not null && propertySymbol.SetMethod is not null;

            return x => false;
        }

        Func<ISymbol, bool> GetFieldFilter()
        {
            if (kindFlags.HasFlag(MemberKindFlags.AnyField))
                return x => x is IFieldSymbol;

            if (kindFlags.HasFlag(MemberKindFlags.WritableField))
                return x => x is IFieldSymbol field && !field.IsReadOnly;

            if (kindFlags.HasFlag(MemberKindFlags.ReadonlyField))
                return x => x is IFieldSymbol field && field.IsReadOnly;

            return x => false;
        }
    }
}
