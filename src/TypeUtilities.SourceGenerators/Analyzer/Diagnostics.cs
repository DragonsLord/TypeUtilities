using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TypeUtilities.SourceGenerators.Analyzer
{
    internal static class Diagnostics
    {
        private static readonly DiagnosticDescriptor _internalError = new(
            id: "TU000",
            title: "Internal Error",
            messageFormat: "TypeUtilities generator failed with \"{0}\"",
            description: "Unexpected error happend during TypeUtilities Source Generator execution",
            category: "TypeUtilities.InternalError",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static Diagnostic InternalError(Exception ex, SyntaxNode? syntax)
            => Diagnostic.Create(_internalError, syntax?.GetLocation() ?? Location.None, ex.Message);

        internal static Diagnostic InternalError(Exception ex, params Location[] locations)
            => Diagnostic.Create(_internalError, locations.FirstOrDefault(), locations.Skip(1), ex.Message);


        private static readonly DiagnosticDescriptor _missingPartialModifier = new(
            id: "TU001",
            title: "Missing Partial Modifier",
            messageFormat: "{0} should have partial modifier",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static Diagnostic MissingPartialModifier(TypeDeclarationSyntax syntax)
            => Diagnostic.Create(_missingPartialModifier, syntax?.Identifier.GetLocation() ?? Location.None, syntax?.Identifier.ToString() ?? string.Empty);

        public static Diagnostic MissingPartialModifier(string targetTypeName, Location location)
            => Diagnostic.Create(_missingPartialModifier, location, targetTypeName);


        private static readonly DiagnosticDescriptor _noMappedMembers = new(
            id: "TU002",
            title: "No mapped members",
            messageFormat: "Specified member selection flags doesn't yield any members from the {0} to map",
            description: "Specified member selection flags doesn't yield any members to map.",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static Diagnostic NoMappedMembers(INamedTypeSymbol sourceType, Location location)
            => Diagnostic.Create(_noMappedMembers, location, sourceType.Name);


        private static readonly DiagnosticDescriptor _missingMembersToOmit = new(
            id: "TU003",
            title: "Missing members to omit",
            messageFormat: "Members {0} specified to be omitted are not present in the {1} selection",
            description: "Some fields specified to be omitted are not present in the selection.",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static Diagnostic MissingMembersToOmit(INamedTypeSymbol sourceType, IEnumerable<string> members, Location location)
            => Diagnostic.Create(_missingMembersToOmit, location, string.Join(", ", members), sourceType.Name);


        private static readonly DiagnosticDescriptor _missingMembersToPick = new(
            id: "TU004",
            title: "Missing members to pick",
            messageFormat: "Members {0} are not present in the {1} selection and will be missing",
            description: "Some fields specified to be picked are not present in the selection.",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static Diagnostic MissingMembersToPick(INamedTypeSymbol sourceType, IEnumerable<string> members, Location location)
            => Diagnostic.Create(_missingMembersToPick, location, string.Join(", ", members), sourceType.Name);


        private static readonly DiagnosticDescriptor _missingTypeParameter = new(
            id: "TU005",
            title: "Missing type parameter",
            messageFormat: "Missing a Type parameter in the {0} type",
            description: "Map Tamplate type should have a single type parameter",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        //TODO: test
        public static Diagnostic MissingTypeParameter(TypeDeclarationSyntax templateType)
            => Diagnostic.Create(_missingTypeParameter, templateType.Identifier.GetLocation(), templateType.Identifier);


        private static readonly DiagnosticDescriptor _moreThenOneTypeParameter = new(
            id: "TU006",
            title: "More then one type parameter",
            messageFormat: "{0} template type can have only a single type parameter",
            description: "Map Tamplate type should have a single type parameter",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        //TODO: test
        public static Diagnostic MoreThenOneTypeParameter(TypeDeclarationSyntax templateType)
            => Diagnostic.Create(_moreThenOneTypeParameter, templateType.TypeParameterList?.GetLocation(), templateType.Identifier);


        private static readonly DiagnosticDescriptor _missingMemberMapping = new(
            id: "TU007",
            title: "Missing member mapping",
            messageFormat: "Missing a Member Mapping function for the {0} template type",
            description: "Map Tamplate type should have a single member mapping function",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        //TODO: test
        public static Diagnostic MissingMemberMapping(TypeDeclarationSyntax templateType)
            => Diagnostic.Create(_missingMemberMapping, templateType.GetLocation(), templateType.Identifier);


        private static readonly DiagnosticDescriptor _moreThenOneMemberMapping = new(
            id: "TU008",
            title: "More then one member mapping",
            messageFormat: "{0} template type can have only a single member mapping parameter",
            description: "Map Tamplate type should have a single member mapping function",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        //TODO: test
        public static Diagnostic MoreThenOneMemberMapping(TypeDeclarationSyntax templateType, MemberDeclarationSyntax[] mappings)
            => Diagnostic.Create(_moreThenOneMemberMapping, templateType.GetLocation(), mappings.Select(x => x.GetLocation()), templateType.Identifier);

        private static readonly DiagnosticDescriptor _incorrectMemberMappingSignature = new(
            id: "TU009",
            title: "Incorrect member mapping signature",
            messageFormat: "Member mapping {0} should have '{0}<T>(MemberInfo memberInfo, T value)' signature",
            description: "Member mapping should have a correct signature",
            category: "TypeUtilities",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        //TODO: test
        public static Diagnostic IncorrectMemberMappingSignature(MethodDeclarationSyntax methodDeclaration)
            => Diagnostic.Create(_incorrectMemberMappingSignature, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.ValueText);
    }
}
