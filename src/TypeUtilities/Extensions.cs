using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TypeUtilities;

internal static class Extensions
{
    public static IncrementalValuesProvider<T> SkipNulls<T>(this IncrementalValuesProvider<T?> provider)
    {
        return provider.Where(x => x is not null).Select((x, _) => x!);
    }

    public static string GetNamespace(this SyntaxNode node, CancellationToken token = default)
    {
        var currNode = node;
        while (currNode is not BaseNamespaceDeclarationSyntax)
        {
            token.ThrowIfCancellationRequested();

            currNode = currNode.Parent;
            if (currNode is null)
                return string.Empty;
        }
        var namespaceDeclaration = (BaseNamespaceDeclarationSyntax)currNode;
        return namespaceDeclaration.Name.ToString();
    }

    public static string GetFullTypeName(this TypeDeclarationSyntax typeNode, CancellationToken token = default)
    {
        var @namespace = typeNode.GetNamespace(token);
        var typeName = typeNode.Identifier.ToString();
        return string.IsNullOrEmpty(@namespace) ? typeName : $"{@namespace}.{typeName}";
    }
}
