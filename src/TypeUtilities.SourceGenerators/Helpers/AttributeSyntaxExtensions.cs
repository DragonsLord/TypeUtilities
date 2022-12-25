using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class AttributeSyntaxExtensions
    {
        public static string GetShortAttributeName(this Type type)
        {
            return type.Name.Substring(0, type.Name.Length - 9).ToString();
        }

        public static bool Is<T>(this AttributeSyntax attributeSyntax)
            where T : Attribute
        {
            var attributeType = typeof(T);
            var attributeNameRegex = new Regex($"^({attributeType.Namespace})?{attributeType.GetShortAttributeName()}(Attribute)?$");

            return attributeNameRegex.IsMatch(attributeSyntax.Name.ToString());
        }
    }
}
