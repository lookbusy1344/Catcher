﻿namespace Catcher;

internal static class Program
{
	public const string FileName = "CatcherExample.cs";
	public const string WordsFile = @"C:\dev\wordcount.txt";

#pragma warning disable IDE0022 // Use expression body for method

	private static async Task Main()
	{
		CatcherExample.CheckEquality();

		await CatcherExample.GoAsync();
	}

	//private static void Main(string[] args)
	//{
	//	Console.WriteLine("Hello, World!");
	//	CatcherExample.Go();
	//}
}
