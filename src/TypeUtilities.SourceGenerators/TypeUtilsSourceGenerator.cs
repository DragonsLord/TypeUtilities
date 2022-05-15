using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using TypeUtilities.Pick;

namespace TypeUtilities.SourceGenerators;

[Generator]
internal class TypeUtilsSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif

        var types = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node)
            .Collect()
            .Select((types, ct) => types.ToDictionary(x => x.GetFullName(ct)));

        context.CreatePickUtility(types);
    }
}
