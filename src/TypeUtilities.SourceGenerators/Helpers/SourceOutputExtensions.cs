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
            INamespaceSymbol @namespace, // Could be inferred from typeDeclarationSyntax
            TypeDeclarationSyntax typeDeclarationSyntax,
            IEnumerable<ISymbol?> members,
            string outputFileName)
        {
            var sourceBuilder = new StringBuilder();

            if (!@namespace.IsGlobalNamespace)
            {
                sourceBuilder.AppendLine($"namespace {@namespace.ToDisplayString()};\n");
            }

            sourceBuilder.AppendLine($"{typeDeclarationSyntax.Modifiers} {typeDeclarationSyntax.Keyword} {typeDeclarationSyntax.Identifier}");
            sourceBuilder.AppendLine("{");

            foreach (var member in members)
            {
                if (member is null)
                    continue;

                var accessibility = member.DeclaredAccessibility.ToString().ToLower();

                if (member is IPropertySymbol prop)
                {
                    var propType = prop.Type.ToDisplayString();
                    var propName = prop.Name;

                    sourceBuilder.AppendLine($"    {accessibility} {propType} {propName}" + " { get; set; }");
                    continue;
                }

                if (member is IFieldSymbol field)
                {
                    var propType = field.Type.ToDisplayString();
                    var propName = field.Name;

                    sourceBuilder.AppendLine($"    {accessibility} {propType} {propName};");
                }
            }

            sourceBuilder.AppendLine("}");

            var source = sourceBuilder.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

            context.AddSource(outputFileName, SourceText.From(source, Encoding.Unicode));
        }
    }
}
