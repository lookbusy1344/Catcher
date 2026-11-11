namespace Catcher;

internal static class Program
{
	public const string FileName = "CatcherExample.cs";
	public const string WordsFile = "wordcount.txt";

	private static Task Main()
	{
		CatcherExample.CheckEquality();
		CatcherExample.CheckUnion();

		return CatcherExample.GoAsync();
	}

	//private static void Main(string[] args)
	//{
	//	Console.WriteLine("Hello, World!");
	//	CatcherExample.Go();
	//}
}
