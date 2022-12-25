using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Diagnostics;
using TypeUtilities.SourceGenerators.Map;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace TypeUtilities.SourceGenerators.MapTemplate;

internal static class MapTemplateSourceGeneratorExtensions
{
    class MapTemplateMetadata
    {
        public TypeDeclarationSyntax TemplateType { get; set; }
        public TypeParameterSyntax TypeParameter { get; set; }
        public MethodDeclarationSyntax MemberMapping { get; set; }
    }

    private static IncrementalValuesProvider<MapTemplateMetadata> GetMapTemplateMetadata(this IncrementalValuesProvider<AttributeSyntax> attributeSyntaxProvider)
    {
        return attributeSyntaxProvider.Select((attributeSyntax, token) =>
        {
            try
            {
                if (!attributeSyntax.TryFindParent<TypeDeclarationSyntax>(out var targetTypeSyntax, token))
                    return null;

                if (!targetTypeSyntax.Modifiers.Any(m => m.ValueText == "partial"))
                {
                    // context.ReportMissingPartialModifier(targetTypeSyntax);
                    return null;
                }

                if (targetTypeSyntax.TypeParameterList is null)
                {
                    // No Type Parameter Found
                    return null;
                }

                if (targetTypeSyntax.TypeParameterList.Parameters.Count > 1)
                {
                    // More Then One Type Parameter  Found
                    return null;
                }

                var memberMappings = targetTypeSyntax.Members.Where(member => member.AttributeLists.SelectMany(x => x.Attributes).Any(x => x.Is<MemberMappingAttribute>())).ToArray();
                if (!memberMappings.Any())
                {
                    // No Member Mapping Found
                    return null;
                }
                if (memberMappings.Length > 1)
                {
                    // More then one mamber mappings found
                    return null;
                }
                if (memberMappings[0] is not MethodDeclarationSyntax memberMapping)
                {
                    return null;
                }

                return new MapTemplateMetadata
                {
                    TemplateType = targetTypeSyntax,
                    TypeParameter = targetTypeSyntax.TypeParameterList.Parameters[0],
                    MemberMapping = memberMapping
                };
            }
            catch (Exception ex)
            {
                //context.ReportInternalError(ex, attributeSyntax);
                return null;
            }
        }).WhereNotNull();
    }

    //private static IncrementalValuesProvider<MapTemplateMetadata> GetMapTemplateTargets(this IncrementalValuesProvider<(MapTemplateMetadata MapTemplate, ImmutableArray<(InvocationExpressionSyntax Invocation, IdentifierNameSyntax InstanceType)> MapTargets)> invocationsPrivder)
    //{
    //    return invocationsPrivder.Select((node, token) =>
    //    {
    //        var mapTemplate = node.MapTemplate;
    //        var mapTargets = node.MapTargets
    //            .Where(x => x.InstanceType.Identifier == mapTemplate.TemplateType.Identifier);

    //        // TODO: return MapConfig and MapTemplate
    //        return null;
    //    });
    //}

