using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeUtilities.SourceGenerators.Omit;

internal static class OmitSourceGeneratorExtensions
{
    private static Regex attributeNameRegex = new Regex("^(TypeUtilities)?Omit(Attribute)?$");

    public static IncrementalGeneratorInitializationContext CreateOmitUtility(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<Dictionary<string, TypeDeclarationSyntax>> types)
    {
        var attributes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is AttributeSyntax attr && attributeNameRegex.IsMatch(attr.Name.ToString()),
                transform: static (ctx, ct) =>
                {
                    try
                    {
                        var attributeSyntax = (AttributeSyntax)ctx.Node;
                        var attributeSymbolInfo = ctx.SemanticModel.GetSymbolInfo(attributeSyntax, ct);

                        var symbol = attributeSymbolInfo.Symbol ?? attributeSymbolInfo.CandidateSymbols.FirstOrDefault();

                        if (symbol is not IMethodSymbol attributeSymbol)
                            return null;

                        var attributeFullName = attributeSymbol.ContainingType.ToDisplayString();

                        if (attributeFullName != "TypeUtilities.OmitAttribute")
                            return null;

                        if (!attributeSyntax.TryFindParent<TypeDeclarationSyntax>(out var targetTypeSyntax, ct))
                            return null;

                        var targetTypeSymbolInfo = ctx.SemanticModel.GetDeclaredSymbol(targetTypeSyntax, ct);

                        if (targetTypeSymbolInfo is not INamedTypeSymbol targetTypeSymbol)
                            return null;

                        return OmitTypeConfig.Create(attributeSymbol.ContainingType, targetTypeSymbol);
                    }
                    catch
                    {
                        // TODO: diagnostics?
                        return null;
                    }

                })
            .SkipNulls()
            .WithComparer(OmitTypeConfig.Comparer)
            .Combine(types);

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(attributes, static (context, tuple) => {
            try
            {
                var config = tuple.Left!;
                var types = tuple.Right!;

                // TODO: add include base type support
                // TODO: also introduce fine grained control over memners to pick
                var pickedMembers = config.Source
                    .GetMembers()
                    .Where(m => !m.IsImplicitlyDeclared)
                    .Where(m => m is IPropertySymbol || m is IFieldSymbol)
                    .Where(m => !config.Fields.Contains(m.Name));

                var targetTypeSyntax = types[config.Target.ToDisplayString()];

                var sourceBuilder = new StringBuilder();

                if (!config.Target.ContainingNamespace.IsGlobalNamespace)
                {
                    var @namespace = config.Target.ContainingNamespace.ToDisplayString();

                    sourceBuilder.AppendLine($"namespace {@namespace};\n");
                }

                // TODO: proper conversion
                var accessability = config.Target.DeclaredAccessibility.ToString().ToLower();
                var targetName = config.Target.Name;

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

                context.AddSource($"{targetName}.omit.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.Unicode));
            }
            catch { /* TODO: diagnostics? */ }
        });

        return context;
    }
}
