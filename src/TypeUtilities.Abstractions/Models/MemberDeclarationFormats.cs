namespace TypeUtilities.Abstractions;

public static class MemberDeclarationFormats
{
    public static class Tokens
    {
        public const string Accessibility = "{accessibility}";
        public const string Scope = "{scope}";
        public const string FieldAccess = "{fieldAccess}";
        public const string Type = "{type}";
        public const string Name = "{name}";
        public const string Accessors = "{accessors}";
    }

    public const string Source = $"{Tokens.Accessibility}{Tokens.Scope}{Tokens.FieldAccess} {Tokens.Type} {Tokens.Name}{Tokens.Accessors}";

    public const string GetSetProp = $"{Tokens.Accessibility} {Tokens.Type} {Tokens.Name} {{ get; set; }}";
    public const string GetProp = $"{Tokens.Accessibility} {Tokens.Type} {Tokens.Name} {{ get; }}";
    public const string SetProp = $"{Tokens.Accessibility} {Tokens.Type} {Tokens.Name} {{ set; }}";
    public const string PublicGetSetProp = $"public {Tokens.Type} {Tokens.Name} {{ get; set; }}";

    public const string Field = $"{Tokens.Accessibility}{Tokens.FieldAccess} {Tokens.Type} {Tokens.Name};";
    public const string PublicField = $"public{Tokens.FieldAccess} {Tokens.Type} {Tokens.Name};";
}
