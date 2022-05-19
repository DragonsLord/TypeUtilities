using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeUtilities.Pick;

namespace TypeUtilities.SourceGenerators
{
    internal class TypeUtilitiesSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typesDict = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is TypeDeclarationSyntax,
                    transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node)
                .Collect()
                .Select((types, ct) => types.ToDictionary(x => x.GetFullName(ct)));

            context.CreatePickUtility(typesDict);
        }
    }
}
