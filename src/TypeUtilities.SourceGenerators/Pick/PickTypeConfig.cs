using Microsoft.CodeAnalysis;

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

    public static PickTypeConfig? Create(INamedTypeSymbol attributeSymbol, INamedTypeSymbol targetTypeSymbol)
    {
         var attributeData = targetTypeSymbol
            .GetAttributes()
            .FirstOrDefault(x =>
                x.AttributeClass is not null &&
                x.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));

        if (attributeData is null)
            return null;

        // TODO: move to a separate step?
        if (attributeData.ConstructorArguments.Length == 0)
            return null;

        var sourceTypeSymbol = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol; // return null if null :)

        if (sourceTypeSymbol is null)
            return null;

        var fields = Array.Empty<string>();

        if (attributeData.ConstructorArguments.Length > 1)
        {
            if (attributeData.ConstructorArguments[1].Type?.TypeKind == TypeKind.Array)
            {
                fields = attributeData.ConstructorArguments[1].Values.Select(x => x.Value as string).Where(x => x is not null).ToArray()!;
            }
            else
            {
                fields = attributeData.ConstructorArguments
                            .Skip(1)
                            .Where(arg => !arg.IsNull && arg.Type?.TypeKind == TypeKind.Class)
                            .Select(arg => arg.Value as string)
                            .Where(arg => arg is not null)
                            .ToArray()!;
            }
        }

        var namedArgs = attributeData.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var includeBaseTypes = namedArgs.GetParamValue("IncludeBaseTypes", true);
        fields = namedArgs.GetParamValues("Fields", fields);

        return new PickTypeConfig(sourceTypeSymbol, targetTypeSymbol, fields, includeBaseTypes);
    }

    #region Comparer
    public static IEqualityComparer<PickTypeConfig> Comparer = new EqualityComparer();

    private class EqualityComparer : IEqualityComparer<PickTypeConfig>
    {
        private static SymbolEqualityComparer symbolComparer = SymbolEqualityComparer.Default;

        public bool Equals(PickTypeConfig x, PickTypeConfig y)
        {
            return  symbolComparer.Equals(x.Source, y.Source) &&
                    symbolComparer.Equals(x.Target, y.Target) &&
                    x.IncludeBaseTypes == y.IncludeBaseTypes  &&
                    IsFieldsEquals(x.Fields, y.Fields);
        }

        public int GetHashCode(PickTypeConfig obj)
        {
            return  symbolComparer.GetHashCode(obj.Source) ^
                    symbolComparer.GetHashCode(obj.Target) ^
                    obj.IncludeBaseTypes.GetHashCode()     ^
                    obj.Fields.GetHashCode();
        }

        private bool IsFieldsEquals(string[] x, string[] y)
        {
            return
                (x == y) ||
                (
                    x.Length == y.Length &&
                    Enumerable.Range(0, x.Length).All(i => x[i] == y[i])
                );
        }
    }
    #endregion
}
