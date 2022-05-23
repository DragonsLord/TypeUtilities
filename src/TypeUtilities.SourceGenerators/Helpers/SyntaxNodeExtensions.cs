﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class SyntaxNodeExtensions
    {
        public static bool TryCompileNamedTypeSymbol(this SyntaxNode syntax, Compilation compilation, CancellationToken token, [NotNullWhen(true)] out INamedTypeSymbol? result)
        {
            result = null;
            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

            var decleredSymbol = semanticModel.GetDeclaredSymbol(syntax, token);
            if (decleredSymbol is not INamedTypeSymbol namedTypeSymbol)
                return false;

            result = namedTypeSymbol;
            return true;
        }

        public static ISymbol? GetSymbol(this SyntaxNode syntaxNode, Compilation compilation, CancellationToken token)
        {
            // TODO: include candidate symbols?
            return compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetSymbolInfo(syntaxNode, token).Symbol;
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
}