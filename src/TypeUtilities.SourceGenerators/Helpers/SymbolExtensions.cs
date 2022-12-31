using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class SymbolExtensions
    {
        public static IEnumerable<ITypeSymbol> GetTypeHierarchy(this ITypeSymbol typeSymbol)
        {
            var current = typeSymbol;

            while (current is not null && current.SpecialType != SpecialType.System_Object)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetExplicitMembers(this ITypeSymbol typeSymbol, bool includeBase)
        {
            var types = includeBase ? typeSymbol.GetTypeHierarchy() : new ITypeSymbol[] { typeSymbol };

            return types.SelectMany(type => type
                    .GetMembers()
                    .Where(m => !m.IsImplicitlyDeclared));
        }

        public static string GetAccessors(this IPropertySymbol symbol)
        {
            string GetAccessorDeclaration(IMethodSymbol method, string accessor)
            {

                var accessibility = method.DeclaredAccessibility == symbol.DeclaredAccessibility ?
                    "" : method.DeclaredAccessibility.ToString().ToLower() + " ";

                return $" {accessibility}{accessor};";
            }

            string GetSetDeclaration(IMethodSymbol setMethod)
            {
                if (setMethod.IsInitOnly)
                    return " init;";

                return GetAccessorDeclaration(setMethod, "set");
            }

            var get = symbol.GetMethod is null ? string.Empty : GetAccessorDeclaration(symbol.GetMethod, "get");
            var set = symbol.SetMethod is null ? string.Empty : GetSetDeclaration(symbol.SetMethod);

            return "{" + get + set + " }";
        }

        public static string GetFullName(this ITypeSymbol? typeSymbol) => typeSymbol is not null ? $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}" : string.Empty;
    }
}
