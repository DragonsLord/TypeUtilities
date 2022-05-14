using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeUtilities.Pick;

internal class TypeMapping
{
    public INamedTypeSymbol Source { get; set; }
    public INamedTypeSymbol Target { get; set; }
    public string[] Fields { get; set; }
}

[Generator]
internal class PickSourceGenerator : IIncrementalGenerator
{
    // TODO: move to separate non analyzer package
    private string _attributeSrc = @"
using System;

namespace TypeUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class PickAttribute : Attribute
{
    public Type SourceType { get; set; } 
    public string[] Fields { get; set; } = Array.Empty<string>();

    public string Comment { get; set; }

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
//#if DEBUG
//        if (!Debugger.IsAttached)
//        {
//            Debugger.Launch();
//        }
//#endif

        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "PickAttribute.g.cs",
            SourceText.From(_attributeSrc, Encoding.UTF8)));

        var typesDict = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node)
            .Collect()
            .Select((types, ct) => types.ToDictionary(x => x.Identifier.ToString())); // TODO: include namespace (use parent)

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

                    // TODO: figure out how to recalculate on source type changes
                    var attributeData = targetTypeSymbol.GetAttributes()[0]; // TODO: loop
                    var sourceTypeSymbol = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol; // return null if null :)
                    var fields = attributeData.ConstructorArguments[1].Values.Select(x => x.Value as string).Where(x => x is not null).ToArray();

                    return new TypeMapping
                    {
                        Target = targetTypeSymbol,
                        Source = sourceTypeSymbol!,
                        Fields = fields!
                    };
                })
            .Where(x => x is not null)
            .Combine(typesDict);

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(attributes, static (context, tuple) => {
            var mapping = tuple.Left;
            var types = tuple.Right;
            var pickedMembers = mapping.Fields
                .Select(f => mapping.Source.GetMembers(f).FirstOrDefault());

            var sourceBuilder = new StringBuilder();

            var @namespace = mapping.Target.ContainingNamespace.ToDisplayString();

            sourceBuilder.AppendLine($"namespace {@namespace};\n");

            var accessability = mapping.Target.DeclaredAccessibility.ToString().ToLower();
            var targetName = mapping.Target.Name;

            sourceBuilder.AppendLine($"{accessability} partial class {targetName}");
            sourceBuilder.AppendLine("{");

            foreach (var member in pickedMembers)
            {
                if (member is IPropertySymbol prop)
                {
                    var propAccessibility = prop.DeclaredAccessibility.ToString().ToLower();
                    var propType = prop.Type.ToDisplayString();
                    var propName = prop.Name;

                    sourceBuilder.AppendLine($"    {propAccessibility} {propType} {propName}" + " { get; set; }");
                }
            }

            sourceBuilder.AppendLine("}");

            context.AddSource($"{targetName}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        });
    }
}
