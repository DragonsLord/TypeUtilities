using System.Threading.Tasks;
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

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ShouldHandleSyntaxSyntaxErors()
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

[Pick(typeof(SourceType), nameof(SourceType.Id), ""adssf)]
public partial class TargetType
{
    public double AdditionalValue { get; set; }
}
";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
}
