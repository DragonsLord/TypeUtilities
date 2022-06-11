using Microsoft.CodeAnalysis;
using System;
using TypeUtilities.Tests.Fixture;
using Xunit;

namespace TypeUtilities.Tests.Suites
{
    public abstract class DiagnosticsTestSuite<T>
        where T : Attribute
    {
        private readonly CompilationFixture _fixture;
        private readonly string _attributeName;

        public DiagnosticsTestSuite(CompilationFixture compilationFixture, string attributeName)
        {
            _fixture = compilationFixture;
            _attributeName = attributeName;
        }

        [Fact]
        public void ShouldRequirePartialModifier()
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

"+$"[{_attributeName}(typeof(SourceType))]"+
@"public class TargetType
{
    public double AdditionalValue { get; set; }
}
";

            var result = _fixture.Generate(source);

            result
                .ShouldNotHaveSources()
                // TODO: extract diagnostics id into contstants
                .ShouldHaveSingleDiagnostic("TU001", DiagnosticSeverity.Error, "TargetType should have partial modifier");
        }
    }
}
