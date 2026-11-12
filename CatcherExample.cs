namespace Catcher;

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0059 // Unnecessary assignment of a value

internal static class CatcherExample
{
	/// <summary>
	/// Count words in file, returning a Result(int)
	/// </summary>
	public static Result<int> CountWordsInFile(string fname) =>
		Catcher.Try(() => File.ReadAllLines(fname))
			.Then(contents => contents.Sum(line => line.Split(' ').Length))
			.OnError(_ => new("Failed to count words in file"));

	public static Task<Result<int>> CountWordsInFileAsync(string fname) =>
		Catcher.TryAsync(() => File.ReadAllLinesAsync(fname))
		.Then(contents => contents.Sum(line => line.Split(' ').Length));

	public static Task<Result<int>> CountWordsInFile2Async(string fname) =>
		Catcher.TryAsync(() => File.ReadAllLinesAsync(fname))
		.Then(contents => contents.Sum(line => line.Split(' ').Length))
		.Then(i => i switch {
			> 1000 => ResultBuilder.Success(i),
			_ => ResultBuilder.Failure<int>(new($"Only {i} words found, this is an error"))
		});

	public static async Task GoAsync()
	{
		Console.WriteLine("First count:");
		var words = await CountWordsInFileAsync(Program.WordsFile);

		words.Switch(
			count => Console.WriteLine($"File has {count} words"),
			ex => Console.WriteLine($"ERROR: {ex.Message}")
		);

		Console.WriteLine("\r\nSecond count:");
		var words2 = await CountWordsInFile2Async(Program.WordsFile);

		words2.Switch(
			count => Console.WriteLine($"File has {count} words"),
			ex => Console.WriteLine($"ERROR: {ex.Message}")
		);
	}

	public static void CheckEquality()
	{
		var a = ResultBuilder.Success(1);
		var b = ResultBuilder.Success(1);
		Assert(a == b); // both success and match

		b = ResultBuilder.Success(2);
		Assert(a != b); // both success but don't match

		b = ResultBuilder.Failure<int>(new("a"));
		Assert(a != b); // one success, one failure

		a = ResultBuilder.Failure<int>(new("another"));
		Assert(a != b); // both failure but don't match

		a = ResultBuilder.Failure<int>(new("a"));
		Assert(a == b); // both failure and match

		var hash1 = a.GetHashCode();
		var hash2 = b.GetHashCode();

		a = ResultBuilder.Failure<int>(new NullReferenceException("a"));
		Assert(a != b); // both failure and messages same but different types

		hash1 = a.GetHashCode();
	}

