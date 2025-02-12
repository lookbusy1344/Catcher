namespace Catcher;

/// <summary>
/// Helpers to build results
/// </summary>
public static class ResultBuilder
{
	/// <summary>
	/// Build a success result
	/// </summary>
	public static Result<T> Success<T>(T result) => new() { ResultValue = result, Error = null };

	/// <summary>
	/// Build a failure result
	/// </summary>
	public static Result<T> Failure<T>(Exception exception)
	{
		// not allowed to set failure exception to null
		if (exception == null) {
			Environment.FailFast("Setting Result failure with a null exception", exception);
			throw new ArgumentNullException(nameof(exception)); // unreachable
		}

		return new() { ResultValue = default!, Error = exception };
	}

	/// <summary>
	/// Unit success (with no payload)
	/// </summary>
	public static readonly Result<Unit> SuccessUnit = Success(Unit.Value);

	/// <summary>
	/// Turn a nullable reference type from T? into a Result(T)
	/// Where NULL is an error
	/// Eg string? -> Result(string)
	/// </summary>
	public static Result<T> FromNullable<T>(T? value) where T : class =>
		value == null ? Failure<T>(new ArgumentNullException(nameof(value))) : Success(value);

	/// <summary>
	/// Turn a nullable value type from T? into a Result(T)
	/// Where NULL is an error
	/// Eg int? -> Result(int)
	/// </summary>
	public static Result<T> FromNullable<T>(T? value) where T : struct =>
		!value.HasValue ? Failure<T>(new ArgumentNullException(nameof(value))) : Success(value.Value);

	/// <summary>
	/// Turns a nullable Result(T?) into a Result(T), if the result is null its turned into an error
	/// eg Result(string?) -> Result(string)
	/// </summary>
	public static Result<T> RemoveNullable<T>(Result<T?> result) where T : class
	{
		// I'd prefer these in Result<T>, but cant because of the generic constraint
		if (result.IsError) {
			return Failure<T>(result.Error!);
		}

		if (result.ResultValue == null) {
			return Failure<T>(new ArgumentNullException(nameof(result)));
		}

		return Success(result.ResultValue);
	}

	/// <summary>
	/// Turns a nullable Result(T?) into a Result(T), if the result is null its turned into an error
	/// eg Result(int?) -> Result(int)
	/// </summary>
	public static Result<T> RemoveNullable<T>(Result<T?> result) where T : struct
	{
		// I'd prefer these in Result<T>, but cant because of the generic constraint
		if (result.IsError) {
			return Failure<T>(result.Error!);
		}

		if (!result.ResultValue.HasValue) {
			return Failure<T>(new ArgumentNullException(nameof(result)));
		}

		return Success(result.ResultValue.Value);
	}
}
