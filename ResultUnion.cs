namespace Catcher;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// C# 15 custom union of In and Exception. Case types are In and Exception.
/// Only the success constructor sets isSuccess, so default(Result) is a failure holding an InvalidOperationException.
/// An exhaustive switch over the case types therefore covers default; for a reference In it does not cover
/// a success holding a null that bypassed nullability analysis.
/// Storage is typed, so value-type In is never boxed. TryGetValue and HasValue form the non-boxing access pattern
/// the compiler uses for type and null patterns.
/// </summary>
[Union]
public readonly partial struct Result<In> : IUnion
{
	private readonly bool isSuccess;
	private readonly Exception? error;

	/// <summary>
	/// Success case. Also the implicit union conversion from In.
	/// A value that is an Exception becomes the failure case, so Value and the patterns never see an Exception success
	/// </summary>
	public Result(In value)
	{
		if (value is Exception exception) {
			ResultValue = default!;
			error = exception;
		} else {
			ResultValue = value;
			isSuccess = true;
		}
	}

	/// <summary>
	/// Failure case. Also the implicit union conversion from Exception, and from the null literal.
	/// A null exception becomes an ArgumentNullException, so Result is always a failure here
	/// </summary>
	public Result(Exception error)
	{
		ResultValue = default!;
		this.error = error ?? new ArgumentNullException(nameof(error));
	}

	/// <summary>
	/// The success value (even if null itself). Read it only when IsSuccess
	/// </summary>
	public In ResultValue { get; }

	/// <summary>
	/// Null on success. On failure the error, or UninitializedResult.Error for default(Result)
	/// </summary>
	public Exception? Error => isSuccess ? null : error ?? UninitializedResult.Error;

	/// <summary>
	/// True only for a Result built from a success value
	/// </summary>
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess => isSuccess;

	/// <summary>
	/// True for a failure, including default(Result)
	/// </summary>
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsError => !isSuccess;

	/// <summary>
	/// The contents as object: the Error, or ResultValue boxed. Null for a success holding null. Prefer TryGetValue
	/// </summary>
	public object? Value => IsSuccess ? ResultValue : Error;

	/// <summary>
	/// True when Value is not null: any failure, or a success holding a non-null value
	/// </summary>
	public bool HasValue => IsError || ResultValue is not null;

	/// <summary>
	/// Same as `Value is In value`: true for a success holding a non-null value, or a failure whose Error is an In.
	/// The compiler lowers a type pattern that only In covers, such as `is ArgumentException` on Result(object),
	/// to this overload plus a type test, so failures must bind here too
	/// </summary>
	public bool TryGetValue([MaybeNullWhen(false)] out In value)
	{
		if (IsSuccess) {
			value = ResultValue;
			return value is not null;
		}

		// for value-type In the JIT folds this test to false, so nothing is boxed
		if (Error is In failure) {
			value = failure;
			return true;
		}

		value = default;
		return false;
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

/// <summary>
/// The error held by default(Result). One shared instance, so default Results compare equal and never allocate
/// </summary>
internal static class UninitializedResult
{
	public const string Message = "Result is uninitialized";

	public static readonly InvalidOperationException Error = new(Message);
}
