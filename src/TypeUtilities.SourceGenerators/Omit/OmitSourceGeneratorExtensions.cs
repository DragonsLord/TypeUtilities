using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Omit;

internal static class OmitSourceGeneratorExtensions
{
    public static IncrementalGeneratorInitializationContext CreateOmitUtility(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<Dictionary<string, TypeDeclarationSyntax>> types)
    {
        var attributes = context.SyntaxProvider
            .CreateAttributeSyntaxProvider("Omit")
            .Combine(types)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(attributes, static (context, tuple) =>
        {
            try
            {
                var token = context.CancellationToken;
                var attributeSyntax = tuple.Left.Left!;
                var types = tuple.Left.Right!;
                var compilation = tuple.Right!;

                if (!attributeSyntax.TryFindParent<TypeDeclarationSyntax>(out var targetTypeSyntax, token))
                    return;

                if (!targetTypeSyntax.TryCompileNamedTypeSymbol(compilation, token, out var targetTypeSymbol))
                    return;

                var config = OmitTypeConfig.Create(targetTypeSymbol);
                if (config is null)
                    return;

                // TODO: add include base type support
                // TODO: also introduce fine grained control over memners to pick
                var pickedMembers = config.Source
                    .GetMembers()
                    .Where(m => !m.IsImplicitlyDeclared)
                    .Where(m => m is IPropertySymbol || m is IFieldSymbol)
                    .Where(m => !config.Fields.Contains(m.Name));

                context.WriteType(
                               @namespace:  config.Target.ContainingNamespace,
                    typeDeclarationSyntax:  targetTypeSyntax,
                                  members:  pickedMembers,
                           outputFileName:  $"{config.Target.Name}.omit.{config.Source.Name}.g.cs");
            }
            catch { /* TODO: diagnostics? */ }
        });

        return context;
    }
}
