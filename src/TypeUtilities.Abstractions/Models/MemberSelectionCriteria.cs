namespace TypeUtilities.Abstractions
{
    // This approach currently does not allow to filter out readonly or writeonly properties
    [Flags]
    public enum MemberSelectionFlags
    {
        Default = 0,
        /// <summary>
        /// Include Public members
        /// </summary>
        Public = 1 << 0,
        /// <summary>
        /// Include Private members
        /// </summary>
        Private = 1 << 1,
        /// <summary>
        /// Include Protected members
        /// </summary>
        Protected = 1 << 2,
        // internal?

        /// <summary>
        /// Include members declared on the instance level (non-static members)
        /// </summary>
        Instance = 1 << 3,
        /// <summary>
        /// Include static members
        /// </summary>
        Static = 1 << 4,

        /// <summary>
        /// Include properties with get accessor
        /// </summary>
        GetProperty = 1 << 5,
        /// <summary>
        /// Include properties with get accessor
        /// </summary>
        SetProperty = 1 << 6,
        /// <summary>
        /// Include properties with both get and set accessors
        /// </summary>
        GetSetProperty = 1 << 7,
        /// <summary>
        /// Include fields
        /// </summary>
        WritableField = 1 << 8,
        /// <summary>
        /// Include readonly fields
        /// </summary>
        ReadonlyField = 1 << 9,

        /// <summary>
        /// Include members declared at the level of the target type
        /// </summary>
        Declared = 1 << 10,

        /// <summary>
        /// Include inherited members
        /// </summary>
        Inherited = 1 << 11,

        /// <summary>
        /// Include members with any accessibility (public, private or protected)
        /// </summary>
        AnyAccessibility = Public | Private | Protected,
        /// <summary>
        /// Include both static and instance members
        /// </summary>
        AnyScope = Instance | Static,
    }

    public static class MemberSelections
    {
        /// <summary>
        /// Include all members
        /// </summary>
        public const MemberSelectionFlags All =
            MemberSelectionFlags.AnyAccessibility   |
            MemberSelectionFlags.AnyScope           |
            MemberSelectionFlags.Declared           |
            MemberSelectionFlags.Inherited          |
            MemberSelectionFlags.GetProperty        |
            MemberSelectionFlags.SetProperty        |
            MemberSelectionFlags.WritableField;

        /// <summary>
        /// Include declated instance properties.
        /// This is default value
        /// </summary>
        public const MemberSelectionFlags DeclaredInstanceProperties =
            MemberSelectionFlags.AnyAccessibility   |
            MemberSelectionFlags.Instance           |
            MemberSelectionFlags.Declared           |
            MemberSelectionFlags.GetProperty        |
            MemberSelectionFlags.SetProperty;

        /// <summary>
        /// Include any members declared at the level of the target type
        /// </summary>
        public const MemberSelectionFlags DeclaredMembers =
            MemberSelectionFlags.AnyAccessibility   |
            MemberSelectionFlags.AnyScope           |
            MemberSelectionFlags.Declared           |
            MemberSelectionFlags.GetProperty        |
            MemberSelectionFlags.SetProperty        |
            MemberSelectionFlags.WritableField;

        /// <summary>
        /// Include public declared properties with both gettter and setter defined
        /// </summary>
        public const MemberSelectionFlags PublicDeclaredGetSetProperties =
            MemberSelectionFlags.Public             |
            MemberSelectionFlags.Instance           |
            MemberSelectionFlags.Declared           |
            MemberSelectionFlags.GetSetProperty;
    }
}
