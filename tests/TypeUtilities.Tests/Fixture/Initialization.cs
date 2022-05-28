using System.Runtime.CompilerServices;
using VerifyTests;

namespace TypeUtilities.Tests.Fixture;

public static class Initialization
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Enable();
    }
}