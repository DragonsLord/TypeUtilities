using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeUtilities.Pick;

internal class PickTypeMapping
{
    public INamedTypeSymbol Source { get; set; }
    public INamedTypeSymbol Target { get; set; }
    public string[] Fields { get; set; }

    public static IEqualityComparer<PickTypeMapping> Comparer = new EqualityComparer();

    private class EqualityComparer : IEqualityComparer<PickTypeMapping>
    {
        private static SymbolEqualityComparer symbolComparer = SymbolEqualityComparer.Default;

        public bool Equals(PickTypeMapping x, PickTypeMapping y)
        {
            return  symbolComparer.Equals(x.Source, y.Source) &&
                    symbolComparer.Equals(x.Target, y.Target) &&
                    IsFieldsEquals(x.Fields, y.Fields);
        }

        public int GetHashCode(PickTypeMapping obj)
        {
            return  symbolComparer.GetHashCode(obj.Source) ^
                    symbolComparer.GetHashCode(obj.Target) ^
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
}

[Generator]
internal class PickSourceGenerator : IIncrementalGenerator
{
    // TODO: move to separate non analyzer package
    private string _attributeSrc =
@"using System;

namespace TypeUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class PickAttribute : Attribute
{
    public Type SourceType { get; set; } 
    public string[] Fields { get; set; } = Array.Empty<string>();

    public bool IncludeBaseClass { get; set; } = true;

    public PickAttribute(Type type, params string[] fields) : this(type)
    {
        Fields = fields;
    }

    public PickAttribute(Type type)
    { 
        SourceType = type;
    }
}
";

    private static Regex attributeNameRegex = new Regex("^(TypeUtilities)?Pick(Attribute)?$");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif

        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "PickAttribute.g.cs",
            SourceText.From(_attributeSrc, Encoding.Unicode)));

        var typesDict = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node)
            .Collect()
            .Select((types, ct) => types.ToDictionary(x => x.GetFullTypeName(ct)));

        var attributes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is AttributeSyntax attr && attributeNameRegex.IsMatch(attr.Name.ToString()),
                transform: static (ctx, ct) =>
                {
                    var attributeSyntax = (AttributeSyntax)ctx.Node;
                    var attributeSymbolInfo = ctx.SemanticModel.GetSymbolInfo(attributeSyntax, ct);

                    if (attributeSymbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                        return null;

                    var attributeFullName = attributeSymbol.ContainingType.ToDisplayString();

                    if (attributeFullName != "TypeUtilities.PickAttribute")
                        return null;

                    if (attributeSyntax.Parent is not AttributeListSyntax attrList ||
                        attrList.Parent is not TypeDeclarationSyntax targetTypeSyntax)
                        return null;

                    var targetTypeSymbolInfo = ctx.SemanticModel.GetDeclaredSymbol(targetTypeSyntax, ct);

                    if (targetTypeSymbolInfo is not INamedTypeSymbol targetTypeSymbol)
                        return null;

                    var attributeData = targetTypeSymbol
                        .GetAttributes()
                        .FirstOrDefault(x =>
                            x.AttributeClass is not null &&
                            x.AttributeClass.Equals(attributeSymbol.ContainingType, SymbolEqualityComparer.Default));

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
                        fields = attributeData.ConstructorArguments[1].Values.Select(x => x.Value as string).Where(x => x is not null).ToArray()!;
                    }

                    return new PickTypeMapping
                    {
                        Target = targetTypeSymbol,
                        Source = sourceTypeSymbol,
                        Fields = fields
                    };
                })
            .SkipNulls()
            .WithComparer(PickTypeMapping.Comparer)
            .Combine(typesDict);

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(attributes, static (context, tuple) => {
            var mapping = tuple.Left!;
            var types = tuple.Right!;

            // TODO: support base class members
            var pickedMembers = mapping.Fields
                .Select(f => mapping.Source.GetMembers(f).FirstOrDefault());

            var targetTypeSyntax = types[mapping.Target.ToDisplayString()];

            var sourceBuilder = new StringBuilder();

            if (!mapping.Target.ContainingNamespace.IsGlobalNamespace)
            {
                var @namespace = mapping.Target.ContainingNamespace.ToDisplayString();

                sourceBuilder.AppendLine($"namespace {@namespace};\n");
            }

            // TODO: proper conversion
            var accessability = mapping.Target.DeclaredAccessibility.ToString().ToLower();
            var targetName = mapping.Target.Name;

            sourceBuilder.AppendLine($"{targetTypeSyntax.Modifiers} {targetTypeSyntax.Keyword} {targetName}");
            sourceBuilder.AppendLine("{");

            foreach (var member in pickedMembers)
            {
                if (member is null)
                    continue;

                var accessibility = member.DeclaredAccessibility.ToString().ToLower();

                if (member is IPropertySymbol prop)
                {
                    var propType = prop.Type.ToDisplayString();
                    var propName = prop.Name;

                    sourceBuilder.AppendLine($"    {accessibility} {propType} {propName}" + " { get; set; }");
                    continue;
                }

                if (member is IFieldSymbol field)
                {
                    var propType = field.Type.ToDisplayString();
                    var propName = field.Name;

                    sourceBuilder.AppendLine($"    {accessibility} {propType} {propName};");
                }
            }

            sourceBuilder.AppendLine("}");

            context.AddSource($"{targetName}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.Unicode));
        });
    }
}
