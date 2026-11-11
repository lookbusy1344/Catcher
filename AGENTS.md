# Catcher - Project Guidelines

## Project Overview

Catcher is an **experimental** library demonstrating functional Result-type error handling in C#. This project intentionally explores exception-free code patterns while acknowledging they are NOT the recommended approach for idiomatic C#.

**Key Philosophy**: This is a pedagogical exercise showing the difficulty of truly exception-free C# code. The README explicitly states "stick with exceptions" - this project exists to validate that recommendation through practical demonstration.

## Architecture Principles

### Core Components

1. **Result<T>**: C# 15 custom union (`[Union]` readonly struct implementing `IUnion`) with case types `T` and `Exception`. Partial struct: `ResultUnion.cs` holds the union core (constructors, state, `Value`, `HasValue`, `TryGetValue`); `Result.cs` holds chaining, matching and equality
2. **ResultBuilder**: Static factory methods for creating Results where an implicit conversion has no target type, and for `Result<Exception>`
3. **Catcher**: Static wrapper methods converting exceptions into Results
4. **Unit**: Void-equivalent type for Result<Unit> chains

### Discriminated Union Pattern

The Result type uses null as the discriminator:
- `Error == null` → success state (even if ResultValue is null)
- `Error != null` → failure state

This requires strict discipline when using the API. Never access ResultValue without checking IsSuccess first.

### Union Members

- One public single-parameter constructor per case type: `Result(T)` and `Result(Exception)`. These give implicit union conversions, so `Result<int> r = 42;` and `Result<int> r = ex;` work. Do not add other public single-parameter constructors; each one adds a case type.
- `Value` returns `object?`: the `Error`, or `ResultValue` boxed. It is null for a success holding null.
- Non-boxing access pattern: `HasValue`, `TryGetValue(out T)`, `TryGetValue(out Exception)`. The compiler uses them for type and null patterns. They must agree with `Value`: `TryGetValue(out T)` is true only for a success holding non-null; `HasValue` is `Value is not null`.
- `default(Result<T>)` is a success holding `default(T)`.
- The `null` literal converts to the `Exception` case. `Result(Exception)` replaces null with `ArgumentNullException` so `result == null` and `Equals(null)` stay safe.
- `Result<Exception>` has ambiguous implicit conversions (CS0457). `Result<object>` maps an `Exception` to the failure case.

### Fail-Fast Philosophy

When user-provided lambdas throw in Match/Switch/Transform operations, the system calls `Environment.FailFast()` rather than trying to handle them. This treats programming errors as fatal rather than recoverable.

**Rationale**: If a lambda in Match throws, your error handling has failed. This is a programming error that should crash the application immediately for diagnosis.

## Development Guidelines

### Code Style

- **Functional composition**: Prefer Then/Pipe/Transform chains over imperative if-else
- **Immutability**: All types are readonly structs or records
- **Explicit nullability**: Use FromNullable/RemoveNullable for nullable conversions
- **No throw in public API**: All exceptions must be caught and wrapped in Results

### Testing Strategy

Given the experimental nature, tests are integrated into CatcherExample.cs:
- `CheckEquality`, `CheckUnion` and `CheckChaining` run from `Main` and throw on the first failed assertion
- Test both success and failure paths
- Validate equality semantics
- Demonstrate composition patterns
- Show async/await integration (`CheckAsync`)

### When Modifying This Project

1. **Adding new methods**: All public methods must return Result<T> or Task<Result<T>>
2. **Exception handling**: Catch at entry points only; use Catcher.Try() for wrapping. Return values and exceptions directly; the union conversions build the Result. Avoid capturing lambdas on hot paths
3. **Null checks**: Every public method validates inputs before use
4. **Compiler attributes**: Use MemberNotNullWhen to guide nullability analysis
5. **Struct semantics**: Result<T> must remain a readonly struct for efficiency

### Intentional Design Decisions

These are NOT bugs:

