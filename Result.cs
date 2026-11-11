namespace Catcher;

#pragma warning disable CA1815, CA2231, CA1066 // Equals and operator equals on value types
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Chaining, matching and equality for the Result union. The union core is in ResultUnion.cs
/// </summary>
public readonly partial struct Result<In> : IEquatable<Result<In>>
{
	/// <summary>
	/// Get the result, or re-throw. DANGER this throws!
	/// </summary>
#pragma warning disable CA1024 // Use properties where appropriate
	public In GetResultOrThrow() => IsSuccess ? ResultValue : throw Error;
#pragma warning restore CA1024 // Use properties where appropriate

	/// <summary>
	/// Call the success function if success, or the failure function if failure
	/// </summary>
	public void Switch(Action<In> success, Action<Exception> failure)
	{
		try {
			// these will indirectly call a fail fast if null
			ArgumentNullException.ThrowIfNull(success);
			ArgumentNullException.ThrowIfNull(failure);

			if (IsSuccess) {
				success(ResultValue);
			} else {
				failure(Error);
			}
		}
		catch (Exception ex) {
			// an exception in the switch function is a fatal error
			Environment.FailFast("An unexpected error occurred in Switch", ex);
			throw; // unreachable
		}
	}

	/// <summary>
	/// Turn success or failure into a common type Out
	/// </summary>
	public Out Match<Out>(Func<In, Out> success, Func<Exception, Out> failure)
	{
		try {
			// these will indirectly call a fail fast if null
			ArgumentNullException.ThrowIfNull(success);
			ArgumentNullException.ThrowIfNull(failure);

			return IsSuccess ? success(ResultValue) : failure(Error);
		}
		catch (Exception ex) {
			// an exception in the match function is a fatal error
			Environment.FailFast("An unexpected error occurred in Match", ex);
			throw; // unreachable
		}
	}

	/// <summary>
	/// Turn success or failure into a common type Out. Any exceptions in the lambdas result in the default value
	/// </summary>
	public Out MatchDefault<Out>(Func<In, Out> success, Func<Exception, Out> failure)
	{
		try {
			// these will indirectly call a fail fast if null
			ArgumentNullException.ThrowIfNull(success);
			ArgumentNullException.ThrowIfNull(failure);

			return IsSuccess ? success(ResultValue) : failure(Error);
		}
		catch {
			// any problems cause the default value to be returned
			return default!;
		}
	}

	/// <summary>
	/// Transform success or failure into a common type Result(Out) for further chaining. Functions return Out to the chain
	/// </summary>
	public Result<Out> Transform<Out>(Func<In, Out> success, Func<Exception, Out> failure)
	{
		try {
			// these will indirectly call a fail fast if null
			ArgumentNullException.ThrowIfNull(success);
			ArgumentNullException.ThrowIfNull(failure);

			return IsSuccess ? Catcher.Try(success, ResultValue) : Catcher.Try(failure, Error);
		}
		catch (Exception ex) {
			// an exception in the transform function is a fatal error
			Environment.FailFast("An unexpected error occurred in Transform", ex);
			throw; // unreachable
		}
	}

	/// <summary>
	/// Transform success or failure into a common type Result(Out) for further chaining.
	/// Functions directly return a Result(Out) to the chain, allowing greater control
	/// </summary>
	public Result<Out> Transform<Out>(Func<In, Result<Out>> success, Func<Exception, Result<Out>> failure)
	{
		try {
			// these will indirectly call a fail fast if null
			ArgumentNullException.ThrowIfNull(success);
			ArgumentNullException.ThrowIfNull(failure);

			try {
				return IsSuccess ? success(ResultValue) : failure(Error);
			}
			catch (Exception ex) {
				// a handler threw, so return it as a failure
				return ex;
			}
		}
		catch (Exception ex) {
			// an exception in the transform function is a fatal error
			Environment.FailFast("An unexpected error occurred in Transform", ex);
			throw; // unreachable
		}
	}

	/// <summary>
	/// On success, chain an Action that takes the result as a parameter
	/// </summary>
	public Result<Unit> Then(Action<In> action) => action switch {
		_ when IsError => Error,
		null => new ArgumentNullException(nameof(action)),
		_ => Catcher.Try(action, ResultValue),
	};

	/// <summary>
	/// On success, chain an Func that takes the result as a parameter, and returns Out
	/// </summary>
	public Result<Out> Then<Out>(Func<In, Out> func) => func switch {
		_ when IsError => Error,
		null => new ArgumentNullException(nameof(func)),
		_ => Catcher.Try(func, ResultValue),
	};

	/// <summary>
	/// On success, chain an Func that takes the result as a parameter, and returns Result(Out) directly
	/// </summary>
	public Result<Out> Then<Out>(Func<In, Result<Out>> func)
	{
		if (IsError) {
			return Error;
		}

		if (func == null) {
			return new ArgumentNullException(nameof(func));
		}

		try {
			return func(ResultValue);
		}
		catch (Exception ex) {
			return ex;
		}
	}

	/// <summary>
	/// On success, chain an async Func that takes the result as a parameter, and returns Out
	/// </summary>
	public async Task<Result<Out>> ThenAsync<Out>(Func<In, Task<Out>> func) => func switch {
		_ when IsError => Error,
		null => new ArgumentNullException(nameof(func)),
		_ => await Catcher.TryAsync(func, ResultValue),
	};

	/// <summary>
	/// On success, chain an async Func that takes the result as a parameter, and returns Result(Out) directly
	/// </summary>
	public async Task<Result<Out>> ThenAsync<Out>(Func<In, Task<Result<Out>>> func)
	{
		if (IsError) {
			return Error;
		}

		if (func == null) {
			return new ArgumentNullException(nameof(func));
		}

		try {
			return await func(ResultValue);
		}
		catch (Exception ex) {
			return ex;
		}
	}

	/// <summary>
	/// Pipe the current Result(In) and convert it into Result(Out)
	/// </summary>
	/// <typeparam name="Out">Resulting type</typeparam>
	/// <param name="func">Function to convert this Result into new result</param>
	/// <returns>New Result(Out)</returns>
	public Result<Out> Pipe<Out>(Func<Result<In>, Result<Out>> func)
	{
		if (func == null) {
			return new ArgumentNullException(nameof(func));
		}

		try {
			return func(this);
		}
		catch (Exception ex) {
			return ex;
		}
	}

	/// <summary>
	/// On failure, chain an Func that takes the error as a parameter, and returns In without changing the chain type
	/// </summary>
	public Result<In> OnError(Func<Exception, In> func) => func switch {
		_ when IsSuccess => this,
		null => new ArgumentNullException(nameof(func)),
		_ => Catcher.Try(func, Error),
	};

	/// <summary>
	/// On failure, chain an Func that takes the error as a parameter, and returns Result(In) without changing the chain type
	/// </summary>
	public Result<In> OnError(Func<Exception, Result<In>> func)
	{
		if (IsSuccess) {
			return this;
		}

		if (func == null) {
			return new ArgumentNullException(nameof(func));
		}

		try {
			return func(Error);
		}
		catch (Exception ex) {
			return ex;
		}
	}

	/// <summary>
	/// On failure, replace the error with a new error
	/// </summary>
	public Result<In> OnError(Exception exception) =>
		IsSuccess ? this : exception ?? new ArgumentNullException(nameof(exception));

	/// <summary>
	/// On failure, chain an Func that takes the error as a parameter, and replaces it with another error
	/// </summary>
	public Result<In> OnError(Func<Exception, Exception> func)
	{
		if (IsSuccess) {
			return this;
		}

		if (func == null) {
			return new ArgumentNullException(nameof(func));
		}

		try {
			return func(Error);
		}
		catch (Exception ex) {
			return ex;
		}
	}

	/// <summary>
	/// Get the success result, and fail fast if there is an error
	/// </summary>
	public In Unwrap()
	{
		if (IsSuccess) {
			return ResultValue;
		}

		Environment.FailFast("Unwrapped an error"); // should halt the program immediately
		throw new NotSupportedException("Unwrap should be unreachable"); // to ensure we never return
	}

	public override string? ToString() => IsSuccess ? $"{ResultValue}" : $"ERROR({Error})";

	/// <summary>
	/// Deconstruct into a 2-tuple
	/// </summary>
	public void Deconstruct(out In result, out Exception? exception)
	{
		result = ResultValue;
		exception = Error;
	}

	/// <summary>
	/// Deconstruct into a 3-tuple
	/// </summary>
	public void Deconstruct(out In result, out Exception? exception, out bool issuccess)
	{
		result = ResultValue;
		exception = Error;
		issuccess = IsSuccess;
	}

	public bool Equals(Result<In> other)
	{
		// if both in error, compare the errors
		if (IsError && other.IsError) { return ExceptionEquals(Error, other.Error); }

		// here one CANNOT be in error state. If the other is an error they are not equal
		if (IsError || other.IsError) { return false; }

		// Both in Success state, so compare the values
		// Use EqualityComparer for a more flexible and null-safe comparison
		return EqualityComparer<In>.Default.Equals(ResultValue, other.ResultValue);
	}

	public override bool Equals(object? obj) => obj is Result<In> other && Equals(other);

	public override int GetHashCode() => IsError ? ExceptionHashCode(Error) : ResultValue?.GetHashCode() ?? 0;

	public static bool operator ==(Result<In> left, Result<In> right) => left.Equals(right);

	public static bool operator !=(Result<In> left, Result<In> right) => !left.Equals(right);

	/// <summary>
	/// Compare two exceptions for equality, on type and message
	/// </summary>
	private static bool ExceptionEquals(Exception ex1, Exception ex2) =>
		ReferenceEquals(ex1, ex2) || (ex1.GetType() == ex2.GetType() && ex1.Message == ex2.Message);

	/// <summary>
	/// Exceptions give ref based hashcodes, this compares type and message
	/// </summary>
	private static int ExceptionHashCode(Exception ex) => HashCode.Combine(ex.GetType(), ex.Message);

	///// <summary>
	///// Fail fast, but fake a return value to keep the compiler happy
	///// </summary>
	//[DoesNotReturn]
	//private static Out NotSupported<Out>(string name)
	//{
	//	Environment.FailFast($"Not supported: {name}"); // should halt the program immediately
	//	throw new NotSupportedException($"Not supported: {name}"); // to ensure we never return
	//}
}
