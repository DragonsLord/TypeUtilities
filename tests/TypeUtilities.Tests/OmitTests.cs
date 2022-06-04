using System.Threading.Tasks;
using TypeUtilities.Tests.Fixture;
using VerifyXunit;
using Xunit;

namespace TypeUtilities.Tests;

[UsesVerify]
[Collection("Compilation Collection")]
public class OmitGeneratorTests
{
    private readonly CompilationFixture _fixture;

    public OmitGeneratorTests(CompilationFixture compilationFixture)
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
    public Task ShouldAddNotSpecifiedField(string accessibility, string typeKind)
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

[Omit(typeof(SourceType), nameof(SourceType.Value))]"+"\n"+
$"{accessibility} partial {typeKind} TargetType"+
@"{
    public double AdditionalValue { get; set; }
}
";
        return Verify(source, accessibility, typeKind);
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
    public int Count { get; }
    public double Score { get; }
}

public class SourceType : BaseType
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Omit(typeof(SourceType), ""Value"", ""Created"", ""Score"")]
public partial class IncludeByDefault {}

[Omit(typeof(SourceType), ""Id"", ""Score"", IncludeBaseTypes = false)]
public partial class DoNotInclude {}

[Omit(typeof(SourceType), ""Id"", ""Count"", IncludeBaseTypes = true)]
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
    public DateTime Created { get; set; }
}

[Omit(typeof(SourceType), nameof(SourceType.Id), ""Created"")]
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

[Omit(typeof(SourceType), nameof(SourceType.Id), ""adssf)]
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

[Omit(typeof(SourceTy, nameof(SourceType.Id))]
public partial class TargetType1
{
    public double AdditionalValue { get; set; }
}

[Omit(typ]
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

[Omit(typeof(SourceType), ""Value"",  IncludeBaseTypes = fal)]
public partial class ValueError {}

[Omit(typeof(SourceType), ""Value"", IncludeBaseType = false)]
public partial class NameError {}
";

        return Verify(source);
    }
    #endregion

    private Task Verify(string source)
    {
        return _fixture.Verify(source, "omit");
    }

    private Task Verify(string source, params string[] parameters)
    {
        return _fixture.Verify(source, "omit", parameters);
    }
}
