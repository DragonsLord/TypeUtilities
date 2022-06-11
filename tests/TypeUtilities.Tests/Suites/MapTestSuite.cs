using System.Collections.Generic;
using System.Text;
using TypeUtilities.Abstractions;
using TypeUtilities.Tests.Fixture;
using Xunit;

namespace TypeUtilities.Tests.Suites;

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
        var source = new StringBuilder(@"
using System;
using TypeUtilities;

namespace MapTests;

public class SourceType
{
    public Guid Id { get; set; }
    public int Value { get; }
    public DateTime Created { set; }
}
")
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
            .AppendLine("\tpublic int Value { get; }")
            .AppendLine("\tpublic System.DateTime Created { set; }")
            .AppendLine("}\n");

        result.ShouldHaveSingleSource($"TargetType.{_attributeName.ToLower()}.SourceType.g.cs", expected.ToString());
    }

    public static object[] MemberFormatData = new[] {
        new object[]
        {
            MemberDeclarationFormats.Source,
            new string[] {
                "public System.Guid Id { get; }",
                "protected int Value { get; set; }",
                "private System.DateTime Created { set; }",
                "public string StaticProp { get; set; }", //TODO: add static support
                "public double publicField;",
                "private int _privateField;",
                "public int readonlyField;" //TODO: add readonly field support
            }
        },
        new object[]
        {
            MemberDeclarationFormats.PublicGetSetProp,
            new string[] {
                "public System.Guid Id { get; set; }",
                "public int Value { get; set; }",
                "public System.DateTime Created { get; set; }",
                "public string StaticProp { get; set; }",
                "public double publicField { get; set; }",
                "public int _privateField { get; set; }",
                "public int readonlyField { get; set; }"
            }
        },
        new object[]
        {
            MemberDeclarationFormats.Field,
            new string[] {
                "public System.Guid Id;",
                "protected int Value;",
                "private System.DateTime Created;",
                "public string StaticProp;",
                "public double publicField;",
                "private int _privateField;",
                "public int readonlyField;"
            }
        },
        new object[]
        {
            MemberDeclarationFormats.GetProp,
            new string[] {
                "public System.Guid Id { get; }",
                "protected int Value { get; }",
                "private System.DateTime Created { get; }",
                "public string StaticProp { get; }",
                "public double publicField { get; }",
                "private int _privateField { get; }",
                "public int readonlyField { get; }"
            }
        },
        new object[]
        {
            "{accessibility} {type} Mapped{name} { get; set; }",
            new string[] {
                "public System.Guid MappedId { get; set; }",
                "protected int MappedValue { get; set; }",
                "private System.DateTime MappedCreated { get; set; }",
                "public string MappedStaticProp { get; set; }",
                "public double MappedpublicField { get; set; }",
                "private int Mapped_privateField { get; set; }",
                "public int MappedreadonlyField { get; set; }"
            }
        }
    };

    [Theory]
    [MemberData(nameof(MemberFormatData))]
    public void ShouldUseMemberFormat(string format, IEnumerable<string> expectedProps)
    {
        var source = new StringBuilder(@"
using System;
using TypeUtilities;
using TypeUtilities.Abstractions;

namespace MapTests;

public class SourceType
{
    public Guid Id { get; }
    protected int Value { get; set; }
    private DateTime Created { set; }
    public static string StaticProp { get; set; }
    public double publicField;
    private int _privateField;
    public readonly int readonlyField;
}
")
            .AppendLine($"[{_attributeName}(typeof(SourceType){_additionCtorArgs},")
            .AppendLine($"MemberDeclarationFormat = \"{format}\",")
            .AppendLine($"MemberScopeSelection = MemberScopeFlags.Any,")
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
        var source = new StringBuilder(@"
using System;
using TypeUtilities;
using TypeUtilities.Abstractions;

namespace MapTests;

public class BaseType
{
    public int BaseCount { get; }
    public double BaseScore { get; }
}

public class SourceType : BaseType
{
    public Guid Id { get; }
    public int Value { get; set; }
    public DateTime Created { set; }
}
")
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
	public System.DateTime Created { set; }
}")
            .ShouldHaveSource($"DoNotIncludeExplicitly.{_attributeName.ToLower()}.SourceType.g.cs", @"
namespace MapTests;

public partial class DoNotIncludeExplicitly
{
	public System.Guid Id { get; }
	public int Value { get; set; }
	public System.DateTime Created {  set; }
}")
                            .ShouldHaveSource($"Include.{_attributeName.ToLower()}.SourceType.g.cs", @"
namespace MapTests;

public partial class Include
{
	public System.Guid Id { get; }
	public int Value { get; set; }
	public System.DateTime Created { set; }
	public int BaseCount { get; }
	public double BaseScore { get; }
}");
    }

    public static object[] MemberAccessibilityData = new[] {
        new object[]
        {
            "MemberAccessibilityFlags.Public",
            new string[] {
                "public string Id { get; }"
            }
        },
        new object[]
        {
            "MemberAccessibilityFlags.Protected",
            new string[] {
                "protected string Value { get; }"
            }
        },
        new object[]
        {
            "MemberAccessibilityFlags.Private",
            new string[] {
                "private string Created { get; }"
            }
        },
        new object[]
        {
            "MemberAccessibilityFlags.Public | MemberAccessibilityFlags.Protected",
            new string[] {
                "public string Id { get; }",
                "protected string Value { get; }"
            }
        },
        new object[]
        {
            "MemberAccessibilityFlags.Public | MemberAccessibilityFlags.Private",
            new string[] {
                "public string Id { get; }",
                "private string Created { get; }"
            }
        },
        new object[]
        {
            "MemberAccessibilityFlags.Protected | MemberAccessibilityFlags.Private",
            new string[] {
                "protected string Value { get; }",
                "private string Created { get; }"
            }
        },
        new object[]
        {
            "MemberAccessibilityFlags.Public | MemberAccessibilityFlags.Protected | MemberAccessibilityFlags.Private",
            new string[] {
                "public string Id { get; }",
                "protected string Value { get; }",
                "private string Created { get; }"
            }
        },
        new object[]
        {
            "MemberAccessibilityFlags.Any",
            new string[] {
                "public string Id { get; }",
                "protected string Value { get; }",
                "private string Created { get; }"
            }
        }
    };

    [Theory]
    [MemberData(nameof(MemberAccessibilityData))]
    public void ShouldHandleSelectedAccessibility(string accessibilites, string[] expectedProps)
    {
        var source = new StringBuilder(@"
using System;
using TypeUtilities;
using TypeUtilities.Abstractions;

namespace MapTests;

public class SourceType
{
    public string Id { get; }
    protected string Value { get; }
    private string Created { get; }
}
")
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
