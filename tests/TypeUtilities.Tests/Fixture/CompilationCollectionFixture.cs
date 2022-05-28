using Xunit;

namespace TypeUtilities.Tests.Fixture
{
    [CollectionDefinition("Compilation Collection")]
    public class CompilationCollection : ICollectionFixture<CompilationFixture>
    {
    }
}
