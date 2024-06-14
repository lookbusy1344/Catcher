namespace Catcher;

#pragma warning disable CA1815, CA2231, CA1066 // Equals and operator equals on value types

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// This basically is a struct discriminated union of In and Exception.
/// An indicator field is not needed - if Exception is null, then Result is always valid (even if null itself)
/// </summary>
public readonly struct Result<In>(In result, Exception? exception)
{
	/// <summary>
	/// If Error is null, this is the result (even if null itself)
	/// </summary>
	public required In ResultValue { get; init; } = result;

	/// <summary>
	/// If not null, this result is a failure
	/// </summary>
	public required Exception? Error { get; init; } = exception;

	/// <summary>
	/// This has an attribute to indicate that if the result is true, then the exception is null
	/// </summary>
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess => Error == null;

	/// <summary>
	/// This has an attribute to indicate that if the result is true, then the exception is not null
	/// </summary>
	[MemberNotNullWhen(true, nameof(Error))]
	public bool IsError => Error != null;

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
			throw;  // unreachable
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

			if (IsSuccess) {
				return success(ResultValue);
			} else {
				return failure(Error);
			}
		}
		catch (Exception ex) {
			// an exception in the match function is a fatal error
			Environment.FailFast("An unexpected error occurred in Match", ex);
			throw;  // unreachable
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

			if (IsSuccess) {
				return success(ResultValue);
			} else {
				return failure(Error);
			}
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

			if (IsSuccess) {
				return Catcher.Try(success, ResultValue);
			} else {
				return Catcher.Try(failure, Error);
			}
		}
		catch (Exception ex) {
			// an exception in the transform function is a fatal error
			Environment.FailFast("An unexpected error occurred in Transform", ex);
			throw;  // unreachable
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
				if (IsSuccess) {
					return success(ResultValue);
				} else {
					return failure(Error);
				}
			}
			catch (Exception ex) {
				// the failure handler threw an exception, so return it as a failure Result<R>
				return ResultBuilder.Failure<Out>(ex);
			}
		}
		catch (Exception ex) {
			// an exception in the transform function is a fatal error
			Environment.FailFast("An unexpected error occurred in Transform", ex);
			throw;  // unreachable
		}
	}

	/// <summary>
	/// On success, chain an Action that takes the result as a parameter
	/// </summary>
	public Result<Unit> Then(Action<In> action)
	{
		// if we receive a failure, just return it
		if (IsError) {
			return ResultBuilder.Failure<Unit>(Error);
		}

		if (action == null) {
			return ResultBuilder.Failure<Unit>(new ArgumentNullException(nameof(action)));
		}

		return Catcher.Try(action, ResultValue);
	}

	/// <summary>
	/// On success, chain an Func that takes the result as a parameter, and returns Out
	/// </summary>
	public Result<Out> Then<Out>(Func<In, Out> func)
	{
		// if we receive a failure, just return it
		if (IsError) {
			return ResultBuilder.Failure<Out>(Error);
		}

		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		return Catcher.Try(func, ResultValue);
	}

	/// <summary>
	/// On success, chain an Func that takes the result as a parameter, and returns Result(Out) directly
	/// </summary>
	public Result<Out> Then<Out>(Func<In, Result<Out>> func)
	{
		// if we receive a failure, just return it
		if (IsError) {
			return ResultBuilder.Failure<Out>(Error);
		}

		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		try {
			// the worker function returns a Result<Out> directly
			return func(ResultValue);
		}
		catch (Exception ex) {
			// any exceptions, we catch and return as a failure
			return ResultBuilder.Failure<Out>(ex);
		}
	}

	public async Task<Result<Out>> ThenAsync<Out>(Func<Task<Out>> func)
	{
		if (IsError) {
			return ResultBuilder.Failure<Out>(Error);
		}

		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		return await Catcher.TryAsync(func);
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
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		try {
			return func(this);
		}
		catch (Exception ex) {
			// any exceptions, we catch and return as a failure
			return ResultBuilder.Failure<Out>(ex);
		}
	}

	/// <summary>
	/// On failure, chain an Func that takes the error as a parameter, and returns In without changing the chain type
	/// </summary>
	public Result<In> OnError(Func<Exception, In> func)
	{
		if (IsSuccess) {
			return this;
		}

		if (func == null) {
			return ResultBuilder.Failure<In>(new ArgumentNullException(nameof(func)));
		}

		return Catcher.Try(func, Error);
	}

	/// <summary>
	/// On failure, chain an Func that takes the error as a parameter, and returns Result(In) without changing the chain type
	/// </summary>
	public Result<In> OnError(Func<Exception, Result<In>> func)
	{
		if (IsSuccess) {
			return this;
		}

		if (func == null) {
			return ResultBuilder.Failure<In>(new ArgumentNullException(nameof(func)));
		}

		try {
			return func(Error);
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<In>(ex);
		}
	}

	/// <summary>
	/// On failure, replace the error with a new error
	/// </summary>
	public Result<In> OnError(Exception exception) =>
		IsSuccess ? this : ResultBuilder.Failure<In>(exception ?? new ArgumentNullException(nameof(exception)));

	/// <summary>
	/// On failure, chain an Func that takes the error as a parameter, and replaces it with another error
	/// </summary>
	public Result<In> OnError(Func<Exception, Exception> func)
	{
		if (IsSuccess) {
			return this;
		}

		if (func == null) {
			return ResultBuilder.Failure<In>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Failure<In>(func(Error));
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<In>(ex);
		}
	}

	/// <summary>
	/// Get the success result, and fail fast if there is an error
	/// </summary>
	public In Unwrap()
	{
		if (IsSuccess) {
			return ResultValue;
		} else {
			Environment.FailFast("Unwrapped an error"); // should halt the program immediately
			throw new NotSupportedException("Unwrap should be unreachable"); // to ensure we never return
		}
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

	/// <summary>
	/// We can't support this, and we cant throw so we fail fast
	/// </summary>
	public override bool Equals(object? obj) => NotSupported<bool>(nameof(Equals));

	/// <summary>
	/// We can't support this, and we cant throw so we fail fast
	/// </summary>
	public override int GetHashCode() => NotSupported<int>(nameof(GetHashCode));

	/// <summary>
	/// Fail fast, but fake a return value to keep the compiler happy
	/// </summary>
	[DoesNotReturn]
	private static Out NotSupported<Out>(string name)
	{
		Environment.FailFast($"Not supported: {name}"); // should halt the program immediately
		throw new NotSupportedException($"Not supported: {name}"); // to ensure we never return
	}
}
