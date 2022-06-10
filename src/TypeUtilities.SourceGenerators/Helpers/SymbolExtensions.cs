using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class SymbolExtensions
    {
        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol typeSymbol)
        {
            var current = typeSymbol?.BaseType;

            while (current is not null && current.SpecialType != SpecialType.System_Object)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetExplicitMembers(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers().Where(m => !m.IsImplicitlyDeclared);
        }

        public static string GetAccessors(this IPropertySymbol symbol)
        {
            var get = symbol.GetMethod is null ? string.Empty : " get;";
            var set = symbol.SetMethod is null ? string.Empty : " set;";

            return "{"+get+set+" }";
        }
    }
}
