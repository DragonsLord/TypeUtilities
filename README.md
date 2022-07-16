# TypeUtilities

[![Build](https://github.com/DragonsLord/TypeUtilities/actions/workflows/build.yml/badge.svg)](https://github.com/DragonsLord/TypeUtilities/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/TypeUtilities.svg)](https://www.nuget.org/packages/TypeUtilities/)

Type Utilities provides a source generators to create/transform one type into another.

This project was inspired by the [TypeScript Utility Types](https://www.typescriptlang.org/docs/handbook/utility-types.html) and was ment to bring similar functionality to the C# via source generators

## Installation

To use the the TypeUtilities, install the [TypeUtilities package](https://www.nuget.org/packages/TypeUtilities) into your project.

To install the packages, add the references to your _csproj_ file, for example by running

```bash
dotnet add package TypeUtilities --prerelease
```

This adds a `<PackageReference>` to your project. You can additionally mark the package as `PrivateAsets="all"` and `ExcludeAssets="runtime"`.

> Setting `PrivateAssets="all"` means any projects referencing this one will not also get a reference to the _TypeUtilities_ package. Setting `ExcludeAssets="runtime"` ensures the _TypeUtilities.Abstractions.dll_ file is not copied to your build output (it is not required at runtime).

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>

  <!-- Add the package -->
  <PackageReference Include="TypeUtilities" Version="0.0.1-alpha2" PrivateAssets="all" ExcludeAssets="runtime" />
  <!-- -->

</Project>
```

## Usage

TypeUtilities provides several attributes:

### Map

Map Attribute simply maps memebers of the source type to the target type using specified format.

```csharp
using TypeUtilities;
using TypeUtilities.Abstractions;

public class SourceType
{
    public Guid Id { get; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Map(typeof(SourceType))]
public partial class SimpleMap
{
}

// Generated result
//----- SimpleMap.map.SourceType.g.cs
public partial class SimpleMap
{
    public System.Guid Id { get; }
    public int Value { get; set; }
    public System.DateTime Created { get; set; }
}
// --------------------

[Map(typeof(SourceType),
      MemberDeclarationFormat = $"{Tokens.Accessibility} string Mapped{Tokens.Name}{Tokens.Accessors}",
      MemberKindSelection = MemberKindFlags.ReadonlyProperty
    )]
public partial class AdvancedMap
{
}

// Generated result
//----- AdvancedMap.map.SourceType.g.cs
public partial class AdvancedMap
{
    public string MappedId { get; }
}
// --------------------
```

More detailed description for Map is provided [here](docs/Map.md)

### Omit

Omit Attribute is similar to Map but also accepts an explicit list of members that should be exluded

```csharp
using TypeUtilities;

public class SourceType
{
    public Guid Id { get; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Omit(typeof(SourceType), "Value")]
public partial class TargetType
{
  public int MyValue { get; set; }
}

// Generated result
//----- TargetType.omit.SourceType.g.cs
public partial class TargetType
{
    public Guid Id { get; }
    public DateTime Created { get; set; }
}
```

### Pick

Pick Attribute is similar to Map but also requires to explicitly specify all members that should be included

```csharp
using TypeUtilities;

public class SourceType
{
    public Guid Id { get; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}

[Pick(typeof(SourceType), "Id", nameof(SourceType.Value))]
public partial class TargetType
{
}

// Generated result
//----- TargetType.omit.SourceType.g.cs
public partial class TargetType
{
    public Guid Id { get; }
    public int Value { get; set; }
}
```
