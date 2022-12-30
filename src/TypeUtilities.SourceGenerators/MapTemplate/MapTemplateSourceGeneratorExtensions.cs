using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using TypeUtilities.SourceGenerators.Models;
using TypeUtilities.SourceGenerators.Analyzer;

namespace TypeUtilities.SourceGenerators.MapTemplate;

internal static class MapTemplateSourceGeneratorExtensions
{
    public static IncrementalGeneratorInitializationContext CreateMapTemplateUtility(
        this IncrementalGeneratorInitializationContext context)
    {
        var mapInvocationsProvider = context.SyntaxProvider
            .CreateInvocationExpressionProvider(methodName: "Map", argsCount: 1)
            .Collect();

        var mapTemplatesProvider = context.SyntaxProvider
            .CreateAttributeSyntaxProvider<MapTemplateAttribute>()
            .SelectFromSyntax((attributeSyntax, token) =>
            (
                from targetTypeSyntax in attributeSyntax
                        .FindParent<TypeDeclarationSyntax>(token)
                        .Where(x => x.Modifiers.Any(m => m.ValueText == "partial"), Diagnostics.MissingPartialModifier)
                        .Where(x => x.TypeParameterList is not null, Diagnostics.MissingTypeParameter)
                        .Where(x => x.TypeParameterList!.Parameters.Count == 1, Diagnostics.MoreThenOneTypeParameter)
                from memberMapping in targetTypeSyntax.Members
                        .Where(member => member.AttributeLists.SelectMany(x => x.Attributes).Any(x => x.Is<MemberMappingAttribute>()))
                        .ToArray().AsSyntaxResult()
                        .Where(x => x.Any(), Diagnostics.MissingMemberMapping(targetTypeSyntax))
                        .Where(x => x.Length == 1, x => Diagnostics.MoreThenOneMemberMapping(targetTypeSyntax, x))
                        .Where(x => x[0] is MethodDeclarationSyntax) //TODO: only mathod is supported for member mapping Diagnostic ?
                select
                (
                    TemplateType: targetTypeSyntax,
                    TypeParameter: targetTypeSyntax.TypeParameterList!.Parameters[0],
                    MemberMapping: (MethodDeclarationSyntax)memberMapping[0]
                )
            ), context);

        var attributes = mapTemplatesProvider
            .Combine(mapInvocationsProvider)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(attributes, static (context, tuple) =>
        {
            var token = context.CancellationToken;

            var compilation = tuple.Right!;
            var mapTemplate = tuple.Left.Left!;
            var invocations = tuple.Left.Right!;

            var mappingsResult = (
                from mapTemplateType in mapTemplate.TemplateType.CompileNamedTypeSymbolDeclaration(compilation, token)
                from config in MapTemplateConfig.Create(mapTemplateType)
                select invocations
                    .Where(x => x.InstanceType.Identifier.ValueText == mapTemplate.TemplateType.Identifier.ValueText)
                    .Select(x =>
                    {
                        var semanticModel = compilation.GetSemanticModel(x.Invocation.SyntaxTree);
                        if (mapTemplateType.Equals(semanticModel.GetTypeInfo(x.InstanceType, token).Type, SymbolEqualityComparer.Default))
                        {
                            var arg = x.Invocation.ArgumentList.Arguments.First()!;
                            return semanticModel.GetTypeInfo(arg.Expression, token).Type!;
                        }
                        return null;
                    })
                    .Where(x => x is INamedTypeSymbol) // TODO:add diagnostics
                    .Cast<INamedTypeSymbol>()
                    .Select(mappingSource =>
                    {
                        var members = config.GetMembers(mappingSource);
                        if (members is null || !members.Any())
                        {
                            //TODO: Warning Diagnostics
                            SyntaxResult.Skip<(string, ISymbol[], INamedTypeSymbol, INamedTypeSymbol)>();
                        }

                        return SyntaxResult.Ok((
                            MemberDeclarationFormat: config.MemberDeclarationFormat,
                            SourceMembers: members.ToArray(),
                            SourceType: mappingSource,
                            MapTemplateType: mapTemplateType
                        ));
                    })
                    .Unwrap(context)
                    .ToArray()
            );

            mappingsResult.Unwrap(mappings =>
            {
                if (!mappings.Any())
                    return;

                var mapFnName = mapTemplate.MemberMapping.Identifier.ValueText;
                var mapTemplateNamespace = mappings[0].MapTemplateType.ContainingNamespace.ToString();
                var mapTemplateName = mapTemplate.TemplateType.Identifier.ValueText;

                foreach (var mapping in mappings)
                {
                    var mappedTypeName = $"{mapTemplateName}Of{mapping.SourceType.Name}";

                    var mappedMembersDelarations = mapping.SourceMembers
                        .Select(x => (Symbol: x, Format: MemberFormat.FormatDeclaration(x, mapping.MemberDeclarationFormat)))
                        .Where(x => x.Format != null)
                        .ToArray();

                    var accessibility =
                        mapping.SourceType.DeclaredAccessibility == Accessibility.Public && mapping.MapTemplateType.DeclaredAccessibility == Accessibility.Public
                            ? "public" : "internal";

                    new SourceBuilder()
                        .AddNamespace(mapping.SourceType.ContainingNamespace.ToString())
                        .AddLine($"{accessibility} class {mappedTypeName} : {mapTemplateNamespace}.{mapping.MapTemplateType!.Name}<{mapping.SourceType.Name}>")
                        .OpenScope()
                            // Add Mapped Members
                            .AddLines(mappedMembersDelarations.Select(x => x.Format).ToArray()!)
                            .AddEmptyLine()
                            // Add Constractor from source
                            .AddLine($"public {mappedTypeName}({mapping.SourceType.Name} source)")
                            .OpenScope()
                                .AddLines(mappedMembersDelarations
                                    .Select(x => (Source: x.Symbol, Target: SyntaxFactory.ParseMemberDeclaration(x.Format!)?.TryGetIdentifier()))
                                    .Where(x => x.Target is not null)
                                    .Select(x => $"this.{x.Target} = {mapFnName}(\"{x.Source.Name}\", source.{x.Source.Name});"))
                            .CloseScope()
                        .CloseScope()
                        .Build($"{mappedTypeName}.g.cs", context);
                }

                new SourceBuilder()
                    .AddNamespace(name: mapTemplateNamespace, fileScoped: mapTemplate.TemplateType.GetNamespace() is FileScopedNamespaceDeclarationSyntax)
                    .AddLine($"public static class {mapTemplateName}")
                    .OpenScope()
                        .AddLines(mappings
                            .Select(x => x.SourceType)
                            .Select(t => 
                                $"public static {t.ContainingNamespace}.{mapTemplateName}Of{t.Name} Map({t.ContainingNamespace}.{t.Name} source)" +
                                $" => new {t.ContainingNamespace}.{mapTemplateName}Of{t.Name}(source);\n"))
                    .CloseScope()
                    .Build($"{mapTemplate.TemplateType.Identifier}.factory.g.cs", context);
            },
            context);                
        });

        return context;
    }

    private static SyntaxToken? TryGetIdentifier(this MemberDeclarationSyntax memberDeclaration)
    {
        return memberDeclaration switch
        {
            PropertyDeclarationSyntax prop => prop.Identifier,
            FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier,
            _ => null
        };
    }
}
