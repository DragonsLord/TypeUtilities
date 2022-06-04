﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TypeUtilities.SourceGenerators.Diagnostics
{
    internal static class DiagnosticsExtensions
    {
        private static readonly DiagnosticDescriptor InternalError = new(
            id: "TU000",
            title: "Internal Error",
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

        private static readonly DiagnosticDescriptor MissingPartialModifier = new(
            id: "TU001",
            title: "Missing Partial Modifier",
            messageFormat: "{0} should have partial modifier",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static void ReportMissingPartialModifier(this SourceProductionContext ctx, TypeDeclarationSyntax syntax) =>
            ReportMissingPartialModifier(ctx, syntax.Identifier.ToString(), syntax?.Identifier.GetLocation() ?? Location.None);

        public static void ReportMissingPartialModifier(this SourceProductionContext ctx, string typeName, Location location)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(MissingPartialModifier, location, typeName));
        }
    }
}