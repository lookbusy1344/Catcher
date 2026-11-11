namespace Catcher;

/// <summary>
/// Helpers to build results
/// </summary>
public static class ResultBuilder
{
	/// <summary>
	/// Build a success result
	/// </summary>
	public static Result<T> Success<T>(T result) => new(result);

	/// <summary>
	/// Build a failure result. A null exception becomes an ArgumentNullException
	/// </summary>
	public static Result<T> Failure<T>(Exception exception) => new(exception);

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

	extension<T>(Result<T?> result) where T : class
	{
		/// <summary>
		/// Turns a nullable Result(T?) into a Result(T), if the result is null its turned into an error
		/// eg Result(string?) -> Result(string)
		/// </summary>
		public Result<T> RemoveNullable() => result switch {
			{ IsError: true } => result.Error,
			{ ResultValue: null } => new ArgumentNullException(nameof(result)),
			_ => result.ResultValue,
		};
	}

	extension<T>(Result<T?> result) where T : struct
	{
		/// <summary>
		/// Turns a nullable Result(T?) into a Result(T), if the result is null its turned into an error
		/// eg Result(int?) -> Result(int)
		/// </summary>
		public Result<T> RemoveNullable() => result switch {
			{ IsError: true } => result.Error,
			{ ResultValue: null } => new ArgumentNullException(nameof(result)),
			_ => result.ResultValue.Value,
		};
	}
}
