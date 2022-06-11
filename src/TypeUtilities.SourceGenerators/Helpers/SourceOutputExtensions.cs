using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

using static TypeUtilities.Abstractions.MemberDeclarationFormats;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class SourceOutputExtensions
    {
        private static string ApplyFormat(string format, string accessibility, string scope, string fieldAccess, string type, string name, string accessors)
        {
            return format
                .Replace(Tokens.Accessibility, accessibility)
                .Replace(Tokens.Scope, scope)
                .Replace(Tokens.FieldAccess, fieldAccess)
                .Replace(Tokens.Type, type)
                .Replace(Tokens.Name, name)
                .Replace(Tokens.Accessors, accessors);
        }

        public static void WriteType(
            this SourceProductionContext context,
            TypeDeclarationSyntax typeDeclarationSyntax,
            IEnumerable<ISymbol?> members,
            string memberDeclarationFormat,
            string outputFileName,
            CancellationToken token = default)
        {
            var sourceBuilder = new SourceBuilder();

            var @namespace = typeDeclarationSyntax.GetNamespace(token);

            if (@namespace is not null)
            {
                var fileScoped = @namespace is FileScopedNamespaceDeclarationSyntax;
                sourceBuilder.AddNamespace(@namespace.Name.ToString(), fileScoped);
            }

            sourceBuilder.AddTypeDeclaration(typeDeclarationSyntax.Modifiers, typeDeclarationSyntax.Keyword, typeDeclarationSyntax.Identifier);

            foreach (var member in members)
            {
                if (member is null)
                    continue;

                var accessibility = member.DeclaredAccessibility.ToString().ToLower();
                var scope = member.IsStatic ? " static" : string.Empty;

                if (member is IPropertySymbol prop)
                {
                    var type = prop.Type.ToDisplayString();
                    var name = prop.Name;
                    var accessors = " " + prop.GetAccessors();

                    sourceBuilder.AddLine(ApplyFormat(memberDeclarationFormat, accessibility, scope, string.Empty, type, name, accessors));
                    continue;
                }

                if (member is IFieldSymbol field)
                {
                    var type = field.Type.ToDisplayString();
                    var name = field.Name;
                    var fieldAccess = field.IsReadOnly ? " readonly" : string.Empty;

                    sourceBuilder.AddLine(ApplyFormat(memberDeclarationFormat, accessibility, scope, fieldAccess, type, name, ";"));
                }
            }

            var source = sourceBuilder.Build();

            context.AddSource(outputFileName, SourceText.From(source, Encoding.Unicode));
        }
    }
}
