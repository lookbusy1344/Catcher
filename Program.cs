namespace Catcher;

internal static class Program
{
	public const string FileName = "CatcherExample.cs";
	public const string WordsFile = "wordcount.txt";

	private static async Task Main()
	{
		CatcherExample.CheckEquality();
		CatcherExample.CheckUnion();
		CatcherExample.CheckChaining();
		await CatcherExample.CheckAsync();

		await CatcherExample.GoAsync();
	}

	//private static void Main(string[] args)
	//{
	//	Console.WriteLine("Hello, World!");
	//	CatcherExample.Go();
	//}
}
