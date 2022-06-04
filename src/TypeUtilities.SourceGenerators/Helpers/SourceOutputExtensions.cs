using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class SourceOutputExtensions
    {
        public static void WriteType(
            this SourceProductionContext context,
            TypeDeclarationSyntax typeDeclarationSyntax,
            IEnumerable<ISymbol?> members,
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

                if (member is IPropertySymbol prop)
                {
                    var propType = prop.Type.ToDisplayString();
                    var propName = prop.Name;

                    sourceBuilder.AddLine($"{accessibility} {propType} {propName}" + " { get; set; }");
                    continue;
                }

                if (member is IFieldSymbol field)
                {
                    var propType = field.Type.ToDisplayString();
                    var propName = field.Name;

                    sourceBuilder.AddLine($"{accessibility} {propType} {propName};");
                }
            }

            var source = sourceBuilder.Build();

            context.AddSource(outputFileName, SourceText.From(source, Encoding.Unicode));
        }
    }
}
