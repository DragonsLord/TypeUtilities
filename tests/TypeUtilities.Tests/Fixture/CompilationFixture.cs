using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TypeUtilities.SourceGenerators;
using VerifyTests;
using VerifyXunit;

namespace TypeUtilities.Tests.Fixture
{
    public class CompilationFixture
    {
        private CSharpCompilation _compiledDependencies;

        public CompilationFixture()
        {
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
                .Select(_ => MetadataReference.CreateFromFile(_.Location))
                .Concat(new[] { MetadataReference.CreateFromFile(typeof(PickAttribute).Assembly.Location) })
                .Concat(new[] { MetadataReference.CreateFromFile(typeof(TypeUtilitiesSourceGenerator).Assembly.Location) });

            // Create a Roslyn compilation for the syntax tree.
            _compiledDependencies = CSharpCompilation.Create(
                assemblyName: "Tests",
                references: references);
        }

        public Task Verify(string source, string snapshotPath, params string[] parameters)
        {
            var settings = new VerifySettings();
            settings.UseParameters(parameters);
            return Verify(source, snapshotPath, settings);
        }

        public Task Verify(string source, string snapshotPath = "", VerifySettings? settings = null)
        {
            var compilation = _compiledDependencies.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

            var generator = new TypeUtilitiesSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);

            return Verifier.Verify(driver, settings).UseDirectory(Path.Combine("../snapshots", snapshotPath));
        }
    }
}
