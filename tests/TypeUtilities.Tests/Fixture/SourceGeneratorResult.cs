using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace TypeUtilities.Tests.Fixture
{
    public class SourceGeneratorResult
    {
        public IReadOnlyDictionary<string, string>  GeneratedSources { get; }
        public IReadOnlyDictionary<string, List<Diagnostic>> Diagnostics { get; }

        public SourceGeneratorResult(GeneratorDriverRunResult runResult)
        {
            GeneratedSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToDictionary(src => src.HintName, src => src.SourceText.ToString());
            Diagnostics = runResult.Diagnostics.GroupBy(d => d.Id).ToDictionary(x => x.Key, x => x.ToList());
        }

        public SourceGeneratorResult ShouldHaveSourcesCount(int count)
        {
            Assert.True(count == GeneratedSources.Count, $"Should have {count} generated source, but actualy have {GeneratedSources.Count}");
            return this;
        }

        public SourceGeneratorResult ShouldNotHaveSources()
            => ShouldHaveSourcesCount(0);

        public SourceGeneratorResult ShouldHaveSource(string name, string expectedSourceText)
        {
            Assert.True(GeneratedSources.ContainsKey(name), $"{name} source was not generated");

            var normilizedActual = NormilizeSource(GeneratedSources[name]);
            var normilizedExpected = NormilizeSource(expectedSourceText);

            Assert.Equal(normilizedExpected, normilizedActual);
            return this;
        }

        public SourceGeneratorResult ShouldHaveSingleSource(string name, string expectedSourceText)
        {
            return ShouldHaveSourcesCount(1).ShouldHaveSource(name, expectedSourceText);
        }

        public SourceGeneratorResult ShouldHaveDiagnosticsCount(int count)
        {
            var actualCount = Diagnostics.Sum(x => x.Value.Count);
            Assert.True(count == actualCount, $"Should have {count} diagnostics, but actualy have {actualCount}");
            return this;
        }

        public SourceGeneratorResult ShouldNotHaveDiagnostics()
            => ShouldHaveDiagnosticsCount(0);

        public SourceGeneratorResult ShouldHaveDiagnostic(string id, DiagnosticSeverity severity, string message)
        {
            Assert.True(Diagnostics.ContainsKey(id), $"The diagnostics with id {id} is missing");

            var diagnostics = Diagnostics[id];

            Assert.Contains(diagnostics, x => x.Severity == severity && x.GetMessage() == message);

            return this;
        }

        public SourceGeneratorResult ShouldHaveSingleDiagnostic(string id, DiagnosticSeverity severity, string message)
        {
            return ShouldHaveDiagnosticsCount(1).ShouldHaveDiagnostic(id, severity, message);
        }

        private string NormilizeSource(string source)
        {
            var noSumbols = Regex.Replace(source, @"[\r\n\t]", " ");
            return Regex.Replace(noSumbols, @"\s+", " ").TrimStart().TrimEnd();
        }
    }
}
