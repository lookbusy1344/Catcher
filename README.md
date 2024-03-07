# Catcher - a library for helping with exception free C#

First it's important to say, **stick with Exceptions they are idiomatic and far better than other forms of error handling** in C#. This is not Rust!

To quote *Framework Design Guidelines* page 250:

> **Always use exceptions for communicating errors**

> Return codes cannot be used for certain constructs, and an out-of-band mechanism must be used. Now the question becomes whether to use exceptions for everything or whether to use them for the special constructs and use return codes for methods. Obviously, having two different error reporting mechanisms is worse than having one, so it should be obvious
that exceptions should be used to report all errors for all code constructs.
> Always use exceptions for communicating errors

And on page 255:

> **DO NOT return error codes.**
> Report execution failures by throwing exceptions. If a member cannot successfully do what it is designed to do, it should be considered an execution failure, and an exception should be thrown.

> One of the **biggest misconceptions** about exceptions is that they are for “exceptional conditions.” The reality is that they are intended for communicating error conditions. From a framework design perspective, there is no such thing as an “exceptional condition.” **One man’s exceptional condition is another man’s chronic condition.**

Remember .NET contains tens of thousands of `throw`s just in the BCL, regardless of anything in your own code. Methods like `int.TryParse()` still throw **(they just dont throw for the one common case)**.

Every single method and constructor in the BCL can potentially throw, and most operators can too.

## However if you want to proceed

This is a small library to help you keep parts of your code notionally exception free, in a functional monadic style.

This is built around `Result<T>` which is a discriminated union of `T` and `Exception`. If the worker method succeeds, `T` is returned. If it throws, the exception is returned. It shouldn't be possible to throw out of these methods, anything that cannot be caught and returned leads to a `FailFast`.

When no return value is expected, we use `Result<Unit>` which contains no payload, just the exception if there is one.

## Traditional vs Catcher call signatures

|Traditional|Catcher|
|---|---|
|`int GetIntOrThrow()` | `Result<int> GetIntOrError()` |
|`void DoWorkOrThrow()` | `Result<Unit> DoWorkOrError()` |
|`Task<int> GetIntAsync()` | `Result<Task<int>> GetIntAsync()` |
|`Task DoWorkAsync()` | `Task<Result<Unit>> DoWorkAsync()` |

## How to use - starting a chain

Start your call with `Catcher.Try()` or `Catcher.TryAsync()`

```
Result<int> result = Catcher.Try(() => GetIntOrThrow());	// int result
Result<Unit> emptyresult = Catcher.Try(() => DoWorkOrThrow());	// void result

Task<Result<int>> awaitableresult = Catcher.TryAsync(() => GetIntAsync());	// async Task<int> result
Task<Result<Unit>> awaitableresult2 = Catcher.TryAsync(() => DoWorkAsync());	// async Task result
```

## Chaining calls

Then chain `.Then()`, `.Transform()` and `.Pipe()` calls to transform the result. These all turn a `Result<T>` into a `Result<U>` in different ways.

### Then()

This `Then()` example turns a `Result<int>` into a `Result<string>`. The lambda is bypassed entirely if the result is already in error, and the error transferred to the destination.

New failured can be signalled by throwing in the lamdba.

```
Result<string> str = result.Then(i => i.ToString());
Result<string> str = result.Then(i => throw new Exception("oh no!"));
```

### Transform()

This `Transform()` example turns a `Result<int>` into a `Result<string>`, using different lambdas for success and failure.

New failured can be signalled by throwing in the lamdba

```
Result<string> str = result.Transform(
	success: i => i.ToString(),
	failure: ex => ex.Message
);
```

### Pipe()

This `Pipe()` example turns a `Result<int>` into a `Result<decimal>`. Here the **full** `Result<int>` is passed to the lambda, which returns a full `Result<decimal>`.

Failured can be signalled **without** throwing, so this is particularly useful when writing exception-free code

```
Result<decimal> dec = result.Pipe(ires =>
	ires.IsSuccess ? ResultBuilder.Success((decimal)ires.ResultValue) : ResultBuilder.Failure<decimal>(ires.Error));
```

## Ending a chain

Finally call `.Unwrap()`, `.Match()` or `.Switch()` to handle the result.

### Unwrap()

`Unwrap()` fails-fast if there is an error

```
int value = result.Unwrap();
```

### Match()

`Match()` Unwraps the result, or returns a default value if there is an error

```
int value = result.Match(
	success: int.Parse,
	failure: _ => -1);
```

### Switch()

`Switch()` calls actions according to the result, success or failure

```
result.Switch(
	success: i => Console.WriteLine(i),
	failure: ex => Console.WriteLine($"ERROR: {ex.Message}")
);

```

## More examples

Some more complex example are below. Calls can be chained:

```
int result = Catcher.Try(() => step1)
	.Then(a => step2)
	.Then(b => 1)
	.Unwrap();

// returns Result<string?>
Result<string?> valuea = Catcher.Try(() =>
	(DateTime.Now.Ticks % 2 == 0) ? "Even" : null   // even or null
);

// transforms into Result<int?>
Result<int?> valueb = valuea.Then(s => s?.Length);   // length or null

// transforms into Result<string>, handling any embedded error
Result<string> valuec = valueb.Transform(
	success: i => i == null ? "" : "Yikes!",
	failure: _ => "nothing"
);

// Unwrap to a string, if there is no error
string finalresult = valuec.Unwrap();

// or switch on the result
valuec.Switch(
	success: s => Console.WriteLine(s),     // it worked
	failure: ex => Console.WriteLine($"ERROR: {ex.Message}")        // there was an exception
);
```

## Async code

Build a function that results a `Task<Result<T>>` instead of `Task<T>`. If there is no return value, use `Task<Result<Unit>>` instead of `Task`.

```
public static async Task<Result<int>> CountWordsInFileAsync(string fname) =>
	(await Catcher.TryAsync(() => File.ReadAllLinesAsync(fname)))
		.Then(contents => contents.Sum(line => line.Split(' ').Length));
```

And used like this:

```
Console.WriteLine("Count words:");
Result<int> words = await CountWordsInFileAsync(@"C:\files\wordcount.txt");

words.Switch(
	success: count => Console.WriteLine($"File has {count} words"),     // it worked
	failure: ex => Console.WriteLine($"ERROR: {ex.Message}")        // there was an exception
);

bool success = words.IsSuccess;   // true if it worked
```
