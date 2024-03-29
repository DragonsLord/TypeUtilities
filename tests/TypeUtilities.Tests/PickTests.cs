﻿using Microsoft.CodeAnalysis;
using TypeUtilities.Tests.Fixture;
using TypeUtilities.Tests.Suites;
using Xunit;

namespace TypeUtilities.Tests;

[Collection("Compilation Collection")]
public class PickGeneratorTests
{
    [Collection("Compilation Collection")]
    public class MapSuite : MapTestSuite<PickAttribute>
    {
        public MapSuite(CompilationFixture compilationFixture)
            // TODO: make some context class to improve and simplify suite setup
            : base(compilationFixture, "Pick", ", \"Id\", \"Value\", \"Created\", \"StaticProp\", \"publicField\", \"_privateField\", \"readonlyField\", \"BaseCount\", \"BaseScore\"")
        {

        }
    }

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
    public void ShouldAddSpecifiedField()
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
        var result = _fixture.Generate(source);

        result
            .ShouldHaveSingleSource("TargetType.pick.SourceType.g.cs", @"
namespace PickTests;

public partial class TargetType
{
	public System.Guid Id { get; set; }
	public System.DateTime Created { get; set; }
}");
    }

    #region Diagnostics

    [Fact]
    public void ShouldWarnIfPickMembersIsMissing()
    {
        // The source code to test
        var source = @"
using System;
using TypeUtilities;

namespace DiagnosticsTests;

public class SourceType
{
    public Guid Id { get; set; }
    public DateTime Created { get; set; }
}

[Pick(typeof(SourceType), ""Id"", ""Member1"", ""Member2"")]
public partial class TargetType { }
";

        var result = _fixture.Generate(source);

        result
            .ShouldHaveSingleDiagnostic("TU004", DiagnosticSeverity.Warning, "Members Member1, Member2 are not present in the SourceType selection and will be missing");
    }

    #endregion

    #region SyntaxErrors
    [Fact]
    public void ShouldHandleFieldsSyntaxErors()
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

        var result = _fixture.Generate(source);

        result
            .ShouldHaveSingleSource("TargetType.pick.SourceType.g.cs", @"
namespace PickTests;

public partial class TargetType
{
	public string Id { get; set; }
}");
    }

    [Fact]
    public void ShouldHandleSourceTypeSyntaxErors()
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

    var result = _fixture.Generate(source);

    result
        .ShouldHaveSingleSource("TargetType1.pick.SourceTy.g.cs", @"
namespace PickTests;

public partial class TargetType1
{
}
");
    }

    [Fact]
    public void ShouldHandleIncludeBaseSyntaxErrors()
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

        var result = _fixture.Generate(source);

        result
            .ShouldHaveSourcesCount(2)
            .ShouldHaveSource("NameError.pick.SourceType.g.cs", @"
namespace PickTests;

public partial class NameError
{
	public System.Guid Id { get; set; }
}")
            .ShouldHaveSource("ValueError.pick.SourceType.g.cs", @"
namespace PickTests;

public partial class ValueError
{
	public System.Guid Id { get; set; }
}");
    }
    #endregion
}
