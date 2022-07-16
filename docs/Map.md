# Map

Besides SourceType Map has the following properties:

- [MemberDeclarationFormat](#MemberDeclarationFormat)
- [IncludeBaseTypes](#IncludeBaseTypes)
- [MemberAccessibilitySelection](#MemberAccessibilitySelection)
- [MemberScopeSelection](#MemberScopeSelection)
- [MemberKindSelection](#MemberKindSelection)

Let's cover each one in more details

## MemberDeclarationFormat

MemberDeclarationFormat allows you to provide format in wich target member will be generated.  
By default it will have the **same format as a source** but you can customize it.

MemberDeclarationFormat accepts source string with the tokens support. Tokens will be replaced with the source value.
Here is full list of tokens:

- `"{accessibility}"` - source member accesibility like private, public etc
- `"{scope}"` - will be replace with static or empty string for instance members
- `"{fieldAccess}"` - will be replace with readonly for readonly field members or empty string for other cases
- `"{type}"` - source member type
- `"{name}"` - source member name
- `"{accessors}"` - will be replace with source property accessort like { get; set; } or empty string for non property members

This tokens are also provided as consts in the `TypeUtilities.Abstractions.MemberDeclarationFormats.Tokens` class.
Also `TypeUtilities.Abstractions.MemberDeclarationFormats` contains a set of predefined formats like Source, PublicGetSetProp, PublicField and others

### Example

```csharp
using TypeUtilities;
using static TypeUtilities.Abstractions.MemberDeclarationFormats;

public class Box<T>
{
    public T Value { get; set; }
}

public class SourceType
{
    public static int Count { get; }
    public Guid Id { get; }
    protected int Value { get; set; }
    public DateTime Created { get; set; }

    private double _score;
}

[Map(typeof(SourceType), MemberDeclarationFormat = $"public{Tokens.Scope}  Box<{Tokens.Type}> {Tokens.Name}{Tokens.Accessors}")]
public partial class MappedType
{
}

// Generated result
//----- MappedType.map.SourceType.g.cs
public partial class MappedType
{
    public static Box<int> Count { get; }
    public Box<Guid> Id { get; }
    public Box<int> Value { get; set; }
    public Box<DateTime> Created { get; set; }

    public Box<double> _score;
}
// --------------------
```

## IncludeBaseTypes

IncludeBaseTypes allows you to map members not only from the source type but of the source base type as well.  
By default base types are **not** included

### Example

```csharp
using TypeUtilities;

public class BaseType
{
    public DateTime Created { get; set; }
}

public class SourceType : BaseType
{
    public Guid Id { get; }
    public int Value { get; set; }
}

[Map(typeof(SourceType), IncludeBaseTypes = false)]
public partial class MappedWithoutBaseTypes
{
}

// Generated result
//----- MappedWithoutBaseTypes.map.SourceType.g.cs
public partial class MappedWithoutBaseTypes
{
    public Guid Id { get; }
    public int Value { get; set; }
}
// --------------------


[Map(typeof(SourceType), IncludeBaseTypes = false)]
public partial class MappedWithBaseTypes
{
}

// Generated result
//----- MappedWithBaseTypes.map.SourceType.g.cs
public partial class MappedWithBaseTypes
{
    public Guid Id { get; }
    public int Value { get; set; }
    public DateTime Created { get; set; }
}
// --------------------
```

## MemberAccessibilitySelection

MemberAccessibilitySelection allows you map only member with specified accesibility.  
By default only membrers with **public** accessibility are included

### Example

```csharp
using TypeUtilities;
using TypeUtilities.Abstractions;

public class SourceType
{
    public string PublicProp { get; }
    protected string ProtectedProp { get; }
    private string PrivateProp { get; }
}

[Map(typeof(SourceType), MemberAccessibilitySelection = MemberAccessibilityFlags.Public | MemberAccessibilityFlags.Protected)]
public partial class MappedType
{
}

// Generated result
//----- MappedType.map.SourceType.g.cs
public partial class MappedType
{
    public string PublicProp { get; }
    protected string ProtectedProp { get; }
}
// --------------------
```

## MemberScopeSelection

MemberScopeSelection allows you map only member with specified scope (static or instance members).  
By default **only instance membrers** are included

### Example

```csharp
using TypeUtilities;
using TypeUtilities.Abstractions;

public class SourceType
{
    public static string StaticProp { get; }
    public string InstanceProp { get; }
}

[Map(typeof(SourceType), MemberScopeSelection = MemberScopeFlags.Static)]
public partial class MappedType
{
}

// Generated result
//----- MappedType.map.SourceType.g.cs
public partial class MappedType
{
    public static string StaticProp { get; }
}
// --------------------
```

## MemberKindSelection

MemberKindSelection allows you map only specific kind of member like readonly props or fields etc.  
By default **only properties** are included

### Example

```csharp
using TypeUtilities;
using TypeUtilities.Abstractions;

public class SourceType
{
    public string RWProp { get; set; }
    public string ReadonlyProp { get; }

    public string field;
    public readonly string readonlyField;
}

[Map(typeof(SourceType), MemberKindSelection = MemberKindFlags.ReadonlyProperty | MemberKindFlags.WritableField)]
public partial class MappedType
{
}

// Generated result
//----- MappedType.map.SourceType.g.cs
public partial class MappedType
{
    public string ReadonlyProp { get; }
    public string field;
}
// --------------------
```
