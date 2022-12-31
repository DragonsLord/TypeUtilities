using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using TypeUtilities.SourceGenerators.Models;
using TypeUtilities.SourceGenerators.Analyzer;
using TypeUtilities.Abstractions;

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
                        .Where(x => x.TypeParameterList is not null, Diagnostics.MissingTypeParameter)
                        .Where(x => x.TypeParameterList!.Parameters.Count == 1, Diagnostics.MoreThenOneTypeParameter)
                from memberMapping in targetTypeSyntax.Members
                        .Where(member => member.AttributeLists.SelectMany(x => x.Attributes).Any(x => x.Is<MemberMappingAttribute>()))
                        .ToArray().AsSyntaxResult()
                        .Where(x => x.Any(), Diagnostics.MissingMemberMapping(targetTypeSyntax))
                        .Where(x => x.Length == 1, x => Diagnostics.MoreThenOneMemberMapping(targetTypeSyntax, x))
                        // TODO: validate method signature
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

                    var sourceNamespace = mapping.SourceType.ContainingNamespace.ToString();

                    new SourceBuilder()
                        .AddLines(
                            $"using {nameof(TypeUtilities)};",
                            $"using {nameof(TypeUtilities)}.{nameof(Abstractions)};")
                        .AddEmptyLine()
                        .AddNamespace(mapTemplateNamespace)
                        .AddLine($"{accessibility} class {mappedTypeName} : {mapTemplateNamespace}.{mapping.MapTemplateType!.Name}<{sourceNamespace}.{mapping.SourceType.Name}>")
                        .OpenScope()
                            // Add Mapped Members
                            .AddLines(mappedMembersDelarations.Select(x => x.Format).ToArray()!)
                            .AddEmptyLine()
                            // Add ctor from source
                            .AddLine($"public {mappedTypeName}({sourceNamespace}.{mapping.SourceType.Name} source)")
                            .OpenScope()
                                .AddLines(mappedMembersDelarations
                                    .Select(x => (Source: x.Symbol, Target: SyntaxFactory.ParseMemberDeclaration(x.Format!)?.TryGetIdentifier()))
                                    .Where(x => x.Target is not null)
                                    .Select(x => $"this.{x.Target} = {mapFnName}(MemberInfo.Create<{x.Source.GetMemberType()}>(\"{x.Source.Name}\", {x.Source.GetMemberAccessibility()}, {x.Source.GetMemberScope()}, {x.Source.GetMemberKind()}), source.{x.Source.Name});"))
                            .CloseScope()
                        .CloseScope()
                        .Build($"{mappedTypeName}.g.cs", context);
                }

                new SourceBuilder()
                    .AddNamespace(name: mapTemplateNamespace, fileScoped: mapTemplate.TemplateType.GetNamespace() is FileScopedNamespaceDeclarationSyntax)
                    .AddLine($"public static class {mapTemplateName}")
                    .OpenScope()
                        // TODO: use switch map on T instead of throwing
                        .AddLine($"public static {mapTemplateName}<T> Map<T>(T source) => throw new System.NotImplementedException($\"Missing 'Map' for {{ typeof(T).Name}} type\");\n")
                        .AddLines(mappings
                            .Select(x => x.SourceType)
                            .Select(t => 
                                $"public static {mapTemplateName}Of{t.Name} Map({t.ContainingNamespace}.{t.Name} source)" +
                                $" => new {mapTemplateName}Of{t.Name}(source);\n"))
                    .CloseScope()
                    .Build($"{mapTemplate.TemplateType.Identifier}.factory.g.cs", context);
            },
            context);                
        });

        return context;
    }

    private static string GetMemberType(this ISymbol symbol)
    {
        return symbol switch
        {
            IPropertySymbol prop => prop.Type.GetFullName(),
            IFieldSymbol field => field.Type.GetFullName(),
            _ => string.Empty
        };
    }

    private static string GetMemberAccessibility(this ISymbol symbol)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => $"{nameof(MemberAccessibilityFlags)}.{nameof(MemberAccessibilityFlags.Public)}",
            Accessibility.Protected => $"{nameof(MemberAccessibilityFlags)}.{nameof(MemberAccessibilityFlags.Protected)}",
            Accessibility.Private => $"{nameof(MemberAccessibilityFlags)}.{nameof(MemberAccessibilityFlags.Private)}",
            _ => $"default({nameof(MemberAccessibilityFlags)})"
        };
    }

    private static string GetMemberScope(this ISymbol symbol)
    {
        return symbol.IsStatic
            ? $"{nameof(MemberScopeFlags)}.{nameof(MemberScopeFlags.Static)}"
            : $"{nameof(MemberScopeFlags)}.{nameof(MemberScopeFlags.Instance)}";
    }

    private static string GetMemberKind(this ISymbol symbol)
    {
        return symbol switch
        {
            IPropertySymbol { GetMethod: not null, SetMethod: not null } => $"{nameof(MemberKindFlags)}.{nameof(MemberKindFlags.GetSetProperty)}",
            IPropertySymbol { GetMethod: not null, SetMethod:     null } => $"{nameof(MemberKindFlags)}.{nameof(MemberKindFlags.ReadonlyProperty)}",
            IPropertySymbol { GetMethod:     null, SetMethod: not null } => $"{nameof(MemberKindFlags)}.{nameof(MemberKindFlags.WriteonlyProperty)}",
            IFieldSymbol { IsReadOnly:  true } => $"{nameof(MemberKindFlags)}.{nameof(MemberKindFlags.ReadonlyField)}",
            IFieldSymbol { IsReadOnly: false } => $"{nameof(MemberKindFlags)}.{nameof(MemberKindFlags.WritableField)}",
            _ => $"default({nameof(MemberKindFlags)})"
        };
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
