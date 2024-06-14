using System;

namespace Catcher;

#pragma warning disable IDE0059 // Unnecessary assignment of a value

internal static class CatcherExample
{
	/// <summary>
	/// Count words in file, returning a Result(int)
	/// </summary>
	public static Result<int> CountWordsInFile(string fname) =>
		Catcher.Try(() => File.ReadAllLines(fname))
			.Then(contents => contents.Sum(line => line.Split(' ').Length))
			.OnError(_ => new Exception("Failed to count words in file"));

	public static async Task<Result<int>> CountWordsInFileAsync(string fname) =>
		(await Catcher.TryAsync(() => File.ReadAllLinesAsync(fname)))
			.Then(contents => contents.Sum(line => line.Split(' ').Length));

	public static async Task<Result<int>> CountWordsInFile2Async(string fname) =>
		(await Catcher.TryAsync(() => File.ReadAllLinesAsync(fname)))
			.Then(contents => contents.Sum(line => line.Split(' ').Length))
			.Then(i => i switch {
				> 1000 => ResultBuilder.Success(i),
				_ => ResultBuilder.Failure<int>(new Exception($"Only {i} words found, this is an error"))
			});

	public static async Task GoAsync()
	{
		Console.WriteLine("First count:");
		var words = await CountWordsInFileAsync(Program.WordsFile);

		words.Switch(
			success: count => Console.WriteLine($"File has {count} words"),
			failure: ex => Console.WriteLine($"ERROR: {ex.Message}")
		);

		Console.WriteLine("\r\nSecond count:");
		var words2 = await CountWordsInFile2Async(Program.WordsFile);

		words2.Switch(
			success: count => Console.WriteLine($"File has {count} words"),
			failure: ex => Console.WriteLine($"ERROR: {ex.Message}")
		);
	}

	public static void CheckEquality()
	{
		var a = ResultBuilder.Success(1);
		var b = ResultBuilder.Success(1);
		Assert(a == b); // both success and match

		b = ResultBuilder.Success(2);
		Assert(a != b); // both success but don't match

		b = ResultBuilder.Failure<int>(new Exception("a"));
		Assert(a != b); // one success, one failure

		a = ResultBuilder.Failure<int>(new Exception("another"));
		Assert(a != b); // both failure but don't match

		a = ResultBuilder.Failure<int>(new Exception("a"));
		Assert(a == b); // both failure and match

		var hash1 = a.GetHashCode();
		var hash2 = b.GetHashCode();

		a = ResultBuilder.Failure<int>(new NullReferenceException("a"));
		Assert(a != b); // both failure and messages same but different types

		hash1 = a.GetHashCode();
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
			(DateTime.Now.Ticks % 2 == 0) ? "Hello" : null
		);
		var lengthResult = oddResult.Then(s => s?.Length);
		var messageResult = lengthResult.Transform(
			success: i => i == null ? "" : "Yikes!",
			failure: _ => "nothing"
		);

		var unwrapped = messageResult.Unwrap();
		var l = unwrapped.Length;

		//======

		var work = Catcher.Try(() => {
			// this returns long, and starts a chain with Result<long>
			Console.WriteLine("Step 1");
			var file = new FileInfo(Program.FileName);
			if (!file.Exists) {
				throw new FileNotFoundException($"Database file \"{file.FullName}\" not found.");
			}

			return file.Length;
		})
			.Then(length => {
				// chaining long -> Result<long>
				Console.WriteLine("Step 2");
				Console.WriteLine($"File size is {length}");
				if (length < 1000) {
					return ResultBuilder.Failure<long>(new Exception("File too small"));
				} else {
					return ResultBuilder.Success(length + 1);
				}
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
				return ResultBuilder.Failure<string>(new Exception("Step 4 fails"));
			});

		// Match the Result<string> into an int. If failure, return -1
		var matched = work.Match(
			success: int.Parse,
			failure: _ => -1);

		// Transform the Result<string> into an Result<int>. If failure, return Success(-1)
		var transform1 = work.Transform(
			success: s => ResultBuilder.Success(int.Parse(s)),
			failure: _ => ResultBuilder.Success(-1));

		// Transform the Result<string> into an Result<int>
		// unlike Match, this will return a Result<int> not an int
		var transform2 = work.Transform(
			success: int.Parse,
			failure: _ => -1);

		// transforming from Result<string> to Result<int?> 
		var transform3 = work.Transform(
			success: s => (int?)int.Parse(s),
			failure: _ => null);

		// Unwrap the result, or fail-fast
		var final = work.Unwrap();
		_ = transform1.Unwrap();

		// switch on the result
		work.Switch(
			success: s => Console.WriteLine($"Final success is {s}"),
			failure: ex => Console.WriteLine($"ERROR: {ex.Message}")
		);
	}

	private static void Assert(bool b)
	{
		if (!b) {
			throw new Exception("Assertion failed");
		}
	}
}
