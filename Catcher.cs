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
	/// Try this function, and return a Result(Out) for success or failure
	/// </summary>
	/// <typeparam name="Out">Resulting type</typeparam>
	/// <param name="func">Function which returns type Out, and will be converted into Result(Out)</param>
	public static Result<Out> Try<Out>(Func<Out> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Success(func());
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Out>(ex);
		}
	}

	/// <summary>
	/// Call this function which directly returns Result(Out). Throwing is not necessary to signal failure
	/// </summary>
	/// <typeparam name="Out">Inner Result type</typeparam>
	/// <param name="func">Function which returns type Result(Out) directly</param>
	public static Result<Out> Call<Out>(Func<Result<Out>> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		try {
			return func();
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Out>(ex);
		}
	}

	/// <summary>
	/// Try this function, and return a Result(Out) for success or failure
	/// </summary>
	/// <typeparam name="In">Input type of function</typeparam>
	/// <typeparam name="Out">Result type Result(Out)</typeparam>
	/// <param name="func">Function to turn In into Out</param>
	/// <param name="param1">Parameter to be passed to action</param>
	public static Result<Out> Try<In, Out>(Func<In, Out> func, In param1)
	{
		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Success(func(param1));
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Out>(ex);
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
	/// Try this async function (which returns a Task(Out)), and return an awaitable Task(Result(Out))
	/// </summary>
	public static async Task<Result<Out>> TryAsync<Out>(Func<Task<Out>> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Success(await func()); // all returns are wrapped in a Task
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Out>(ex);
		}
	}

	/// <summary>
	/// Try this async function (which returns a Task(Out)), and return an awaitable Task(Result(Out)). Takes a In as input
	/// </summary>
	public static async Task<Result<Out>> TryAsync<In, Out>(Func<In, Task<Out>> func, In param1)
	{
		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		try {
			return ResultBuilder.Success(await func(param1)); // all returns are wrapped in a Task
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Out>(ex);
		}
	}

	/// <summary>
	/// Call this async function (which returns a Task(Result(Out))) directly. Throwing is not necessary to signal failure
	/// </summary>
	public static async Task<Result<Out>> CallAsync<Out>(Func<Task<Result<Out>>> func)
	{
		if (func == null) {
			return ResultBuilder.Failure<Out>(new ArgumentNullException(nameof(func)));
		}

		try {
			return await func();
		}
		catch (Exception ex) {
			return ResultBuilder.Failure<Out>(ex);
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

	/// <summary>
	/// Unit is always equal to another unit
	/// </summary>
	public bool Equals(Unit other) => true;

	public override int GetHashCode() => 0;
}
