namespace Catcher;

// These mirror the Result API on the caller's Task, so they keep Result's names and await a task they did not start
#pragma warning disable VSTHRD200, VSTHRD003

/// <summary>
/// Chaining on Task(Result(In)), so async chains need no parentheses around each await.
/// A null or faulted task becomes a failure
/// </summary>
public static class ResultTaskExtensions
{
	extension<In>(Task<Result<In>> task)
	{
		/// <summary>
		/// Await the task, then chain a Func that takes the result as a parameter, and returns Out
		/// </summary>
		public async Task<Result<Out>> Then<Out>(Func<In, Out> func) => (await Settle(task)).Then(func);

		/// <summary>
		/// Await the task, then chain a Func that takes the result as a parameter, and returns Result(Out) directly
		/// </summary>
		public async Task<Result<Out>> Then<Out>(Func<In, Result<Out>> func) => (await Settle(task)).Then(func);

		/// <summary>
		/// Await the task, then chain an async Func that takes the result as a parameter, and returns Out
		/// </summary>
		public async Task<Result<Out>> ThenAsync<Out>(Func<In, Task<Out>> func) =>
			await (await Settle(task)).ThenAsync(func);

		/// <summary>
		/// Await the task, then chain an async Func that takes the result as a parameter, and returns Result(Out) directly
		/// </summary>
		public async Task<Result<Out>> ThenAsync<Out>(Func<In, Task<Result<Out>>> func) =>
			await (await Settle(task)).ThenAsync(func);

		/// <summary>
		/// Await the task, then turn success or failure into a common type Out. Lambdas that throw fail fast
		/// </summary>
		public async Task<Out> Match<Out>(Func<In, Out> success, Func<Exception, Out> failure) =>
			(await Settle(task)).Match(success, failure);
	}

	/// <summary>
	/// Await the task without throwing. A null task or a faulted task becomes a failure
	/// </summary>
	private static async Task<Result<In>> Settle<In>(Task<Result<In>> task)
	{
		if (task == null) {
			return new ArgumentNullException(nameof(task));
		}

		try {
			return await task;
		}
		catch (Exception ex) {
			return ex;
		}
	}
}