	public static void CheckUnion()
	{
		const int answer = 42;
		const string boom = "boom";
		var exception = new InvalidOperationException(boom);

		// implicit union conversions from each case type
		Result<int> ok = answer;
		Result<int> bad = exception;
		Assert(ok.IsSuccess && ok.ResultValue == answer);
		Assert(bad.IsError && ReferenceEquals(bad.Error, exception));

		// exhaustive switch over the case types, no discard arm
		Assert(Describe(ok) == "ok 42");
		Assert(Describe(bad) == "error boom");

		// non-boxing access pattern
		Assert(ok.TryGetValue(out int okValue) && okValue == answer);
		Assert(!ok.TryGetValue(out Exception? _));
		Assert(!bad.TryGetValue(out int _));
		Assert(bad.TryGetValue(out Exception? badError) && ReferenceEquals(badError, exception));
		Assert(ok.HasValue);
		Assert(bad.HasValue);

		// boxed access through IUnion.Value
		Assert(ok.Value is int boxed && boxed == answer);
		Assert(ReferenceEquals(bad.Value, exception));

		// a success holding null has no contents, but is still a success
		var nullString = ResultBuilder.Success<string?>(null);
		Assert(nullString.IsSuccess);
		Assert(!nullString.HasValue);
		Assert(nullString.Value is null);
		Assert(!nullString.TryGetValue(out string? _));
		Assert(nullString is null);
		Assert(DescribeNullable(nullString) == "null");
		Assert(DescribeNullable(ResultBuilder.Success<string?>("hi")) == "ok hi");
		Assert(DescribeNullable(ResultBuilder.Failure<string?>(exception)) == "error boom");

		// nullable value type case: null is no contents, a value matches the underlying type
		const int five = 5;
		Result<int?> nullInt = (int?)null;
		Result<int?> someInt = (int?)five;
		Assert(nullInt.IsSuccess && !nullInt.HasValue && !nullInt.TryGetValue(out int? _));
		Assert(someInt.TryGetValue(out int? someValue) && someValue == five);
		Assert(someInt is int unwrapped && unwrapped == five);

		// default is a failure, so an exhaustive switch over the case types covers it
		const string uninitialized = "Result is uninitialized";
		Result<int> zero = default;
		Assert(zero.IsError && !zero.IsSuccess && zero.HasValue);
		Assert(zero.Error is InvalidOperationException { Message: uninitialized });
		Assert(!zero.TryGetValue(out int _));
		Assert(zero.TryGetValue(out Exception? zeroError) && zeroError is InvalidOperationException);
		Assert(zero.Value is InvalidOperationException);
		Assert(Describe(zero) == $"error {uninitialized}");
		Assert(DescribeString(default) == $"error {uninitialized}");
		Assert(default(Result<int>) == default(Result<int>));
		Assert(zero != ResultBuilder.Success(0));
		Assert(ResultBuilder.Success(0).IsSuccess);
		Assert(ReferenceEquals(zero.Then(i => i + 1).Error, zero.Error));

		// overlapping case types: an Exception is always the failure case, and patterns agree with Value
		var argument = new ArgumentException("arg");
		Result<object> routed = ResultBuilder.Success<object>(argument);
		Assert(routed.IsError && ReferenceEquals(routed.Error, argument));
		Assert(routed == ResultBuilder.Failure<object>(argument));
		Assert(ok.Then(_ => (object)argument).IsError);
		Assert(Catcher.Try(() => (object)argument).IsError);
		Result<object> objectFailure = argument;
		Assert(objectFailure is Exception);
		Assert(objectFailure is ArgumentException boundArgument && ReferenceEquals(boundArgument, argument));
		Assert(objectFailure is object);
		Assert(objectFailure is not string);
		Assert(objectFailure.TryGetValue(out object? boundObject) && ReferenceEquals(boundObject, argument));
		Result<object> objectSuccess = "text";
		Assert(objectSuccess is string && objectSuccess is not Exception && objectSuccess is not ArgumentException);
		Result<System.Runtime.Serialization.ISerializable> serializable = argument;
		Assert(serializable.TryGetValue(out System.Runtime.Serialization.ISerializable? boundSerializable)
			&& ReferenceEquals(boundSerializable, argument));
		Assert(!bad.TryGetValue(out int _));
		Assert(!ResultBuilder.Failure<string>(exception).TryGetValue(out string? _));

		// equality through union conversion, and Equals(object) on unrelated types
		Assert(ok == answer);
		Assert(bad == new InvalidOperationException(boom));
		Assert(ok.Equals((object)ResultBuilder.Success(answer)));
		Assert(!ok.Equals("not a result"));
		Assert(!ok.Equals((object?)null));

		// null converts to the Exception case, which becomes an ArgumentNullException failure
		var nullError = ResultBuilder.Failure<int>(null!);
		Assert(nullError.Error is ArgumentNullException { ParamName: "error" });
		Assert(ok != null!);

		static string Describe(Result<int> result) => result switch {
			int i => $"ok {i}",
			Exception e => $"error {e.Message}",
		};

		static string DescribeString(Result<string> result) => result switch {
			string s => $"ok {s}",
			Exception e => $"error {e.Message}",
		};

		static string DescribeNullable(Result<string?> result) => result switch {
			string s => $"ok {s}",
			Exception e => $"error {e.Message}",
			null => "null",
		};
	}

