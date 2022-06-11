using TypeUtilities.Tests.Fixture;
using TypeUtilities.Tests.Suites;
using Xunit;

namespace TypeUtilities.Tests;

[Collection("Compilation Collection")]
public class OmitGeneratorTests
{
    [Collection("Compilation Collection")]
    public class MapSuite : MapTestSuite<OmitAttribute>
    {
        public MapSuite(CompilationFixture compilationFixture)
            : base(compilationFixture, "Omit")
        {

        }
    }

    [Collection("Compilation Collection")]
    public class DiagnosticsSuite : DiagnosticsTestSuite<OmitAttribute>
    {
        public DiagnosticsSuite(CompilationFixture compilationFixture)
            : base(compilationFixture, "Omit")
        {

        }
    }

    private readonly CompilationFixture _fixture;

    public OmitGeneratorTests(CompilationFixture compilationFixture)
    {
        _fixture = compilationFixture;
    }

    [Fact]
    public void ShouldAddNotSpecifiedField()
    {
        // The source code to test
        var source = @"
using System;
using TypeUtilities;

namespace OmitTests;

public class SourceType
{
    public Guid Id { get; set; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Omit(typeof(SourceType), nameof(SourceType.Value))]
public partial class TargetType
{
    public double AdditionalValue { get; set; }
}
";
        var result = _fixture.Generate(source);

        result
            .ShouldHaveSingleSource("TargetType.omit.SourceType.g.cs", @"
namespace OmitTests;

public partial class TargetType
{
	public System.Guid Id { get; set; }
	public System.DateTime Created { get; set; }
}");
    }

    #region SyntaxErrors
    [Fact]
    public void ShouldHandleFieldsSyntaxErors()
    {
        var source = @"
using TypeUtilities;

namespace OmitTests;

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

        var result = _fixture.Generate(source);

        result
            .ShouldHaveSingleSource("TargetType.omit.SourceType.g.cs", @"
namespace OmitTests;

public partial class TargetType
{
	public int Value { get; set; }
}");
    }

    [Fact]
    public void ShouldHandleSourceTypeSyntaxErors()
    {
        var source = @"
using TypeUtilities;

namespace OmitTests;

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

        var result = _fixture.Generate(source);

        result
            .ShouldHaveSingleSource("TargetType1.omit.SourceTy.g.cs", @"
namespace OmitTests;
public partial class TargetType1 { }");
    }

    [Fact]
    public void ShouldHandleIncludeBaseSyntaxErrors()
    {
        // The source code to test
        var source = @"
using System;
using TypeUtilities;

namespace OmitTests;

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

        var result = _fixture.Generate(source);

        result
            .ShouldHaveSourcesCount(2)
            .ShouldHaveSource("NameError.omit.SourceType.g.cs", @"
namespace OmitTests;

public partial class NameError
{
	public System.Guid Id { get; set; }
	public System.DateTime Created { get; set; }
}
")
            .ShouldHaveSource("ValueError.omit.SourceType.g.cs", @"
namespace OmitTests;

public partial class ValueError
{
	public System.Guid Id { get; set; }
	public System.DateTime Created { get; set; }
}
");
    }
    #endregion
}
