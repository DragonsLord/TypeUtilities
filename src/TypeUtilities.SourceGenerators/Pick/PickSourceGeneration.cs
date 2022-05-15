using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;
using TypeUtilities.SourceGenerators.Pick;

namespace TypeUtilities.Pick;

internal static class PickExtensions
{
    private static Regex attributeNameRegex = new Regex("^(TypeUtilities)?Pick(Attribute)?$");

    public static void CreatePickUtility(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<Dictionary<string, TypeDeclarationSyntax>> types)
    {
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

                   return attributeSyntax;
               })
           .SkipNulls()
           .Combine(types) // we need typeDict only for change detection
           .Combine(context.CompilationProvider);

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(attributes, static (context, tuple) => {
            var token = context.CancellationToken;
            var attributeSyntax = tuple.Left.Left;
            var compilation = tuple.Right;

            var semanticModel = compilation.GetSemanticModel(attributeSyntax.SyntaxTree);

            if (!attributeSyntax.TryFindParent<TypeDeclarationSyntax>(out var targetTypeSyntax, token))
                return;

            var targetTypeSymbolInfo = semanticModel.GetDeclaredSymbol(targetTypeSyntax, token);

            if (targetTypeSymbolInfo is not INamedTypeSymbol targetTypeSymbol)
                return;

            var attributeSymbol = semanticModel.GetSymbolInfo(attributeSyntax, token).Symbol;
            if (attributeSymbol is not IMethodSymbol)
                return;

            var config = PickTypeConfig.Create(attributeSymbol.ContainingType, targetTypeSymbol);

            if (config is null)
                return;

            var pickedMembers = config.Fields
                .Select(f => config.Source.GetMember(f, config.IncludeBaseTypes));

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

            var generatedSource = sourceBuilder.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

            context.AddSource($"{targetName}.g.cs", SourceText.From(generatedSource, Encoding.Unicode));
        });
    }
}