    public static IncrementalGeneratorInitializationContext CreateMapTemplateUtility(
        this IncrementalGeneratorInitializationContext context)
    {
        var mapTemplatesProvider = context.SyntaxProvider
            .CreateAttributeSyntaxProvider<MapTemplateAttribute>()
            .GetMapTemplateMetadata();

        var mapSourceTypeProvider = context.SyntaxProvider
            .CreateInvocationExpressionProvider(methodName: "Map", argsCount: 1)
            .Collect();

        var attributes = mapTemplatesProvider
            .Combine(mapSourceTypeProvider)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(attributes, static (context, tuple) =>
        {
            var token = context.CancellationToken;

            var compilation = tuple.Right!;
            var mapTemplate = tuple.Left.Left!;
            var sourceTypes = tuple.Left.Right
                .Where(x => x.InstanceType.Identifier.ValueText == mapTemplate.TemplateType.Identifier.ValueText)
                .Select(x => {
                    // TODO: validated fullnames of argument and template types
                    var argSyntaxNode = x.Invocation.ArgumentList.Arguments.First()!;
                    var semanticModel = compilation.GetSemanticModel(argSyntaxNode.SyntaxTree);
                    return semanticModel.GetTypeInfo(argSyntaxNode.Expression).Type!;
                })
                .Where(x => x is not null)
                .ToArray()!;

            if (!mapTemplate.TemplateType.TryCompileNamedTypeSymbol(compilation, token, out var namedTemplateSymbol))
            {
                //TODO: Add Diagnostic
                return;
            }

            var config = MapTemplateConfig.Create(namedTemplateSymbol);

            if (config is null)
            {
                return;
            }

            foreach (var mappingSource in sourceTypes)
            {
                if (mappingSource is not INamedTypeSymbol sourceTypeSymbol)
                {
                    //TODO: only named types is supported
                    continue;
                }

                var sourceMembers = config.GetMembers(mappingSource);

                if (sourceMembers is null)
                {
                    //TODO: Diagnostics
                    continue;
                }

                var mappedTypeName = $"{mapTemplate.TemplateType.Identifier.ValueText}Of{sourceTypeSymbol.Name}";

                var mappedTypeDeclaration = mapTemplate.TemplateType
                    // TODO: overwrite modifiers?
                    .WithIdentifier(SyntaxFactory.Identifier(mappedTypeName))
                    // TODO: include namespace to base class
                    .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"{mapTemplate.TemplateType.Identifier.ValueText}<{sourceTypeSymbol.Name}>")))))
                    .WithTypeParameterList(null)
                    // TODO: Extension to get full Namespace
                    .WithNamespace(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(namedTemplateSymbol.ContainingNamespace.Name))); 

                var ctorMember = PrintableMember.FunctionSource(
                    $"public {mappedTypeName}({sourceTypeSymbol.Name} source)",
                    // TODO: respected member declaration format
                    sourceMembers.Select(x => $"this.{x.Name} = {mapTemplate.MemberMapping.Identifier.ValueText}(\"{x.Name}\", source.{x.Name});").ToArray()
                );

                var members = sourceMembers.Select(x => PrintableMember.FromSymbol(x, config.MemberDeclarationFormat)).Concat(new[] { PrintableMember.EmptyLine(), ctorMember });

                context.WriteType(mappedTypeDeclaration, members, $"{mappedTypeName}.g.cs", token);
            }

            var factoryMethods =
            //new[]{ PrintableMember.FromSourceLines($"public static {mapTemplate.TemplateType.Identifier}<T> Map<T>(T source) => throw new NotImplementedException($\"Missing 'Map' for {{typeof(T).Name}} type\");") }
            sourceTypes.Select(t =>
                // FIXME: ContainingNamespace is not a full namespace
                PrintableMember.FromSourceLines(
                    $"public static {t.ContainingNamespace.Name}.{mapTemplate.TemplateType.Identifier}Of{t.Name} Map({t.ContainingNamespace.Name}.{t.Name} source) => new {t.ContainingNamespace.Name}.{mapTemplate.TemplateType.Identifier}Of{t.Name}(source);"));

            var factoryClassDeclaration = mapTemplate.TemplateType
                        .WithTypeParameterList(null)
                        .WithModifiers(SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ")), SyntaxKind.StaticKeyword, SyntaxTriviaList.Empty)))
                         //FIXME? This is only working when the last one
                        .WithNamespace(mapTemplate.TemplateType.GetNamespace(token));  // FIXME: this should be a FULL namespace

            context.WriteType(factoryClassDeclaration, factoryMethods, $"{mapTemplate.TemplateType.Identifier}.factory.g.cs", token);
        });

        return context;
    }

    private static T WithNamespace<T>(this T thisNode, BaseNamespaceDeclarationSyntax? namespaceNode)
        where T : MemberDeclarationSyntax
    {
        if (namespaceNode is not null)
        {
            return (T)namespaceNode.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(thisNode)).Members.First();
        }
        return thisNode;
    }
}
