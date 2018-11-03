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

		public static void Info(string m)
		{
			Console.WriteLine("I: "+m);
		}

		public static void Warn(string w)
		{
			Console.Error.WriteLine("W: "+w);
		}

		public static void Debug(string m)
		{
			#if DEBUG
			Console.WriteLine("D: "+m);
			#endif
		}
	}
}