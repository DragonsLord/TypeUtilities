using System.Threading.Tasks;
using TypeUtilities.Abstractions;
using TypeUtilities.Tests.Fixture;
using VerifyXunit;
using Xunit;

namespace TypeUtilities.Tests;

[UsesVerify]
[Collection("Compilation Collection")]
public class PickGeneratorTests
{
    private readonly CompilationFixture _fixture;

    public PickGeneratorTests(CompilationFixture compilationFixture)
    {
        _fixture = compilationFixture;
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
    public Task ShouldAddSpecifiedField(string accessibility, string typeKind)
    {
        // The source code to test
        var source = @"
using System;
using TypeUtilities;

namespace PickTests;

public class SourceType
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Pick(typeof(SourceType), nameof(SourceType.Id), ""Created"")]" + "\n" +
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

namespace PickTests;

public class SourceType
{
    public Guid Id { get; }
    public int Value { get; set; }
    protected DateTime Created;
}

" + $"[Pick(typeof(SourceType), \"Id\", \"Created\", MemberDeclarationFormat = \"{format}\"))]\n" +
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

namespace PickTests;

public class BaseType
{
    public double Score { get; }
}

public class SourceType : BaseType
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Pick(typeof(SourceType), ""Id"", ""Score"")]
public partial class IncludeByDefault {}

[Pick(typeof(SourceType), ""Id"", ""Score"", IncludeBaseTypes = false)]
public partial class DoNotInclude {}

[Pick(typeof(SourceType), ""Id"", ""Score"", IncludeBaseTypes = true)]
public partial class IncludeExplicitly {}
";

        return Verify(source);
    }

    #region Diagnostics
    [Fact]
    public Task ShouldRequirePartialModifier()
    {
        // The source code to test
        var source = @"
using System;
using TypeUtilities;

namespace PickTests;

public class SourceType
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Pick(typeof(SourceType), nameof(SourceType.Id), ""Created"")]
public class TargetType
{
    public double AdditionalValue { get; set; }
}
";

        return Verify(source);
    }
    #endregion

    #region SyntaxErrors
    [Fact]
    public Task ShouldHandleFieldsSyntaxErors()
    {
        var source = @"
using TypeUtilities;

namespace PickTests;

public class SourceType
{
    public string Id { get; set; }
    public int Value { get; set; }
}

[Pick(typeof(SourceType), nameof(SourceType.Id), ""adssf)]
public partial class TargetType
{
    public double AdditionalValue { get; set; }
}
";

        return Verify(source);
    }

    [Fact]
    public Task ShouldHandleSourceTypeSyntaxErors()
    {
        var source = @"
using TypeUtilities;

namespace PickTests;

public class SourceType
{
    public string Id { get; set; }
    public int Value { get; set; }
}

[Pick(typeof(SourceTy, nameof(SourceType.Id))]
public partial class TargetType1
{
    public double AdditionalValue { get; set; }
}

[Pick(typ]
public partial class TargetType2
{
    public double AdditionalValue { get; set; }
}
";

        return Verify(source);
    }

    [Fact] //TODO: Theory
    public Task ShouldHandleIncludeBaseSyntaxErrors()
    {
        // The source code to test
        var source = @"
using System;
using TypeUtilities;

namespace PickTests;

public class BaseType
{
    public double Score { get; }
}

public class SourceType : BaseType
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Pick(typeof(SourceType), ""Id"", ""Score"",  IncludeBaseTypes = fal)]
public partial class ValueError {}

[Pick(typeof(SourceType), ""Id"", ""Score"", IncludeBaseType = false)]
public partial class NameError {}
";

        return Verify(source);
    }
    #endregion

    private Task Verify(string source)
    {
        return _fixture.Verify(source, "pick");
    }

    private Task Verify(string source, params string[] parameters)
    {
        return _fixture.Verify(source, "pick", parameters);
    }
}
