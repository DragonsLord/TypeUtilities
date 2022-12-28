using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using TypeUtilities.SourceGenerators.Models;
using TypeUtilities.SourceGenerators.Analyzer;

namespace TypeUtilities.SourceGenerators.MapTemplate;

internal static class MapTemplateSourceGeneratorExtensions
{
    class MapTemplateMetadata
    {
        public TypeDeclarationSyntax TemplateType { get; set; }
        public TypeParameterSyntax TypeParameter { get; set; }
        public MethodDeclarationSyntax MemberMapping { get; set; }
    }

    private static IncrementalValuesProvider<MapTemplateMetadata> GetMapTemplateMetadata(this IncrementalValuesProvider<AttributeSyntax> attributeSyntaxProvider, IncrementalGeneratorInitializationContext context)
    {
        return attributeSyntaxProvider.SelectFromSyntax((attributeSyntax, token) =>
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
            select new MapTemplateMetadata
            {
                TemplateType = targetTypeSyntax,
                TypeParameter = targetTypeSyntax.TypeParameterList!.Parameters[0],
                MemberMapping = (MethodDeclarationSyntax)memberMapping[0]
            }
        ), context);
    }

    public static IncrementalGeneratorInitializationContext CreateMapTemplateUtility(
        this IncrementalGeneratorInitializationContext context)
    {
        var mapTemplatesProvider = context.SyntaxProvider
            .CreateAttributeSyntaxProvider<MapTemplateAttribute>()
            .GetMapTemplateMetadata(context);

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
                    // TODO: validate full name of argument and template types
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

                var mappedMembersDelarations = sourceMembers.Select(x => (Symbol: x, Format: MemberFormat.FormatDeclaration(x, config.MemberDeclarationFormat))).Where(x => x.Format != null).ToArray();

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
                    mappedMembersDelarations
                        .Select(x => (Source: x.Symbol, Target: SyntaxFactory.ParseMemberDeclaration(x.Format!)?.TryGetIdentifier()))
                        .Where(x => x.Target is not null)
                        .Select(x =>
                            $"this.{x.Target} = {mapTemplate.MemberMapping.Identifier.ValueText}(\"{x.Source.Name}\", source.{x.Source.Name});").ToArray()
                );

                var members = mappedMembersDelarations.Select(x => PrintableMember.FromSourceLines(x.Format!)).Concat(new[] { PrintableMember.EmptyLine(), ctorMember });

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

    private static SyntaxToken? TryGetIdentifier(this MemberDeclarationSyntax memberDeclaration)
    {
        return memberDeclaration switch
        {
            PropertyDeclarationSyntax prop => prop.Identifier,
            FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier,
            _ => null
        };
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
