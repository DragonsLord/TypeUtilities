﻿using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace TypeUtilities.Tests;

[UsesVerify]
public class PickGeneratorTests
{
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

        return TestHelper.Verify(source);
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

        return TestHelper.Verify(source);
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

        return TestHelper.Verify(source);
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

        return TestHelper.Verify(source);
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

        return TestHelper.Verify(source);
    }
    #endregion
}