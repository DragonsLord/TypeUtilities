using TypeUtilities.Abstractions;

namespace TypeUtilities
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class MapTemplateAttribute : Attribute
    {
        public string MemberDeclarationFormat { get; set; } = MemberDeclarationFormats.Source;
        public bool IncludeBaseTypes { get; set; } = false;
        public MemberAccessibilityFlags MemberAccessibilitySelection { get; set; } = MemberAccessibilityFlags.Public;
        public MemberScopeFlags MemberScopeSelection { get; set; } = MemberScopeFlags.Instance;
        public MemberKindFlags MemberKindSelection { get; set; } = MemberKindFlags.AnyProperty;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MemberMappingAttribute : Attribute { }

    public class MemberInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public MemberAccessibilityFlags Accessibility { get; set; }
        public MemberScopeFlags Scope { get; set; }
        public MemberKindFlags Kind { get; set; }

        private MemberInfo(string name, Type type, MemberAccessibilityFlags accessibility, MemberScopeFlags scope, MemberKindFlags kind)
        {
            Name = name;
            Type = type;
            Accessibility = accessibility;
            Scope = scope;
            Kind = kind;
        }

        public static MemberInfo Create<T>(string name, MemberAccessibilityFlags accessibility, MemberScopeFlags scope, MemberKindFlags kind)
        {
            return new MemberInfo(name, typeof(T), accessibility, scope, kind);
        }
    }
}
