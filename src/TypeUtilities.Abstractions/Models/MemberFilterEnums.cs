namespace TypeUtilities.Abstractions
{
    [Flags]
    public enum MemberAccessibilityFlags
    {
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

        Any = Public | Private | Protected
    }

    [Flags]
    public enum MemberScopeFlags
    {
        /// <summary>
        /// Include members declared on the instance level (non-static members)
        /// </summary>
        Instance = 1 << 0,

        /// <summary>
        /// Include static members
        /// </summary>
        Static = 1 << 1,
        // internal?

        Any = Instance | Static
    }

    public enum MemberKindFlags
    {
        /// <summary>
        /// Include properties with get accessor but without set
        /// </summary>
        ReadonlyProperty = 1 << 0,

        /// <summary>
        /// Include properties with get accessor but without get
        /// </summary>
        WriteonlyProperty = 1 << 1,

        /// <summary>
        /// Include properties with both get and set accessors
        /// </summary>
        GetSetProperty = 1 << 2,

        /// <summary>
        /// Include properties with at least get accessor
        /// </summary>
        GetProperty = GetSetProperty | ReadonlyProperty,

        /// <summary>
        /// Include properties with at least set accessor
        /// </summary>
        SetProperty = GetSetProperty | WriteonlyProperty,

        /// <summary>
        /// Include properties with any accessors
        /// </summary>
        AnyProperty = GetSetProperty | ReadonlyProperty | WriteonlyProperty,


        /// <summary>
        /// Include fields
        /// </summary>
        WritableField = 1 << 3,

        /// <summary>
        /// Include readonly fields
        /// </summary>
        ReadonlyField = 1 << 4,

        /// <summary>
        /// Include writable and readolny fields
        /// </summary>
        AnyField = WritableField | ReadonlyField
    }
}
