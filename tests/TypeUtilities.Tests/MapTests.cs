using System.Threading.Tasks;
using TypeUtilities.Tests.Fixture;
using TypeUtilities.Tests.Suites;
using VerifyXunit;
using Xunit;

namespace TypeUtilities.Tests;

[UsesVerify]
[Collection("Compilation Collection")]
public class MapGeneratorTests
{
    [UsesVerify]
    [Collection("Compilation Collection")]
    public class MapSuite : MapTestSuite<MapAttribute>
    {
        public MapSuite(CompilationFixture compilationFixture)
            : base(compilationFixture, "Map")
        {

        }
    }

    [UsesVerify]
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
    public Task ShouldHandleSourceTypeSyntaxErors()
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

        return Verify(source);
    }

    [Fact] //TODO: Theory
    public Task ShouldHandleIncludeBaseSyntaxErrors()
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

        return Verify(source);
    }
    #endregion

    private Task Verify(string source)
    {
        return _fixture.Verify(source, "Map/Specific");
    }
}
