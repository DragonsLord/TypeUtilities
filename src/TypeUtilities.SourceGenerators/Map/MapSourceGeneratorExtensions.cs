using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Analyzer;

namespace TypeUtilities.SourceGenerators.Map;

internal static class MapSourceGeneratorExtensions
{
    public static void AddMappedTypeSource<T>(
        this SourceProductionContext context,
        string mapTransformName,
        AttributeSyntax attributeSyntax,
        Compilation compilation,
        Func<INamedTypeSymbol, T?> getConfig) where T : MapTypeConfig
    {
        var token = context.CancellationToken;

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

            var config = getConfig(targetTypeSymbol);
            if (config is null)
                return;

            var members = config.GetMembers(context, attributeSyntax.GetLocation());

            if (!members.Any())
                context.ReportNoMappedMembers(config.Source, attributeSyntax.GetLocation());

            context.WriteType(
                typeDeclarationSyntax: targetTypeSyntax,
                                members: members,
                memberDeclarationFormat: config.MemberDeclarationFormat,
                        outputFileName: $"{config.Target.Name}.{mapTransformName}.{config.Source.Name}.g.cs",
                                token: token);
        }
        catch (Exception ex)
        {
            context.ReportInternalError(ex, attributeSyntax);
        }
    }

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

            context.AddMappedTypeSource("map", attributeSyntax, compilation, MapTypeConfig.Create);
        });

        return context;
    }
}
