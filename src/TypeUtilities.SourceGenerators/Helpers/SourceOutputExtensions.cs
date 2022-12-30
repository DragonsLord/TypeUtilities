using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

using static TypeUtilities.Abstractions.MemberDeclarationFormats;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class MemberFormat
    {
        public static string ApplyFormat(string format, string accessibility, string scope, string fieldAccess, string type, string name, string accessors)
        {
            return format
                .Replace(Tokens.Accessibility, accessibility)
                .Replace(Tokens.Scope, scope)
                .Replace(Tokens.FieldAccess, fieldAccess)
                .Replace(Tokens.Type, type)
                .Replace(Tokens.Name, name)
                .Replace(Tokens.Accessors, accessors);
        }

        public static string? FormatDeclaration(ISymbol? symbol, string format)
        {
            if (symbol is null)
                return null;

            var accessibility = symbol.DeclaredAccessibility.ToString().ToLower();
            var scope = symbol.IsStatic ? " static" : string.Empty;

            if (symbol is IPropertySymbol prop)
            {
                var type = prop.Type.ToDisplayString();
                var name = prop.Name;
                var accessors = " " + prop.GetAccessors();

                return ApplyFormat(format, accessibility, scope, string.Empty, type, name, accessors);
            }

            if (symbol is IFieldSymbol field)
            {
                var type = field.Type.ToDisplayString();
                var name = field.Name;
                var fieldAccess = field.IsReadOnly ? " readonly" : string.Empty;

                return ApplyFormat(format, accessibility, scope, fieldAccess, type, name, ";");
            }

            return null;
        }
    }

    internal static class PrintableMember
    {
        public static Action<SourceBuilder> FromSymbol(ISymbol? symbol, string format)
        {
            var declaration = MemberFormat.FormatDeclaration(symbol, format);

            return sourceBuilder =>
            {
                if (declaration is not null)
                {
                    sourceBuilder.AddLine(declaration);
                }
            };
        }
    }

    //TODO: remove and use SourceBuilder directly
    internal static class SourceOutputExtensions
    {
        public static void WriteType(
            this SourceProductionContext context,
            TypeDeclarationSyntax typeDeclarationSyntax,
            IEnumerable<ISymbol?> members,
            string memberDeclarationFormat,
            string outputFileName,
            CancellationToken token = default)
        {
            WriteType(context, typeDeclarationSyntax, members.Select(x => PrintableMember.FromSymbol(x, memberDeclarationFormat)), outputFileName, token);
        }

        public static void WriteType(
            this SourceProductionContext context,
            TypeDeclarationSyntax typeDeclarationSyntax,
            IEnumerable<Action<SourceBuilder>> printableMembers,
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

            sourceBuilder.AddTypeDeclaration(typeDeclarationSyntax.Modifiers, typeDeclarationSyntax.Keyword, typeDeclarationSyntax.Identifier, typeDeclarationSyntax.TypeParameterList, typeDeclarationSyntax.BaseList);

            foreach (var printMember in printableMembers)
            {
                printMember(sourceBuilder);
            }

            var source = sourceBuilder.Build();

            context.AddSource(outputFileName, SourceText.From(source, Encoding.Unicode));
        }
    }
}
