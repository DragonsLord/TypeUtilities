using TypeUtilities.Tests.Fixture;
using TypeUtilities.Tests.Suites;
using Xunit;

namespace TypeUtilities.Tests;

[Collection("Compilation Collection")]
public class MapGeneratorTests
{
    [Collection("Compilation Collection")]
    public class MapSuite : MapTestSuite<MapAttribute>
    {
        public MapSuite(CompilationFixture compilationFixture)
            : base(compilationFixture, "Map")
        {

        }
    }

    [Collection("Compilation Collection")]
    public class DiagnosticsSuite : DiagnosticsTestSuite<MapAttribute>
    {
        public DiagnosticsSuite(CompilationFixture compilationFixture)
            : base(compilationFixture, "Map")
        {

        }
    }

    private readonly CompilationFixture _fixture;

    public MapGeneratorTests(CompilationFixture compilationFixture)
    {
        _fixture = compilationFixture;
    }

    #region SyntaxErrors

    [Fact]
    public void ShouldHandleSourceTypeSyntaxErors()
    {
        var source = @"
using TypeUtilities;

namespace MapTests;

public class SourceType
{
    public string Id { get; set; }
    public int Value { get; set; }
}

[Map(typeof(SourceTy)]
public partial class TargetType1
{
    public double AdditionalValue { get; set; }
}

[Map(typ]
public partial class TargetType2
{
    public double AdditionalValue { get; set; }
}
";

        var result = _fixture.Generate(source);

        result
            .ShouldHaveSingleSource("TargetType1.map.SourceTy.g.cs", @"
namespace MapTests;
public partial class TargetType1 { }");
    }

    [Fact] //TODO: Theory
    public void ShouldHandleIncludeBaseSyntaxErrors()
    {
        // The source code to test
        var source = @"
using System;
using TypeUtilities;

namespace MapTests;

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

[Map(typeof(SourceType), IncludeBaseTypes = fal)]
public partial class ValueError {}

[Map(typeof(SourceType), IncludeBaseType = false)]
public partial class NameError {}
";

        var result = _fixture.Generate(source);

        result
            .ShouldHaveSourcesCount(2)
            .ShouldHaveSource("NameError.map.SourceType.g.cs", @"
namespace MapTests;

public partial class NameError
{
	public System.Guid Id { get; set; }
	public int Value { get; set; }
	public System.DateTime Created { get; set; }
}")
            .ShouldHaveSource("ValueError.map.SourceType.g.cs", @"
namespace MapTests;

public partial class ValueError
{
	public System.Guid Id { get; set; }
	public int Value { get; set; }
	public System.DateTime Created { get; set; }
}");
    }
    #endregion
}
