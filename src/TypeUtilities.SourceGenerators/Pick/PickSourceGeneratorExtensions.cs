using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Map;

namespace TypeUtilities.SourceGenerators.Pick;

internal static class PickSourceGeneratorExtensions
{
    public static IncrementalGeneratorInitializationContext CreatePickUtility(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<Dictionary<string, TypeDeclarationSyntax>> types)
    {
        var attributes = context.SyntaxProvider
            .CreateAttributeSyntaxProvider<PickAttribute>()
            .Combine(types)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(attributes, static (context, tuple) =>
        {
            var attributeSyntax = tuple.Left.Left!;
            var compilation = tuple.Right!;

            // TODO: add diagnostic to worn about fields missing in the selection

            context.AddMappedTypeSource("pick", attributeSyntax, compilation, PickTypeConfig.Create);
        });

        return context;
    }
}
