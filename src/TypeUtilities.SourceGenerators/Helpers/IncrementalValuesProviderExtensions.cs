using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class IncrementalValuesProviderExtensions
    {
        public static IncrementalValuesProvider<AttributeSyntax> CreateAttributeSyntaxProvider(this SyntaxValueProvider syntaxProvider, string attributeName)
        {
            var attributeNameRegex = new Regex($"^(TypeUtilities)?{attributeName}(Attribute)?$");
            return syntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node is AttributeSyntax attr && attributeNameRegex.IsMatch(attr.Name.ToString()),
                transform: (ctx, ct) =>
                {
                    try
                    {
                        var attributeSyntax = (AttributeSyntax)ctx.Node;
                        var attributeSymbolInfo = ctx.SemanticModel.GetSymbolInfo(attributeSyntax, ct);

                        var symbol = attributeSymbolInfo.Symbol ?? attributeSymbolInfo.CandidateSymbols.FirstOrDefault();

                        if (symbol is not IMethodSymbol attributeSymbol)
                            return null;

                        var attributeFullName = attributeSymbol.ContainingType.ToDisplayString();

                        if (attributeFullName != $"TypeUtilities.{attributeName}Attribute")
                            return null;

                        return attributeSyntax;
                    }
                    catch
                    {
                    // TODO: diagnostics?
                    return null;
                    }

                }).WhereNotNull();
        }

        public static IncrementalValuesProvider<T> WhereNotNull<T>(this IncrementalValuesProvider<T?> provider)
        {
            return provider.Where(x => x is not null)!;
        }
    }
}
