using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public void ShouldAddMappedMembers(string accessibility, string typeKind)
        {
            var source = new StringBuilder()
                .AppendLine("using System;")
                .AppendLine("using TypeUtilities;")
                .AppendLine("namespace MapTests;")
                .AppendLine("public class SourceType")
                .AppendLine("{")
                .AppendLine("\tpublic Guid Id { get; set; }")
                .AppendLine("\tpublic int Value { get; set; }")
                .AppendLine("\tpublic DateTime Created { get; set; }")
                .AppendLine("}\n")
                .AppendLine($"[{_attributeName}(typeof(SourceType){_additionCtorArgs})]")
                .AppendLine($"{accessibility} partial {typeKind} TargetType")
                .AppendLine("{ public double AdditionalValue { get; set; } }")
                .ToString();

            var result = _fixture.Generate(source);

            var expected = new StringBuilder()
                .AppendLine("namespace MapTests;")
                .AppendLine($"{accessibility} partial {typeKind} TargetType")
                .AppendLine("{")
                .AppendLine("\tpublic System.Guid Id { get; set; }")
                .AppendLine("\tpublic int Value { get; set; }")
                .AppendLine("\tpublic System.DateTime Created { get; set; }")
                .AppendLine("}\n");

            result.ShouldHaveSingleSource($"TargetType.{_attributeName.ToLower()}.SourceType.g.cs", expected.ToString());
        }

        public static object[] MemberFormatData = new[] {
            new object[]
            {
                MemberDeclarationFormats.Source,
                new string[] {
                    "public System.Guid Id { get; }",
                    "public int Value { get; set; }",
                    "public System.DateTime Created;"
                }
            },
            new object[]
            {
                MemberDeclarationFormats.PublicGetSetProp,
                new string[] {
                    "public System.Guid Id { get; set; }",
                    "public int Value { get; set; }",
                    "public System.DateTime Created { get; set; }"
                }
            },
            new object[]
            {
                MemberDeclarationFormats.Field,
                new string[] {
                    "public System.Guid Id;",
                    "public int Value;",
                    "public System.DateTime Created;"
                }
            },
            new object[]
            {
                MemberDeclarationFormats.GetProp,
                new string[] {
                    "public System.Guid Id { get; }",
                    "public int Value { get; }",
                    "public System.DateTime Created { get; }"
                }
            },
            new object[]
            {
                "{accessibility} {type} Mapped{name} { get; set; }",
                new string[] {
                    "public System.Guid MappedId { get; set; }",
                    "public int MappedValue { get; set; }",
                    "public System.DateTime MappedCreated { get; set; }"
                }
            }
        };

        [Theory]
        [MemberData(nameof(MemberFormatData))]
        public void ShouldUseMemberFormat(string format, IEnumerable<string> expectedProps)
        {
            var source = new StringBuilder()
                .AppendLine("using System;")
                .AppendLine("using TypeUtilities;")
                .AppendLine("using TypeUtilities.Abstractions;")
                .AppendLine("namespace MapTests;")
                .AppendLine("public class SourceType")
                .AppendLine("{")
                .AppendLine("\tpublic Guid Id { get; }")
                .AppendLine("\tpublic int Value { get; set; }")
                .AppendLine("\tpublic DateTime Created;")
                .AppendLine("}\n")
                .AppendLine($"[{_attributeName}(typeof(SourceType){_additionCtorArgs},")
                .AppendLine($"MemberDeclarationFormat = \"{format}\",")
                .AppendLine($"MemberKindSelection = MemberKindFlags.AnyProperty | MemberKindFlags.WritableField,")
                .AppendLine($"MemberAccessibilitySelection = MemberAccessibilityFlags.Any))]")
                .AppendLine("public partial class TargetType {}")
                .ToString();

            var result = _fixture.Generate(source);

            var expected = new StringBuilder()
                .AppendLine("namespace MapTests;")
                .AppendLine("public partial class TargetType")
                .AppendLine("{");

            foreach (var prop in expectedProps)
            {
                expected.AppendLine($"\t{prop}");
            }
            expected.AppendLine("}\n");

            result.ShouldHaveSingleSource($"TargetType.{_attributeName.ToLower()}.SourceType.g.cs", expected.ToString());
        }

        [Fact]
        public void ShouldIncludeBaseField()
        {
            // The source code to test
            var source = new StringBuilder()
                .AppendLine("using System;")
                .AppendLine("using TypeUtilities;")
                .AppendLine("namespace MapTests;")
                .AppendLine("public class BaseType")
                .AppendLine("{")
                .AppendLine("\tpublic int Count { get; }")
                .AppendLine("\tpublic double Score { get; }")
                .AppendLine("}")
                .AppendLine("public class SourceType : BaseType")
                .AppendLine("{")
                .AppendLine("\tpublic Guid Id { get; }")
                .AppendLine("\tpublic int Value { get; set; }")
                .AppendLine("\tpublic DateTime Created { get; set; }")
                .AppendLine("}")
                .AppendLine($"[{_attributeName}(typeof(SourceType){_additionCtorArgs})]")
                .AppendLine("public partial class DoNotIncludeByDefault {}")
                .AppendLine($"[{_attributeName}(typeof(SourceType){_additionCtorArgs}, IncludeBaseTypes = false)]")
                .AppendLine("public partial class DoNotIncludeExplicitly {}")
                .AppendLine($"[{_attributeName}(typeof(SourceType){_additionCtorArgs}, IncludeBaseTypes = true)]")
                .AppendLine("public partial class Include {}")
                .ToString();

            var result = _fixture.Generate(source);

            result
                .ShouldHaveSourcesCount(3)
                .ShouldHaveSource($"DoNotIncludeByDefault.{_attributeName.ToLower()}.SourceType.g.cs", @"
namespace MapTests;

public partial class DoNotIncludeByDefault
{
	public System.Guid Id { get; }
	public int Value { get; set; }
	public System.DateTime Created { get; set; }
}")
                .ShouldHaveSource($"DoNotIncludeExplicitly.{_attributeName.ToLower()}.SourceType.g.cs", @"
namespace MapTests;

public partial class DoNotIncludeExplicitly
{
	public System.Guid Id { get; }
	public int Value { get; set; }
	public System.DateTime Created { get; set; }
}")
                                .ShouldHaveSource($"Include.{_attributeName.ToLower()}.SourceType.g.cs", @"
namespace MapTests;

public partial class Include
{
	public System.Guid Id { get; }
	public int Value { get; set; }
	public System.DateTime Created { get; set; }
	public int Count { get; }
	public double Score { get; }
}");
        }


        public static object[] MemberAccessibilityData = new[] {
            new object[]
            {
                "MemberAccessibilityFlags.Public",
                new string[] {
                    "public string PublicProp { get; set; }"
                }
            },
            new object[]
            {
                "MemberAccessibilityFlags.Protected",
                new string[] {
                    "protected string ProtectedProp { get; set; }"
                }
            },
            new object[]
            {
                "MemberAccessibilityFlags.Private",
                new string[] {
                    "private string PrivateProp { get; set; }"
                }
            },
            new object[]
            {
                "MemberAccessibilityFlags.Public | MemberAccessibilityFlags.Protected",
                new string[] {
                    "public string PublicProp { get; set; }",
                    "protected string ProtectedProp { get; set; }"
                }
            },
            new object[]
            {
                "MemberAccessibilityFlags.Public | MemberAccessibilityFlags.Private",
                new string[] {
                    "public string PublicProp { get; set; }",
                    "private string PrivateProp { get; set; }"
                }
            },
            new object[]
            {
                "MemberAccessibilityFlags.Protected | MemberAccessibilityFlags.Private",
                new string[] {
                    "protected string ProtectedProp { get; set; }",
                    "private string PrivateProp { get; set; }"
                }
            },
            new object[]
            {
                "MemberAccessibilityFlags.Public | MemberAccessibilityFlags.Protected | MemberAccessibilityFlags.Private",
                new string[] {
                    "public string PublicProp { get; set; }",
                    "protected string ProtectedProp { get; set; }",
                    "private string PrivateProp { get; set; }"
                }
            },
            new object[]
            {
                "MemberAccessibilityFlags.Any",
                new string[] {
                    "public string PublicProp { get; set; }",
                    "protected string ProtectedProp { get; set; }",
                    "private string PrivateProp { get; set; }"
                }
            }
        };

        [Theory]
        [MemberData(nameof(MemberAccessibilityData))]
        public void ShouldHandleSelectedAccessibility(string accessibilites, string[] expectedProps)
        {
            var source = new StringBuilder()
                .AppendLine("using System;")
                .AppendLine("using TypeUtilities;")
                .AppendLine("using TypeUtilities.Abstractions;")
                .AppendLine("namespace MapTests;")
                .AppendLine("public class SourceType")
                .AppendLine("{")
                .AppendLine("\tpublic string PublicProp { get; set; }")
                .AppendLine("\tprotected string ProtectedProp { get; set; }")
                .AppendLine("\tprivate string PrivateProp { get; set; }")
                .AppendLine("}\n")
                .AppendLine($"[{_attributeName}(typeof(SourceType){_additionCtorArgs}, MemberAccessibilitySelection = {accessibilites})]")
                .AppendLine("public partial class TargetType {}")
                .ToString();

            var result = _fixture.Generate(source);

            var expected = new StringBuilder()
                .AppendLine("namespace MapTests;")
                .AppendLine("public partial class TargetType")
                .AppendLine("{");

            foreach (var prop in expectedProps)
            {
                expected.AppendLine($"\t{prop}");
            }
            expected.AppendLine("}\n");

            result.ShouldHaveSingleSource($"TargetType.{_attributeName.ToLower()}.SourceType.g.cs", expected.ToString());
        }
    }
}
