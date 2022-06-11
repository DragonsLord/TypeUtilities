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
        public IReadOnlyDictionary<string, Diagnostic> Diagnostics { get; }

        public SourceGeneratorResult(GeneratorDriverRunResult runResult)
        {
            GeneratedSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToDictionary(src => src.HintName, src => src.SourceText.ToString());
            Diagnostics = runResult.Diagnostics.ToDictionary(d => d.Id);
        }

        public SourceGeneratorResult ShouldHaveSourcesCount(int count)
        {
            Assert.True(count == GeneratedSources.Count, $"Should have {count} generated source, but acualy have {GeneratedSources.Count}");
            return this;
        }

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

        private string NormilizeSource(string source)
        {
            var noSumbols = Regex.Replace(source, @"[\r\n\t]", " ");
            return Regex.Replace(noSumbols, @"\s+", " ").TrimStart().TrimEnd();
        }
    }
}
