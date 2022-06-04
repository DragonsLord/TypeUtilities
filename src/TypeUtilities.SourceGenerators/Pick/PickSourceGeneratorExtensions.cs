using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Diagnostics;

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
            var token = context.CancellationToken;
            var attributeSyntax = tuple.Left.Left!;
            var types = tuple.Left.Right!;
            var compilation = tuple.Right!;

            try
            {
                if (!attributeSyntax.TryFindParent<TypeDeclarationSyntax>(out var targetTypeSyntax, token))
                    return;

                if (!targetTypeSyntax.Modifiers.Any(m => m.ValueText == "partial"))
                {
                    context.ReportMissingPartialModifier(targetTypeSyntax);
                    return;
                }

                if (!targetTypeSyntax.TryCompileNamedTypeSymbol(compilation, token, out var targetTypeSymbol))
                    return;

                var config = PickTypeConfig.Create(targetTypeSymbol);
                if (config is null)
                    return;

                var pickedMembers = config.Fields
                    .Select(f => config.Source.GetMember(f, config.IncludeBaseTypes, context.CancellationToken));

                // TODO: check and report duplicate members

                context.WriteType(
                               @namespace: config.Target.ContainingNamespace,
                    typeDeclarationSyntax: targetTypeSyntax,
                                  members: pickedMembers,
                           outputFileName: $"{config.Target.Name}.pick.{config.Source.Name}.g.cs");
            }
            catch (Exception ex)
            {
                context.ReportInternalError(ex, attributeSyntax);
            }
        });

        return context;
    }
}
