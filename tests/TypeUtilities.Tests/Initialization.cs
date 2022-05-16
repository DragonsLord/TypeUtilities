using System.Runtime.CompilerServices;
using VerifyTests;

namespace TypeUtilities.Tests;

public static class Initialization
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Enable();
    }
}