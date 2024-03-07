using System;

namespace Catcher;

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
			.Then(i => i switch
				{
					> 1000 => ResultBuilder.Success(i),
					_ => ResultBuilder.Failure<int>(new Exception($"Only {i} words found, this is an error"))
				});

	public static async Task GoAsync()
	{
		Console.WriteLine("First count:");
		var words = await CountWordsInFileAsync(@"C:\dev\wordcount.txt");

		words.Switch(
			success: count => Console.WriteLine($"File has {count} words"),
			failure: ex => Console.WriteLine($"ERROR: {ex.Message}")
		);

		Console.WriteLine("\r\nSecond count:");
		var words2 = await CountWordsInFile2Async(@"C:\dev\wordcount.txt");

		words2.Switch(
			success: count => Console.WriteLine($"File has {count} words"),
			failure: ex => Console.WriteLine($"ERROR: {ex.Message}")
		);
	}

	public static void Go()
	{
		var s = "hello";
		var nv = ResultBuilder.FromNullable(s);
		s = null;
		nv = ResultBuilder.FromNullable(s);

		var (str, err, issuccess) = nv;

		int? i = null;
		var nv2 = ResultBuilder.FromNullable(i);
		i = 1;
		nv2 = ResultBuilder.FromNullable(i);

		// ======

		// turn a Result<string?> into a Result<string>
		var x1 = Catcher.Try(() => (string?)"hello");
		var x2 = ResultBuilder.RemoveNullable(x1);

		// turn a Result<int?> into a Result<int>
		var x3 = Catcher.Try(() => (int?)3);
		var x4 = ResultBuilder.RemoveNullable(x3);

		// now turn a Result<int> into a Result<decimal> using Pipe
		var next = x4.Pipe(i =>
			i.IsSuccess ? ResultBuilder.Success((decimal)i.ResultValue) : ResultBuilder.Failure<decimal>(i.Error));

		// ======

		// checks for nullable types, references and value
		var null1 = Catcher.Try(() =>
			(DateTime.Now.Ticks % 2 == 0) ? "Hello" : null
		);
		var null2 = null1.Then(s => s?.Length);
		var null3 = null2.Transform(
			success: i => i == null ? "" : "Yikes!",
			//success: i => i == null ? null : "Yikes!",
			failure: _ => "nothing"
		);

		var xx = null3.Unwrap();
		var l = xx.Length;

		//======

		var work = Catcher.Try(() =>
		{
			// this returns long, and starts a chain with Result<long>
			Console.WriteLine("Step 1");
			var file = new FileInfo(Program.DefaultDbFile);
			if (!file.Exists)
				throw new FileNotFoundException($"Database file \"{file.FullName}\" not found.");

			return file.Length;
		})
			.Then(length =>
				{
					// chaining long -> Result<long>
					Console.WriteLine("Step 2");
					Console.WriteLine($"File size is {length}");
					if (length < 1000)
						return ResultBuilder.Failure<long>(new Exception("File too small"));
					else
						return ResultBuilder.Success(length + 1);
				})
			.Then(length =>
				{
					// chaining long -> Result<string>
					Console.WriteLine("Step 3");
					return length.ToString();
				})
			.Then(_ =>
				{
					// chaining disregard -> Result<string>
					Console.WriteLine("Step 4");
					return ResultBuilder.Success("Hello");
				})
			.OnError(
				// if there is an error in the chain, turn it into a success string
				ex => $"ERROR: {ex.Message}"
				)
			.Then(s => (int?)int.Parse(s!))
			.Then(i =>
			{
				// always return a failure
				Console.WriteLine($"Step 5 with {i}");
				return ResultBuilder.Failure<string>(new Exception("Step 4 fails"));
			});

		// Match the Result<string> into an int
		var matched = work.Match(
			success: s => int.Parse(s!),
			failure: _ => -1);

		// Transform the Result<string> into an Result<int>
		var transform1 = work.Transform(
			success: s => ResultBuilder.Success(int.Parse(s!)),
			failure: _ => ResultBuilder.Success(-1));

		// Transform the Result<string> into an Result<int>
		var transform2 = work.Transform(
			success: s => int.Parse(s!),
			failure: _ => -1);

		// transforming into a Result<int?>
		var transform3 = work.Transform(
			success: s => (int?)int.Parse(s!),
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
}
