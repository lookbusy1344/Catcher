namespace Catcher;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// C# 15 custom union of In and Exception. Case types are In and Exception.
/// Error is the discriminator: null means success, even when ResultValue is null.
/// Storage is typed, so value-type In is never boxed. TryGetValue and HasValue form the non-boxing access pattern
/// the compiler uses for type and null patterns. default(Result) is a success holding default(In).
/// </summary>
[Union]
public readonly partial struct Result<In> : IUnion
{
	/// <summary>
	/// Success case. Also the implicit union conversion from In
	/// </summary>
	public Result(In value) => ResultValue = value;

	/// <summary>
	/// Failure case. Also the implicit union conversion from Exception, and from the null literal.
	/// A null exception becomes an ArgumentNullException, so Result is always a failure here
	/// </summary>
	public Result(Exception error)
	{
		ResultValue = default!;
		Error = error ?? new ArgumentNullException(nameof(error));
	}

	/// <summary>
	/// If Error is null, this is the result (even if null itself)
	/// </summary>
	public In ResultValue { get; }

	/// <summary>
	/// If not null, this result is a failure
	/// </summary>
	public Exception? Error { get; }

	/// <summary>
	/// True when Error is null
	/// </summary>
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess => Error == null;

	/// <summary>
	/// True when Error is not null
	/// </summary>
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsError => Error != null;

	/// <summary>
	/// The contents as object: the Error, or ResultValue boxed. Null for a success holding null. Prefer TryGetValue
	/// </summary>
	public object? Value => IsSuccess ? ResultValue : Error;

	/// <summary>
	/// True when Value is not null: any failure, or a success holding a non-null value
	/// </summary>
	public bool HasValue => IsError || ResultValue is not null;

	/// <summary>
	/// True for a success holding a non-null value
	/// </summary>
	public bool TryGetValue([MaybeNullWhen(false)] out In value)
	{
		value = ResultValue;
		return IsSuccess && value is not null;
	}

	/// <summary>
	/// True for a failure
	/// </summary>
	public bool TryGetValue([NotNullWhen(true)] out Exception? value)
	{
		value = Error;
		return IsError;
	}
}