	public static void CheckChaining()
	{
		const int answer = 42;
		var boom = new InvalidOperationException("boom");
		var replacement = new FormatException("replaced");
		Result<int> ok = answer;
		Result<int> bad = boom;

		// Catcher entry points
		Assert(Catcher.Try(() => answer) == answer);
		Assert(ReferenceEquals(Catcher.Try<int>(() => throw boom).Error, boom));
		Assert(Catcher.Try((Func<int>)null!).Error is ArgumentNullException { ParamName: "func" });
		Assert(Catcher.Try(() => { }).IsSuccess);
		Assert(Catcher.Try((Action)null!).Error is ArgumentNullException { ParamName: "action" });
		Assert(ReferenceEquals(Catcher.Try(() => throw boom).Error, boom));
		Assert(Catcher.Try((int i) => i + 1, answer) == answer + 1);
		Assert(ReferenceEquals(Catcher.Call<int>(() => boom).Error, boom));
		Assert(ReferenceEquals(Catcher.Call<int>(() => throw boom).Error, boom));

		// Then: success maps, failure passes through by reference, throws and null delegates become failures
		Assert(ok.Then(i => i.ToString(CultureInfo.InvariantCulture)) == "42");
		Assert(ReferenceEquals(bad.Then(i => i + 1).Error, boom));
		Assert(ReferenceEquals(ok.Then<int>(_ => throw boom).Error, boom));
		Assert(ok.Then((Func<int, int>)null!).Error is ArgumentNullException { ParamName: "func" });
		Assert(ok.Then(i => (Result<int>)(i + 1)) == answer + 1);
		Assert(ReferenceEquals(ok.Then(_ => (Result<int>)boom).Error, boom));
		Assert(ReferenceEquals(bad.Then(i => (Result<int>)(i + 1)).Error, boom));
		Assert(ok.Then((Func<int, Result<int>>)null!).Error is ArgumentNullException { ParamName: "func" });
		var seen = 0;
		Assert(ok.Then(i => { seen = i; }).IsSuccess && seen == answer);
		Assert(ReferenceEquals(bad.Then(_ => { }).Error, boom));
		Assert(ok.Then((Action<int>)null!).Error is ArgumentNullException { ParamName: "action" });

		// Pipe passes the whole Result
		Assert(bad.Pipe(r => (Result<bool>)r.IsError) == true);
		Assert(ReferenceEquals(ok.Pipe<int>(_ => throw boom).Error, boom));
		Assert(ok.Pipe((Func<Result<int>, Result<int>>)null!).Error is ArgumentNullException { ParamName: "func" });

		// OnError: success passes through, failure recovers or replaces
		Assert(ok.OnError(_ => -1) == answer);
		Assert(bad.OnError(_ => -1) == -1);
		Assert(bad.OnError((Func<Exception, int>)null!).Error is ArgumentNullException { ParamName: "func" });
		Assert(bad.OnError(_ => (Result<int>)(-1)) == -1);
		Assert(ReferenceEquals(bad.OnError((Func<Exception, Result<int>>)(_ => throw replacement)).Error, replacement));
		Assert(ReferenceEquals(bad.OnError(replacement).Error, replacement));
		Assert(ok.OnError(replacement) == answer);
		Assert(bad.OnError((Exception)null!).Error is ArgumentNullException { ParamName: "exception" });
		Assert(ReferenceEquals(bad.OnError(_ => (Exception)replacement).Error, replacement));
		Assert(ReferenceEquals(bad.OnError((Func<Exception, Exception>)(_ => throw replacement)).Error, replacement));
		Assert(bad.OnError((Func<Exception, Exception>)null!).Error is ArgumentNullException { ParamName: "func" });

		// Transform: both states map to a success, throws become failures
		Assert(ok.Transform(i => i + 1, _ => -1) == answer + 1);
		Assert(bad.Transform(i => i + 1, _ => -1) == -1);
		Assert(ReferenceEquals(bad.Transform<int>(i => i, _ => throw replacement).Error, replacement));
		Assert(bad.Transform(i => (Result<int>)i, e => (Result<int>)e.Message.Length) == "boom".Length);
		Assert(ReferenceEquals(ok.Transform(_ => throw replacement, e => (Result<int>)e).Error, replacement));

		// RemoveNullable as an extension member, reference and value types
		Result<string?> someString = "hi";
		Result<string?> nullString = (string?)null;
		Result<string?> badString = boom;
		Assert(someString.RemoveNullable() == "hi");
		Assert(nullString.RemoveNullable().Error is ArgumentNullException { ParamName: "result" });
		Assert(ReferenceEquals(badString.RemoveNullable().Error, boom));
		Result<int?> someInt = (int?)answer;
		Result<int?> nullInt = (int?)null;
		Result<int?> badInt = boom;
		Assert(someInt.RemoveNullable() == answer);
		Assert(nullInt.RemoveNullable().Error is ArgumentNullException { ParamName: "result" });
		Assert(ReferenceEquals(badInt.RemoveNullable().Error, boom));
	}

