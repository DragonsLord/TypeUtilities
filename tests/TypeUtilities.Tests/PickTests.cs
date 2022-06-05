using System.Threading.Tasks;
using TypeUtilities.Abstractions;
using TypeUtilities.Tests.Fixture;
using TypeUtilities.Tests.Suites;
using VerifyXunit;
using Xunit;

namespace TypeUtilities.Tests;

[UsesVerify]
[Collection("Compilation Collection")]
public class PickGeneratorTests
{
    [UsesVerify]
    [Collection("Compilation Collection")]
    public class MapSuite : MapTestSuite<PickAttribute>
    {
        public MapSuite(CompilationFixture compilationFixture)
            : base(compilationFixture, "Pick", ", \"Id\", \"Created\", \"Score\"")
        {

        }
    }

    [UsesVerify]
    [Collection("Compilation Collection")]
    public class DiagnosticsSuite : DiagnosticsTestSuite<PickAttribute>
    {
        public DiagnosticsSuite(CompilationFixture compilationFixture)
            : base(compilationFixture, "Pick")
        {

        }
    }

    private readonly CompilationFixture _fixture;

    public PickGeneratorTests(CompilationFixture compilationFixture)
    {
        _fixture = compilationFixture;
    }

    [Fact]
    public Task ShouldAddSpecifiedField()
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
public partial class TargetType
{
    public double AdditionalValue { get; set; }
}
";
        return Verify(source);
    }

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
        return _fixture.Verify(source, "Pick");
    }
}
