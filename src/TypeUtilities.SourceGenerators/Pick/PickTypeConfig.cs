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

        if (attributeData.ConstructorArguments.Length == 0)
            return null;

        var sourceTypeSymbol = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol; // return null if null :)

        if (sourceTypeSymbol is null)
            return null;

        var fields = Array.Empty<string>();

        if (attributeData.ConstructorArguments.Length > 1)
        {
            fields = attributeData.ConstructorArguments[1].Values.Select(x => x.Value as string).Where(x => x is not null).ToArray()!;
        }

        var namedArgs = attributeData.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var includeBaseTypes = namedArgs.GetParamValue("IncludeBaseTypes", true);
        fields = namedArgs.GetParamValues("Fields", fields);

        return new PickTypeConfig(sourceTypeSymbol, targetTypeSymbol, fields, includeBaseTypes);
    }
}