	public static async Task CheckAsync()
	{
		const int answer = 42;
		var boom = new InvalidOperationException("boom");
		Result<int> ok = answer;
		Result<int> bad = boom;

		// ThenAsync passes the value on, failures pass through by reference
		Assert(await ok.ThenAsync(i => Task.FromResult(i + 1)) == answer + 1);
		Assert(ReferenceEquals((await bad.ThenAsync(i => Task.FromResult(i + 1))).Error, boom));
		Assert(ReferenceEquals((await ok.ThenAsync<int>(_ => Task.FromException<int>(boom))).Error, boom));
		Assert(ReferenceEquals((await ok.ThenAsync<int>(_ => throw boom)).Error, boom));
		Assert((await ok.ThenAsync((Func<int, Task<int>>)null!)).Error is ArgumentNullException { ParamName: "func" });

		// ThenAsync returning Result directly
		Assert(await ok.ThenAsync(i => Task.FromResult((Result<int>)(i + 1))) == answer + 1);
		Assert(ReferenceEquals((await ok.ThenAsync(_ => Task.FromResult((Result<int>)boom))).Error, boom));
		Assert(ReferenceEquals((await bad.ThenAsync(i => Task.FromResult((Result<int>)(i + 1)))).Error, boom));
		Assert(ReferenceEquals((await ok.ThenAsync(_ => Task.FromException<Result<int>>(boom))).Error, boom));
		Assert((await ok.ThenAsync((Func<int, Task<Result<int>>>)null!)).Error is ArgumentNullException { ParamName: "func" });

		// chaining directly on Task<Result<T>>
		var okTask = Task.FromResult(ok);
		var badTask = Task.FromResult(bad);
		Assert(await okTask.Then(i => i + 1) == answer + 1);
		Assert(ReferenceEquals((await badTask.Then(i => i + 1)).Error, boom));
		Assert(ReferenceEquals((await okTask.Then<int, int>(_ => throw boom)).Error, boom));
		Assert(await okTask.Then(i => (Result<int>)(i + 1)) == answer + 1);
		Assert(ReferenceEquals((await okTask.Then(_ => (Result<int>)boom)).Error, boom));
		Assert(await okTask.ThenAsync(i => Task.FromResult(i + 1)) == answer + 1);
		Assert(await okTask.ThenAsync(i => Task.FromResult((Result<int>)(i + 1))) == answer + 1);
		Assert(ReferenceEquals((await badTask.ThenAsync(i => Task.FromResult(i + 1))).Error, boom));
		Assert(await okTask.Match(i => i + 1, _ => -1) == answer + 1);
		Assert(await badTask.Match(i => i + 1, _ => -1) == -1);
		Assert(await Catcher.TryAsync(() => Task.FromResult(answer)).Then(i => i + 1).ThenAsync(i => Task.FromResult(i * 2))
			== (answer + 1) * 2);

		// a faulted or null task becomes a failure
		Assert(ReferenceEquals((await Task.FromException<Result<int>>(boom).Then(i => i + 1)).Error, boom));
		Assert((await ((Task<Result<int>>)null!).Then(i => i + 1)).Error is ArgumentNullException { ParamName: "task" });
		Assert(await ((Task<Result<int>>)null!).Match(_ => "ok", e => e.GetType().Name) == nameof(ArgumentNullException));
	}

