using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using TypeUtilities.SourceGenerators.Diagnostics;
using TypeUtilities.SourceGenerators.Helpers;

namespace TypeUtilities.SourceGenerators.Omit;

internal static class OmitSourceGeneratorExtensions
{
    public static IncrementalGeneratorInitializationContext CreateOmitUtility(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<Dictionary<string, TypeDeclarationSyntax>> types)
    {
        var attributes = context.SyntaxProvider
            .CreateAttributeSyntaxProvider<OmitAttribute>()
            .Combine(types)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(attributes, static (context, tuple) =>
        {
            var token = context.CancellationToken;
            var attributeSyntax = tuple.Left.Left!;
            var types = tuple.Left.Right!;
            var compilation = tuple.Right!;

            try
            {
                if (!attributeSyntax.TryFindParent<TypeDeclarationSyntax>(out var targetTypeSyntax, token))
                    return;

                if (!targetTypeSyntax.TryCompileNamedTypeSymbol(compilation, token, out var targetTypeSymbol))
                    return;

                var config = OmitTypeConfig.Create(targetTypeSymbol);
                if (config is null)
                    return;

                // TODO: also introduce fine grained control over memners to pick
                var pickedMembers = config.Source
                    .GetExplicitMembers(config.IncludeBaseTypes)
                    .Where(m => m is IPropertySymbol || m is IFieldSymbol)
                    .Where(m => !config.Fields.Contains(m.Name));

                context.WriteType(
                               @namespace:  config.Target.ContainingNamespace,
                    typeDeclarationSyntax:  targetTypeSyntax,
                                  members:  pickedMembers,
                           outputFileName:  $"{config.Target.Name}.omit.{config.Source.Name}.g.cs");
            }
            catch (Exception ex)
            {
                context.ReportInternalError(ex, attributeSyntax);
            }
        });

        return context;
    }
}
