using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using TypeUtilities.SourceGenerators.Analyzer;
using TypeUtilities.SourceGenerators.Models;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class IncrementalValuesProviderExtensions
    {
        public static IncrementalValuesProvider<AttributeSyntax> CreateAttributeSyntaxProvider<T>(this SyntaxValueProvider syntaxProvider)
            where T : Attribute
        {
            var attributeType = typeof(T);

            return syntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node is AttributeSyntax attr && attr.Is<T>(),
                transform: (ctx, ct) =>
                {
                    try
                    {
                        var attributeSyntax = (AttributeSyntax)ctx.Node;
                        var attributeSymbolInfo = ctx.SemanticModel.GetSymbolInfo(attributeSyntax, ct);

                        var symbol = attributeSymbolInfo.Symbol ?? attributeSymbolInfo.CandidateSymbols.FirstOrDefault();

                        if (symbol is not IMethodSymbol attributeSymbol)
                            return null;

                        var attributeFullName = attributeSymbol.ContainingType.ToDisplayString();

                        if (attributeFullName != attributeType.FullName)
                            return null;

                        return attributeSyntax;
                    }
                    catch
                    {
                    // TODO: diagnostics?
                    return null;
                    }

                }).WhereNotNull();
        }

        public static IncrementalValuesProvider<InvocationExpressionSyntax> CreateInvocationExpressionProvider(this SyntaxValueProvider syntaxProvider, string methodName, string[] targetTypeNames)
        {
            return syntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) =>
                    node is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax invokeMember && invokeMember.Name.Identifier.ToString() == methodName &&
                    invokeMember.Expression is TypeSyntax targetType && targetTypeNames.Contains(targetType.ToString()),
                transform: (ctx, token) => (InvocationExpressionSyntax)ctx.Node);
        }

        public static IncrementalValuesProvider<(InvocationExpressionSyntax Invocation, IdentifierNameSyntax InstanceType)> CreateInvocationExpressionProvider(this SyntaxValueProvider syntaxProvider, string methodName, int argsCount)
        {
            return syntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) =>
                    node is InvocationExpressionSyntax invocation && invocation.ArgumentList.Arguments.Count == argsCount &&
                    invocation.Expression is MemberAccessExpressionSyntax invokeMember && invokeMember.Name.Identifier.ValueText == methodName,
                transform: (ctx, token) =>
                {
                    var invocationExpression = (InvocationExpressionSyntax)ctx.Node;
                    var instance = ((MemberAccessExpressionSyntax)invocationExpression.Expression).Expression;
                    if (instance is not IdentifierNameSyntax instanceTypeIdentifier)
                        return default;
                    return (invocationExpression, instanceTypeIdentifier);
                })
                .Where(x => x != default);
        }

        public static IncrementalValuesProvider<T> WhereNotNull<T>(this IncrementalValuesProvider<T?> provider)
        {
            return provider.Where(x => x is not null)!;
        }

        public static IncrementalValuesProvider<TOut> SelectFromSyntax<TIn, TOut>(this IncrementalValuesProvider<TIn> provider, Func<TIn, CancellationToken, ISyntaxResult<TOut>> selectFn, IncrementalGeneratorInitializationContext context)
            where TIn : SyntaxNode
        {
            return provider.Select((syntax, token) =>
            {
                try
                {
                    return selectFn(syntax, token);
                }
                catch (Exception ex)
                {
                    return SyntaxResult.Fail<TOut>(Diagnostics.InternalError(ex, syntax));
                }
            }).Unwrap(context);
        }

        public static IncrementalValuesProvider<T> Unwrap<T>(this IncrementalValuesProvider<ISyntaxResult<T>> provider, IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(provider.Where(r => r.IsDiagnostic), static (ctx, result) => ctx.ReportDiagnostic(result.Diagnostic!));
            return provider.Where(x => x.IsSuccess).Select((x, ct) => x.Result!);
        }
    }
}
