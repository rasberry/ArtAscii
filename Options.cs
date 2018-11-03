using System;
using SixLabors.Fonts;
using System.Text;

namespace ArtAscii
{
	public static class Options
	{
		public static bool HandleArgs(string[] args)
		{
			if (args.Length < 1 || !ParseArgs(args)) {
				Usage();
				return false;
			}
			return true;
		}

		static void Usage()
		{
			Log.Message("Usage: "+nameof(ArtAscii)+" [options] (input image) [output file]"
				+"\n Options:"
				+"\n  -c  (character set)   A set of characters to use for the mapping"
				+"\n  -lf                   List system fonts and exit"
				+"\n  -sf (font name)       Use system font with given name"
				+"\n  -f  (font file)       Font file used for rendering characters"
				+"\n  -fs (style)           Choose a font style"
				+"\n  -tw (number)          Width in characters of output"
				+"\n  -th (number)          Height in characters of output"
				+"\n  -it (text file)       A utf-8 encoded file to use as the source of characters"
				+"\n  -ot                   Output as text instead of an image"
				+"\n\n Characer Sets:"
			);
			var nameList = Enum.GetNames(typeof(CharSets.Set));
			foreach(string name in nameList) {
				Log.Message("  "+name);
			}
		}

		static bool ParseArgs(string[] args)
		{
			for(int a=0; a<args.Length; a++)
			{
				string curr = args[a];
				if (curr == "-c" && ++a < args.Length) {
					CharSets.Set whichSet = CharSets.Set.None;
					if (!Enum.TryParse<CharSets.Set>(args[a],true,out whichSet)) {
						Log.Error("Unknown characer set");
						return false;
					}
					WhichSet = whichSet;
				}
				else if (curr == "-f" && ++a < args.Length) {
					FontFile = args[a];
				}
				else if (curr == "-fs" && ++a < args.Length) {
					string s = args[a]?.ToLowerInvariant();
					if (!Enum.TryParse<FontStyle>(s,out StyleOfFont)) {
						char first = s[0];
						if (first == 'r') { StyleOfFont = FontStyle.Regular; }
						else if (first == 'i') { StyleOfFont = FontStyle.Italic; }
						else if (first == 'b') { StyleOfFont = FontStyle.Bold; }
						else if (s.StartsWith("bi")) { StyleOfFont = FontStyle.BoldItalic; }
						else {
							Log.Error("unrecognized font style '"+s+"'");
							return false;
						}
					}
				}
				else if (curr == "-tw" && ++a < args.Length) {
					if (!int.TryParse(args[a],out CharWidth)) {
						Log.Error("Invalid width");
						return false;
					}
				}
				else if (curr == "-th" && ++a < args.Length) {
					if (!int.TryParse(args[a],out CharHeight)) {
						Log.Error("Invalid height");
						return false;
					}
				}
				else if (curr == "-it" && ++a < args.Length) {
					CharFile = args[a];
				}
				else if (curr == "-lf") {
					ListFonts = true;
				}
				else if (curr == "-sf" && ++a < args.Length) {
					SystemFont = args[a];
				}
				else if (curr == "-ot") {
					OutputText = true;
				}
				else if (null == InputName) {
					InputName = curr;
				}
				else if (null == OutputName) {
					OutputName = curr;
				}
			}

			//Set any dynamic defaults here
			if (String.IsNullOrWhiteSpace(OutputName)) {
				OutputName = nameof(ArtAscii).ToLowerInvariant()
					+ "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
			}
			string suffix = OutputText ? ".txt" : ".png";
			if (!OutputName.EndsWith(suffix)) {
				OutputName += suffix;
			}

			return true;
		}

		public static void ListSystemFonts()
		{
			Log.Message("List of System Fonts: B=Bold I=Italic R=Regular BI=BoldItalic");
			var list = System.Linq.Enumerable.OrderBy(SystemFonts.Families,
				f => f.Name, StringComparer.CurrentCultureIgnoreCase
			);
			foreach(var fam in list) {
				var sb = new StringBuilder();
				bool isFirst = true;
				var stylList = System.Linq.Enumerable.Distinct(
					System.Linq.Enumerable.OrderBy(fam.AvailableStyles,s => s));
				foreach(var styl in stylList) {
					if (!isFirst) { sb.Append(" "); }
					switch(styl) {
					default:
					case FontStyle.Regular: sb.Append("R"); break;
					case FontStyle.Italic: sb.Append("I"); break;
					case FontStyle.Bold: sb.Append("B"); break;
					case FontStyle.BoldItalic: sb.Append("BI"); break;
					}
					isFirst = false;
				}
				Log.Message(" "+fam.Name+"\t["+sb.ToString()+"]");
			}
			return;
		}

		//Options
		public static CharSets.Set WhichSet = CharSets.Set.None;
		public static string FontFile = null;
		public static int CharWidth = 0;
		public static int CharHeight = 0;
		public static string CharFile = null;
		public static string SystemFont = null;
		public static bool ListFonts = false;
		public static FontStyle StyleOfFont = FontStyle.Regular;
		public static string OutputName = null;
		public static string InputName = null;
		public static bool OutputText = false;
	}
}