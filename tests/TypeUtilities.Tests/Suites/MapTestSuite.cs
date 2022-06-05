using System.Threading.Tasks;
using TypeUtilities.Abstractions;
using TypeUtilities.Tests.Fixture;
using Xunit;

namespace TypeUtilities.Tests.Suites
{
    public abstract class MapTestSuite<T>
        where T : MapAttribute
    {
        private readonly CompilationFixture _fixture;
        private readonly string _attributeName;
        private readonly string _additionCtorArgs;

        public MapTestSuite(CompilationFixture compilationFixture, string attributeName, string additionCtorArgs = "")
        {
            _fixture = compilationFixture;
            _attributeName = attributeName;
            _additionCtorArgs = additionCtorArgs;
        }

        [Theory]
        [InlineData("public", "class")]
        [InlineData("internal", "class")]
        [InlineData("private", "class")]
        [InlineData("public", "struct")]
        [InlineData("internal", "struct")]
        [InlineData("private", "struct")]
        [InlineData("public", "record")]
        [InlineData("internal", "record")]
        [InlineData("private", "record")]
        public Task ShouldAddMappedMembers(string accessibility, string typeKind)
        {
            // The source code to test
            var source = @"
using System;
using TypeUtilities;

namespace MapTests;

public class SourceType
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

" + $"[{_attributeName}(typeof(SourceType){_additionCtorArgs})]\n" +
$"{accessibility} partial {typeKind} TargetType" +
@"{
    public double AdditionalValue { get; set; }
}
";
            return Verify(source, accessibility, typeKind);
        }

        [Theory]
        [InlineData(MemberDeclarationFormats.Source)]
        [InlineData(MemberDeclarationFormats.PublicGetSetProp)]
        [InlineData(MemberDeclarationFormats.Field)]
        [InlineData(MemberDeclarationFormats.GetProp)]
        [InlineData("{accessibility} {type} Mapped{name} { get; set; }")]
        public Task ShouldUseMemberFormat(string format)
        {
            // The source code to test
            var source = @"
using System;
using TypeUtilities;

namespace MapTests;

public class SourceType
{
    public Guid Id { get; }
    public int Value { get; set; }
    protected DateTime Created;
}

" + $"[{_attributeName}(typeof(SourceType){_additionCtorArgs}, MemberDeclarationFormat = \"{format}\"))]\n" +
"public partial class TargetType{}";
            return Verify(source, format);
        }

        [Fact] //TODO: Theory
        public Task ShouldIncludeBaseField()
        {
            // The source code to test
            var source = @"
using System;
using TypeUtilities;

namespace MapTests;

public class BaseType
{
    public int Count { get; }
    public double Score { get; }
}

public class SourceType : BaseType
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

" + $"[{_attributeName}(typeof(SourceType){_additionCtorArgs})]\n" +
@"public partial class DoNotIncludeByDefault {}

" + $"[{_attributeName}(typeof(SourceType){_additionCtorArgs}, IncludeBaseTypes = false)]\n" +
@"public partial class DoNotIncludeExplicitly {}

" + $"[{_attributeName}(typeof(SourceType){_additionCtorArgs}, IncludeBaseTypes = true)]\n" +
@"public partial class Include {}";

            return Verify(source);
        }

        private Task Verify(string source)
        {
            return _fixture.Verify(source, $"{_attributeName}/MapSuite");
        }

        private Task Verify(string source, params string[] parameters)
        {
            return _fixture.Verify(source, $"{_attributeName}/MapSuite", parameters);
        }
    }
}