	public static void Go()
	{
		var s = "hello";
		var stringResult = ResultBuilder.FromNullable(s);
		s = null;
		stringResult = ResultBuilder.FromNullable(s);

		// de-structure the result
		var (str, err, issuccess) = stringResult;

		int? i = null;
		var intResult = ResultBuilder.FromNullable(i);
		i = 1;
		intResult = ResultBuilder.FromNullable(i);

		// ======

		// turn a Result<string?> into a Result<string>
		var stringResult1 = Catcher.Try(() => (string?)"hello");
		var stringResult2 = stringResult1.RemoveNullable();

		// turn a Result<int?> into a Result<int>
		var intResult1 = Catcher.Try(() => (int?)3);
		var intResult2 = intResult1.RemoveNullable();

		// now turn a Result<int> into a Result<decimal> using Pipe
		var decimalResult = intResult2.Pipe<decimal>(i =>
			i.IsSuccess ? (decimal)i.ResultValue : i.Error);

		// ======

		// checks for nullable types, references and value
		var oddResult = Catcher.Try(() =>
			DateTime.Now.Ticks % 2 == 0 ? "Hello" : null
		);
		var lengthResult = oddResult.Then(s => s?.Length);
		var messageResult = lengthResult.Transform(
			i => i == null ? "" : "Yikes!",
			_ => "nothing"
		);

		var unwrapped = messageResult.Unwrap();
		var l = unwrapped.Length;

		//======

		var work = Catcher.Try(() => {
			// this returns long, and starts a chain with Result<long>
			Console.WriteLine("Step 1");
			var file = new FileInfo(Program.FileName);
			return !file.Exists ? throw new FileNotFoundException($"Database file \"{file.FullName}\" not found.") : file.Length;
		})
			.Then(length => {
				// chaining long -> Result<long>
				Console.WriteLine("Step 2");
				Console.WriteLine($"File size is {length}");
				return length < 1000
					? ResultBuilder.Failure<long>(new("File too small"))
					: ResultBuilder.Success(length + 1);
			})
			.Then(length => {
				// chaining long -> Result<string>
				Console.WriteLine("Step 3");
				return length.ToString();
			})
			.Then(_ => {
				// chaining disregard -> Result<string>
				Console.WriteLine("Step 4");
				return ResultBuilder.Success("Hello");
			})
			.OnError(
				// if there is an error in the chain, turn it into a success string
				ex => $"ERROR: {ex.Message}"
			)
			.Then(s => (int?)int.Parse(s!))
			.Then(i => {
				// always return a failure
				Console.WriteLine($"Step 5 with {i}");
				return ResultBuilder.Failure<string>(new("Step 4 fails"));
			});

		// Match the Result<string> into an int. If failure, return -1
		var matched = work.Match(
			int.Parse,
			_ => -1);

		// Transform the Result<string> into an Result<int>. If failure, return Success(-1)
		var transform1 = work.Transform(
			s => ResultBuilder.Success(int.Parse(s)),
			_ => ResultBuilder.Success(-1));

		// Transform the Result<string> into an Result<int>
		// unlike Match, this will return a Result<int> not an int
		var transform2 = work.Transform(
			int.Parse,
			_ => -1);

		// transforming from Result<string> to Result<int?> 
		var transform3 = work.Transform(
			s => (int?)int.Parse(s),
			_ => null);

		// Unwrap the result, or fail-fast
		var final = work.Unwrap();
		_ = transform1.Unwrap();

		// switch on the result
		work.Switch(
			s => Console.WriteLine($"Final success is {s}"),
			ex => Console.WriteLine($"ERROR: {ex.Message}")
		);
	}

	private static void Assert(bool b, [CallerArgumentExpression(nameof(b))] string? expression = null)
	{
		if (!b) {
			throw new($"Assertion failed: {expression}");
		}
	}
}
