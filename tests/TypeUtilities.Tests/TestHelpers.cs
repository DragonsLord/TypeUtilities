using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using System.Threading.Tasks;
using TypeUtilities.SourceGenerators;
using VerifyXunit;

namespace TypeUtilities.Tests;

public static class TestHelper
{
    public static Task Verify(string source)
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(new[] { MetadataReference.CreateFromFile(typeof(PickAttribute).Assembly.Location) })
            .Concat(new[] { MetadataReference.CreateFromFile(typeof(TypeUtilitiesSourceGenerator).Assembly.Location) });

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);


        // Create an instance of our EnumGenerator incremental source generator
       var generator = new TypeUtilitiesSourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);
        //driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        //var trees = outputCompilation.SyntaxTrees.ToList();

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver).UseDirectory("snapshots");
    }
}