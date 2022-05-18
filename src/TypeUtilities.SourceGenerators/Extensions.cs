using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;

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

    public static string GetFullName(this TypeDeclarationSyntax typeNode, CancellationToken token = default)
    {
        var @namespace = typeNode.GetNamespace(token);
        var typeName = typeNode.Identifier.ToString();
        return string.IsNullOrEmpty(@namespace) ? typeName : $"{@namespace}.{typeName}";
    }

    public static bool TryFindParent<T>(this SyntaxNode node, [NotNullWhen(true)] out T? found, CancellationToken token = default)
        where T : SyntaxNode
    {
        var current = node;
        while (current is not null)
        {
            token.ThrowIfCancellationRequested();

            if (current is T resolved)
            {
                found = resolved;
                return true;
            }

            current = current.Parent;
        }

        found = default;
        return false;
    }

    public static T GetParamValue<T>(this IDictionary<string, TypedConstant> dict, string name, T defaultValue)
    {
        if (dict.ContainsKey(name) && dict[name].Value is not null)
        {
            return dict[name].Value is T resolved ? resolved : defaultValue;
        }
        return defaultValue;
    }

    public static T[] GetParamValues<T>(this IDictionary<string, TypedConstant> dict, string name, T[] defaultValue)
        where T : class
    {
        if (dict.ContainsKey(name))
        {
            var values = dict[name].Values
                .Select(x => (x.Value as T)!)
                .Where(x => x is not null)
                .ToArray();

            return values.Length > 0 ? values! : defaultValue;
        }
        return defaultValue;
    }

    public static ISymbol? GetMember(this ITypeSymbol typeSymbol, string name, bool includeBase, CancellationToken token)
    {
        var directMember = typeSymbol.GetMembers(name).FirstOrDefault();

        if (directMember is not null || !includeBase)
            return directMember;

        var currSymbol = typeSymbol.BaseType;
        while (currSymbol is not null)
        {
            token.ThrowIfCancellationRequested();

            var member = currSymbol.GetMembers(name).FirstOrDefault();
            if (member is not null)
                return member;

            currSymbol = currSymbol.BaseType;
        }

        return null;
    }
}
