namespace Catcher;

/// <summary>
/// Wraps Action or Func to catch exceptions and return them
/// </summary>
public static class Catcher
{
	/// <summary>
	/// Try this action, and return a Result(Unit) for success or failure
	/// </summary>
	public static Result<Unit> Try(Action action)
	{
		if (action == null) {
			return ResultBuilder.Failure<Unit>(new ArgumentNullException(nameof(action)));
		}

		try {
			action();
			return ResultBuilder.SuccessUnit;
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Unit>(ex);
		}
	}

	/// <summary>
	/// Try this action, and return a Result(Unit) for success or failure
	/// </summary>
	/// <typeparam name="T">Input type of action</typeparam>
	/// <param name="action">Function to run. Any exceptions are turned into a Result(T).Failure</param>
	/// <param name="param1">Parameter to be passed to action</param>
	public static Result<Unit> Try<T>(Action<T> action, T param1)
	{
		if (action == null) {
			return ResultBuilder.Failure<Unit>(new ArgumentNullException(nameof(action)));
		}

		try {
			action(param1);
			return ResultBuilder.SuccessUnit;
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Unit>(ex);
		}
	}

	/// <summary>
	/// Try this function, and return a Result(R) for success or failure
	/// </summary>
	/// <typeparam name="R">Resulting type</typeparam>
	/// <param name="func">Function which returns type R, and will be converted into Result(R)</param>
	public static Result<R> Try<R>(Func<R> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<R>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Success(func());
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<R>(ex);
		}
	}

	/// <summary>
	/// Call this function which directly returns Result(R). Throwing is not necessary to signal failure
	/// </summary>
	/// <typeparam name="R">Inner Result type</typeparam>
	/// <param name="func">Function which returns type Result(R) directly</param>
	public static Result<R> Call<R>(Func<Result<R>> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<R>(new ArgumentNullException(nameof(func)));
		}

		try {
			return func();
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<R>(ex);
		}
	}

	/// <summary>
	/// Try this function, and return a Result(R) for success or failure
	/// </summary>
	/// <typeparam name="T">Input type of function</typeparam>
	/// <typeparam name="R">Result type Result(R)</typeparam>
	/// <param name="func">Function to turn T into R</param>
	/// <param name="param1">Parameter to be passed to action</param>
	public static Result<R> Try<T, R>(Func<T, R> func, T param1)
	{
		if (func == null) {
			return ResultBuilder.Failure<R>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Success(func(param1));
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<R>(ex);
		}
	}

	/// <summary>
	/// Try this async function (which returns a Task), and return an awaitable Task(Result(Unit))
	/// </summary>
	public static async Task<Result<Unit>> TryAsync(Func<Task> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<Unit>(new ArgumentNullException(nameof(func)));
		}

		try {
			await func();
			return ResultBuilder.SuccessUnit; // all returns are wrapped in a Task
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Unit>(ex);
		}
	}

	/// <summary>
	/// Try this async function (which returns a Task(R)), and return an awaitable Task(Result(R))
	/// </summary>
	public static async Task<Result<R>> TryAsync<R>(Func<Task<R>> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<R>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Success(await func()); // all returns are wrapped in a Task
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<R>(ex);
		}
	}

	/// <summary>
	/// Try this async function (which returns a Task(R)), and return an awaitable Task(Result(R)). Takes a T as input
	/// </summary>
	public static async Task<Result<R>> TryAsync<T, R>(Func<T, Task<R>> func, T param1)
	{
		if (func == null) {
			return ResultBuilder.Failure<R>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Success(await func(param1)); // all returns are wrapped in a Task
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<R>(ex);
		}
	}

	/// <summary>
	/// Call this async function (which returns a Task(Result(R))) directly. Throwing is not necessary to signal failure
	/// </summary>
	public static async Task<Result<R>> CallAsync<R>(Func<Task<Result<R>>> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<R>(new ArgumentNullException(nameof(func)));
		}

		try {
			return await func();
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<R>(ex);
		}
	}
}

/// <summary>
/// Unit type, used for void functions in the form Result(Unit)
/// </summary>
public readonly record struct Unit
{
	/// <summary>
	/// Singleton instance
	/// </summary>
	public static Unit Value { get; }
}
