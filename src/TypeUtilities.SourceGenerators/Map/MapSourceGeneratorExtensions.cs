using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Diagnostics;

namespace TypeUtilities.SourceGenerators.Pick;

internal static class MapSourceGeneratorExtensions
{

    public static IncrementalGeneratorInitializationContext CreateMapUtility(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<Dictionary<string, TypeDeclarationSyntax>> types)
    {
        var attributes = context.SyntaxProvider
            .CreateAttributeSyntaxProvider<MapAttribute>()
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

                var config = MapTypeConfig.Create(targetTypeSymbol);
                if (config is null)
                    return;

                var members = config.Source
                    .GetExplicitMembers(config.IncludeBaseTypes)
                    .Where(m => m is IPropertySymbol || m is IFieldSymbol);

                context.WriteType(
                    typeDeclarationSyntax: targetTypeSyntax,
                                  members: members,
                  memberDeclarationFormat: config.MemberDeclarationFormat,
                           outputFileName: $"{config.Target.Name}.map.{config.Source.Name}.g.cs",
                                    token: token);
            }
            catch (Exception ex)
            {
                context.ReportInternalError(ex, attributeSyntax);
            }
        });

        return context;
    }
}
