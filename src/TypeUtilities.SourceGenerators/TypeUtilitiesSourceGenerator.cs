using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Omit;
using TypeUtilities.SourceGenerators.Pick;
using TypeUtilities.SourceGenerators.Map;
using TypeUtilities.SourceGenerators.MapTemplate;

namespace TypeUtilities.SourceGenerators
{
    [Generator]
    internal class TypeUtilitiesSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // we need this provider to monitor for changes in source types
            var typesDict = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is TypeDeclarationSyntax,
                    transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node)
                .Collect()
                .Select((types, ct) => types.ToDictionary(x => x.GetFullName(ct)));

            context
                .CreateMapUtility(typesDict)
                .CreatePickUtility(typesDict)
                .CreateOmitUtility(typesDict)
                .CreateMapTemplateUtility();
        }
    }
}
