﻿using System.Threading.Tasks;
using TypeUtilities.Tests.Fixture;
using TypeUtilities.Tests.Suites;
using VerifyXunit;
using Xunit;

namespace TypeUtilities.Tests;

[UsesVerify]
[Collection("Compilation Collection")]
public class OmitGeneratorTests
{
    [UsesVerify]
    [Collection("Compilation Collection")]
    public class MapSuite : MapTestSuite<OmitAttribute>
    {
        public MapSuite(CompilationFixture compilationFixture)
            : base(compilationFixture, "Omit", ", \"Value\", \"Score\"")
        {

        }
    }

    [UsesVerify]
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
    public Task ShouldAddNotSpecifiedField()
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
        return Verify(source);
    }

    #region SyntaxErrors
    [Fact]
    public Task ShouldHandleFieldsSyntaxErors()
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

        return Verify(source);
    }

    [Fact]
    public Task ShouldHandleSourceTypeSyntaxErors()
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

        return Verify(source);
    }

    [Fact] //TODO: Theory
    public Task ShouldHandleIncludeBaseSyntaxErrors()
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

        return Verify(source);
    }
    #endregion

    private Task Verify(string source)
    {
        return _fixture.Verify(source, "Omit");
    }
}
