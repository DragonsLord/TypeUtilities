using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TypeUtilities.SourceGenerators.Analyzer
{
    internal static class DiagnosticsExtensions
    {
        public static void ReportInternalError(this SourceProductionContext ctx, Exception ex, SyntaxNode? syntax)
            => ctx.ReportDiagnostic(Diagnostics.InternalError(ex, syntax));
        public static void ReportInternalError(this SourceProductionContext ctx, Exception ex, params Location[] locations)
            => ctx.ReportDiagnostic(Diagnostics.InternalError(ex, locations));


        public static void ReportMissingPartialModifier(this SourceProductionContext ctx, TypeDeclarationSyntax syntax)
            => ctx.ReportDiagnostic(Diagnostics.MissingPartialModifier(syntax));
        public static void ReportMissingPartialModifier(this SourceProductionContext ctx, string targetTypeName, Location location)
            => ctx.ReportDiagnostic(Diagnostics.MissingPartialModifier(targetTypeName, location));


        public static void ReportNoMappedMembers(this SourceProductionContext ctx, INamedTypeSymbol sourceType, Location location)
            => ctx.ReportDiagnostic(Diagnostics.NoMappedMembers(sourceType, location));


        public static void ReportMissingMembersToOmit(this SourceProductionContext ctx, INamedTypeSymbol sourceType, IEnumerable<string> members, Location location)
            => ctx.ReportDiagnostic(Diagnostics.MissingMembersToOmit(sourceType, members, location));


        public static void ReportMissingMembersToPick(this SourceProductionContext ctx, INamedTypeSymbol sourceType, IEnumerable<string> members, Location location)
            => ctx.ReportDiagnostic(Diagnostics.MissingMembersToPick(sourceType, members, location));
    }
}