- **Exception equality by type+message**: Ignores stack trace and inner exceptions for testability
- **Null as discriminator**: Simpler than Tagged Union; works with C# nullability
- **Custom union, not a `union` declaration**: A `union` declaration stores contents as `object?` and boxes value types. The custom struct keeps typed fields and the non-boxing access pattern
- **Null exception becomes ArgumentNullException**: The `null` literal converts to the `Exception` case, so failing fast there would crash on `result == null`
- **Fail-fast in Match/Transform**: Programming errors should crash
- **No Try wrapper on Then/Pipe**: These catch exceptions internally; user doesn't need Try
- **Unit type for void operations**: Enables Result<Unit> instead of Result<object>

### What This Project Demonstrates

**Positive Demonstrations:**
- How to implement a C# 15 custom union with the non-boxing access pattern
- Functional composition with monadic bind (Then)
- Integration with nullability analysis via MemberNotNullWhen
- Appropriate use of fail-fast semantics
- Clean async/await support for Result types

**Negative Demonstrations** (intentional limitations):
- The BCL contains ~39k `throw` statements - true exception-free code is impractical
- Even "Try" pattern methods (int.TryParse) still throw in edge cases
- Every constructor can throw
- Fighting C# idioms adds complexity without proportional value

## Framework Integration

### .NET Version
Currently targets .NET 11 (C# 15, the SDK default). When upgrading:
- Use the latest C# language features
- Test nullability warnings carefully
- Verify attribute behavior hasn't changed

### BCL Throws Count
README documents 38,840 throws in .NET 8 BCL (commit c1a9f26). When updating the count:
```bash
git clone https://github.com/dotnet/runtime
cd runtime
git log --oneline | head -1  # Get commit hash
rg "throw new" --type cs src/libraries | wc -l
```

## Code Review Focus Areas

When reviewing changes:

1. **Exception safety**: Verify all exceptions are caught and wrapped
2. **Null handling**: Check MemberNotNullWhen attributes match logic
3. **Union contract**: `Value`, `HasValue` and both `TryGetValue` overloads agree for every state, including default and success-holding-null
4. **Struct semantics**: Ensure Result<T> remains readonly and value-semantic
5. **Equality contracts**: ResultValue equality must be by value, Exception by type+message
6. **Async patterns**: Verify Task<Result<T>> follows TAP correctly
7. **API clarity**: Methods should have obvious success/failure semantics

## Common Patterns

### Creating Results
```csharp
// From value or exception, by implicit union conversion
Result<int> success = 42;
Result<int> failure = new InvalidOperationException("error");

// Inside a method returning Result<T>, return the value or exception directly
return new ArgumentNullException(nameof(func));

// Without a target type
var built = ResultBuilder.Success(42);

// Wrapping throws
var result = Catcher.Try(() => int.Parse("abc"));
```

### Chaining Operations
```csharp
return Catcher.Try(() => GetUserId())
    .Then(id => LoadUser(id))           // Transform success value
    .Then(user => user.Email)           // Chain transformations
    .OnError(ex => GetDefaultEmail())   // Recover from error
    .Match(
        email => SendEmail(email),      // Success path
        ex => LogError(ex)              // Failure path
    );
```

### Async Patterns
```csharp
var result = await Catcher.TryAsync(async () => await FetchDataAsync())
    .Then(data => ProcessData(data))
    .Transform(
        success => SaveToDatabase(success),
        failure => LogFailure(failure)
    );
```

## Anti-Patterns to Avoid

1. **Accessing ResultValue without checking IsSuccess**: Will read uninitialized memory in failure case
2. **Throwing from Match/Transform lambdas**: Will crash via Environment.FailFast
3. **Mixing exceptions and Results**: Pick one error strategy and stick to it
4. **Over-using in BCL-heavy code**: Wrapping every BCL call is noise; use where you control the API
5. **Treating this as production-ready**: This is an experiment, not a framework

## References

- Framework Design Guidelines on exceptions (linked in README)
- F# Result type inspiration
- Railway Oriented Programming pattern
- .NET BCL source code demonstrating pervasive throws
