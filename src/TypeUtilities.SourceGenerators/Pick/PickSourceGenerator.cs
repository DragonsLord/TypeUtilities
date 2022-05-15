using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using TypeUtilities.SourceGenerators.Pick;

namespace TypeUtilities.Pick;

[Generator]
internal class PickSourceGenerator : IIncrementalGenerator
{
    private static Regex attributeNameRegex = new Regex("^(TypeUtilities)?Pick(Attribute)?$");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif

        var typesDict = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node)
            .Collect()
            .Select((types, ct) => types.ToDictionary(x => x.GetFullName(ct)));

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

                    if (!attributeSyntax.TryFindParent<TypeDeclarationSyntax>(out var targetTypeSyntax, ct))
                        return null;

                    var targetTypeSymbolInfo = ctx.SemanticModel.GetDeclaredSymbol(targetTypeSyntax, ct);

                    if (targetTypeSymbolInfo is not INamedTypeSymbol targetTypeSymbol)
                        return null;

                    return PickTypeConfig.Create(attributeSymbol.ContainingType, targetTypeSymbol);
                })
            .SkipNulls()
            .WithComparer(PickTypeConfig.Comparer)
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
