using TypeUtilities;
using static TypeUtilities.Abstractions.MemberDeclarationFormats;

namespace DemoApp.MapTemplateDemo
{
    public record WrapContainer<T>(string Name, T Value);

    [MapTemplate(MemberDeclarationFormat = $"{Tokens.Accessibility} DemoApp.MapTemplateDemo.WrapContainer<{Tokens.Type}> {Tokens.Name}Wrap{Tokens.Accessors}")]
    public class MapTemplate<T>
    {
        [MemberMapping]
        protected static WrapContainer<TProp> MapMember<TProp>(MemberInfo member, TProp value)
            => new WrapContainer<TProp>(member.Name, value);
    }
}
