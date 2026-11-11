namespace Catcher;

using System;
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

	public static async Task<Result<int>> CountWordsInFileAsync(string fname) =>
		(await Catcher.TryAsync(() => File.ReadAllLinesAsync(fname)))
		.Then(contents => contents.Sum(line => line.Split(' ').Length));

	public static async Task<Result<int>> CountWordsInFile2Async(string fname) =>
		(await Catcher.TryAsync(() => File.ReadAllLinesAsync(fname)))
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

		// default is a success holding default(T)
		Result<int> zero = default;
		Assert(zero.IsSuccess && zero.HasValue);
		Assert(zero.TryGetValue(out int zeroValue) && zeroValue == 0);
		Assert(zero.Value is int boxedZero && boxedZero == 0);

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

		static string DescribeNullable(Result<string?> result) => result switch {
			string s => $"ok {s}",
			Exception e => $"error {e.Message}",
			null => "null",
		};
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
		var stringResult2 = ResultBuilder.RemoveNullable(stringResult1);

		// turn a Result<int?> into a Result<int>
		var intResult1 = Catcher.Try(() => (int?)3);
		var intResult2 = ResultBuilder.RemoveNullable(intResult1);

		// now turn a Result<int> into a Result<decimal> using Pipe
		var decimalResult = intResult2.Pipe(i =>
			i.IsSuccess ? ResultBuilder.Success((decimal)i.ResultValue) : ResultBuilder.Failure<decimal>(i.Error));

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
