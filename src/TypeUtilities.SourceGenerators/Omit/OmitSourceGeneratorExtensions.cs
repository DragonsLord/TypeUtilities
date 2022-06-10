using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Map;

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
            var attributeSyntax = tuple.Left.Left!;
            var compilation = tuple.Right!;

            // TODO: add diagnostic to worn about fields missing in the selection

            context.AddMappedTypeSource("omit", attributeSyntax, compilation, OmitTypeConfig.Create);
        });

        return context;
    }
}
