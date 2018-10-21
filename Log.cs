using System;

namespace ArtAscii
{
	public static class Log
	{
		public static void Message(string m)
		{
			Console.WriteLine(m);
		}

		public static void Error(string e)
		{
			Console.Error.WriteLine("E: "+e);
		}
	}
}