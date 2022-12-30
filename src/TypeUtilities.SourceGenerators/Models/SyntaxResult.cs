using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using TypeUtilities.SourceGenerators.Analyzer;

namespace TypeUtilities.SourceGenerators.Models
{
    internal interface ISyntaxResult<T>
    {
        [MemberNotNullWhen(true, nameof(IsSuccess))]
        T? Result { get; }

        [MemberNotNullWhen(true, nameof(IsDiagnostic))]
        Diagnostic? Diagnostic { get; } //TODO: IEnumerable<Diagnostic> ?

        bool IsSuccess { get; }

        bool IsDiagnostic { get; }

        //ISyntaxResult<TOut> Map<TOut>(Func<T, TOut> mapFn);
        //ISyntaxResult<TOut> Map<TOut>(Func<T, ISyntaxResult<TOut>> mapFn);
    }

    internal static class SyntaxResult
    {
        private class SyntaxResultImpl<T> : ISyntaxResult<T>
        {
            [MemberNotNullWhen(true, nameof(IsSuccess))]
            public T? Result { get; }

            [MemberNotNullWhen(true, nameof(IsDiagnostic))]
            public Diagnostic? Diagnostic { get; }

            public bool IsSuccess { get; }

            public bool IsDiagnostic { get; }

            public SyntaxResultImpl(T result)
            {
                Result = result;
                Diagnostic = null;
                IsSuccess = true;
                IsDiagnostic = false;
            }

            public SyntaxResultImpl(Diagnostic diagnostic)
            {
                Result = default;
                Diagnostic = diagnostic;
                IsSuccess = false;
                IsDiagnostic = true;
            }

            public SyntaxResultImpl()
            {
                Result = default;
                Diagnostic = null;
                IsSuccess = false;
                IsDiagnostic = false;
            }
        }

        public static ISyntaxResult<T> Ok<T>(T result) => new SyntaxResultImpl<T>(result);

        public static ISyntaxResult<T> Skip<T>() => new SyntaxResultImpl<T>();

        public static ISyntaxResult<T> Fail<T>(Diagnostic diagnostic) => new SyntaxResultImpl<T>(diagnostic);
    }

    internal static class SyntaxResultMonadExt
    {
        public static ISyntaxResult<TOut> Map<TIn, TOut>(this ISyntaxResult<TIn> source, Func<TIn, TOut> mapFn)
        {
            return source switch
            {
                { IsSuccess: true } okResult => SyntaxResult.Ok(mapFn(okResult.Result!)),
                { IsDiagnostic: true } diagnosticResult => SyntaxResult.Fail<TOut>(diagnosticResult.Diagnostic!),
                _ => SyntaxResult.Skip<TOut>()
            };
        }

        public static ISyntaxResult<TOut> Map<TIn, TOut>(this ISyntaxResult<TIn> source, Func<TIn, ISyntaxResult<TOut>> mapFn)
        {
            return source switch
            {
                { IsSuccess: true } okResult => mapFn(okResult.Result!),
                { IsDiagnostic: true } diagnosticResult => SyntaxResult.Fail<TOut>(diagnosticResult.Diagnostic!),
                _ => SyntaxResult.Skip<TOut>()
            };
        }

        public static ISyntaxResult<T> Where<T>(this ISyntaxResult<T> source, Func<T, bool> condition)
            => Map(source, result => condition(result) ? SyntaxResult.Ok(result) : SyntaxResult.Skip<T>());

        public static ISyntaxResult<T> Where<T>(this ISyntaxResult<T> source, Func<T, bool> condition, Func<T, Diagnostic> failureMsg)
            => Map(source, result => condition(result) ? SyntaxResult.Ok(result) : SyntaxResult.Fail<T>(failureMsg(result)));

        public static ISyntaxResult<T> Where<T>(this ISyntaxResult<T> source, Func<T, bool> condition, Diagnostic failureMsg)
            => Map(source, result => condition(result) ? SyntaxResult.Ok(result) : SyntaxResult.Fail<T>(failureMsg));

        public static ISyntaxResult<TOut> Select<TIn, TOut>(this ISyntaxResult<TIn> source, Func<TIn, TOut> mapFn)
            => Map(source, mapFn);

        public static ISyntaxResult<TOut> SelectMany<TIn, TOut>(this ISyntaxResult<TIn> source, Func<TIn, ISyntaxResult<TOut>> mapFn)
            => Map(source, mapFn);

        public static ISyntaxResult<TResult> SelectMany<TOuter, TInner, TResult>(this ISyntaxResult<TOuter> source, Func<TOuter, ISyntaxResult<TInner>> mapFn, Func<TOuter, TInner, TResult> resultFn)
            => Map(source, outer => mapFn(outer).Map(inner => resultFn(outer, inner)));

        public static ISyntaxResult<T> AsSyntaxResult<T>(this T result) => SyntaxResult.Ok(result);

        public static void Unwrap<T>(this ISyntaxResult<T> result, Action<T> action, SourceProductionContext context)
        {
            if (result.IsDiagnostic)
                context.ReportDiagnostic(result.Diagnostic!);

            if (result.IsSuccess)
                action(result.Result!);
        }

        public static IEnumerable<T> Unwrap<T>(this IEnumerable<ISyntaxResult<T>> results, SourceProductionContext context)
        {
            foreach (var dR in results.Where(r => r.IsDiagnostic))
            {
                context.ReportDiagnostic(dR.Diagnostic!);
            }

            return results.Where(x => x.IsSuccess).Select(x => x.Result!);
        }
    }
}
