using Microsoft.CodeAnalysis;

namespace TypeUtilities.SourceGenerators.Diagnostics
{
    internal static class DiagnosticsExtensions
    {
        private static readonly DiagnosticDescriptor InternalError = new(
            id: "TU000",
            title: "Type Utilities",
            messageFormat: "TypeUtilities generator failed with \"{0}\"",
            description: "Unexpected error happend during TypeUtilities Source Generator execution",
            category: "TypeUtilities.InternalError",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static void ReportInternalError(this SourceProductionContext ctx, Exception ex, SyntaxNode? syntax) =>
            ReportInternalError(ctx, ex, syntax?.GetLocation() ?? Location.None);

        public static void ReportInternalError(this SourceProductionContext ctx, Exception ex, params Location[] locations)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(InternalError, locations.FirstOrDefault(), locations.Skip(1), ex.Message));
        }
    }
}
