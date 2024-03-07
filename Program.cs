namespace Catcher;

internal static class Program
{
	public static string DefaultDbFile = "CatcherExample.cs";

	private static async Task Main()
	{
		await CatcherExample.GoAsync();
	}

	//private static void Main(string[] args)
	//{
	//	Console.WriteLine("Hello, World!");
	//	CatcherExample.Go();
	//}
}
