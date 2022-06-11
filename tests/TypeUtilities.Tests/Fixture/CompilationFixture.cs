using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TypeUtilities.SourceGenerators;

namespace TypeUtilities.Tests.Fixture
{
    public class CompilationFixture
    {
        private readonly CSharpCompilation _compiledDependencies;

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

        public SourceGeneratorResult Generate(string source)
        {
            var compilation = _compiledDependencies.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

            var generator = new TypeUtilitiesSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);

            return new SourceGeneratorResult(driver.GetRunResult());
        }
    }
}
