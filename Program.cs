using System;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;
using System.IO;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp.Advanced;

namespace ArtAscii
{
	/* TODO
		= maybe allow a set of input images instead of using a font
			= this would be another render mode - for collage art
			= would need to put back color matching instead of always using ToGray
			= could also allow a single 'source' image that gets cut up and used as the sprites
			= hummm.. so if you used a single image as both input and output
			 - it would just be puzzling the pieces back together as they were ?
		= TODO RenderArt resize is the naive implementation - maybe also do a more advanced version
			humm.. actually i was thinking about grabbing each box of pixels, matching the size of the sprite
			then finding the one that matches the closest (using diff or something)
	*/
	static class Program
	{
		static void Main(string[] args)
		{
			if (!Options.HandleArgs(args)) { return ;}

			try {
				MainMain();
			} catch(Exception e) {
				Log.Error(e.ToString());
			} finally {
				Dispose();
			}
		}

		static void MainMain()
		{
			if (Options.ListFonts) {
				Options.ListSystemFonts();
				return;
			}
			if (!LoadResources()) {
				return;
			}

			Size dim = CreateCharSprites(SelectedCharSet,SelectedFont);

			//If the character render width/height are not set use the image dimensions
			if (Options.CharWidth < 1 && Options.CharHeight < 1) {
				Options.CharWidth = SourceImage.Width / dim.Width;
				Options.CharHeight = SourceImage.Height / dim.Height;
			} else if (Options.CharWidth < 1) {
				double ratio = (double)SourceImage.Width / SourceImage.Height;
				Options.CharWidth = (int)(ratio * Options.CharHeight);
			} else if (Options.CharHeight < 1) {
				double ratio = (double)SourceImage.Height / SourceImage.Width;
				Options.CharHeight = (int)(ratio * Options.CharWidth);
			}
			if (Options.CharWidth < 1 || Options.CharHeight < 1) {
				Log.Error("Something went wrong with calculating the text width/height");
				return;
			}

			if (Options.OutputText)
			{
				string txt = RenderArtAsText(Options.CharWidth,Options.CharHeight, dim);
				if (txt == null) { return; }
				using(var fs = File.Open(Options.OutputName,FileMode.Create,FileAccess.ReadWrite,FileShare.Read)) {
					var buff = Encoding.UTF8.GetBytes(txt);
					fs.Write(buff,0,buff.Length);
				}
			}
			else
			{
				using(var img = RenderArtAsImage(Options.CharWidth,Options.CharHeight, dim)) {
					if (img == null) { return; }
					using(var fs = File.Open(Options.OutputName,FileMode.Create,FileAccess.ReadWrite,FileShare.Read)) {
						img.SaveAsPng(fs);
					}
				}
			}
		}

		/// <summary>
		/// Generate an image for each font character
		/// </summary>
		/// <param name="list">input list of characters</param>
		/// <param name="font">the font to use</param>
		/// <returns>maximum dimensions among the rendered chars</returns>
		static Size CreateCharSprites(char[] list,Font font)
		{
			Size dim = FindMaxDim(list,font);
			Log.Debug("dim = ["+dim.Width+"x"+dim.Height+"]");

			for(int ci=0; ci<list.Length; ci++)
			{
				char c = list[ci];
				//Log.Debug("Spriting "+c);
				var img = RenderCharSprite(c,font,dim);
				//#if DEBUG
				//using (var fs = File.OpenWrite("sprite-"+((int)c)+".png")) {
				//	img.SaveAsPng(fs);
				//}
				//#endif
				SpriteList.Add(img);
				double avg = Helpers.FindAverageGray(img);
				//Log.Debug("Spriting avg = "+avg);
			}
			return dim;
		}

		/// <summary>
		/// finds the maximum dimensions for the given set of characters
		/// </summary>
		/// <param name="list">list of characters</param>
		/// <param name="font">font for rendering</param>
		/// <returns>maximum dimensions for the given set of characer</returns>
		static Size FindMaxDim(char[] list,Font font)
		{
			var ro = new RendererOptions(font);
			float maxw = float.MinValue;
			float maxh = float.MinValue;
			foreach(char c in list) {
				SizeF size = TextMeasurer.Measure(c.ToString(),new RendererOptions(font));
				if (size.Width > maxw) { maxw = size.Width; }
				if (size.Height > maxh) { maxh = size.Height; }
			}
			return new Size(
				(int)Math.Ceiling(maxw),
				(int)Math.Ceiling(maxh)
			);
		}

		/// <summary>
		/// render the given character to an image
		/// </summary>
		/// <param name="c">character to render</param>
		/// <param name="font">font to use for rendering</param>
		/// <param name="dim">desired dimension of image</param>
		/// <returns>an image with the rendered character</returns>
		static Image<Rgba32> RenderCharSprite(Char c,Font font, Size dim)
		{
			const int workingscale = 2; //TODO make this configuratble
			int w = Math.Max(4,dim.Width * workingscale);
			int h = Math.Max(4,dim.Height * workingscale);
			int x = (w - dim.Width)/2;
			int y = (h - dim.Width)/2;
			var img = new Image<Rgba32>(Configuration.Default,w,h,Rgba32.Black);
			img.Mutate((ctx) => {
				ctx.DrawText(
					new TextGraphicsOptions(true) {
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},c.ToString(),font,Rgba32.White,new PointF(w/2.0f,h/2.0f)
				);
				ctx.Crop(new Rectangle(x,y,w,h)); //this is cropping the center.. why don't i have to use offsets ?
			});
			return img;
		}

		/// <summary>
		/// Loads necesarry resouces for rendering
		/// </summary>
		/// <returns>true loading succeeded</returns>
		static bool LoadResources()
		{
			if (!String.IsNullOrWhiteSpace(Options.SystemFont)) {
				if (!SystemFonts.TryFind(Options.SystemFont,out var family)) {
					Log.Error("font '"+Options.SystemFont+" not found");
					return false;
				}
				SelectedFont = new Font(family,Options.FontSize,Options.StyleOfFont);
				//Log.Debug("FontInfo: "
				//	+"\n\tAscender\t"+SelectedFont.Ascender
				//	+"\n\tBold\t"+SelectedFont.Bold
				//	+"\n\tDescender\t"+SelectedFont.Descender
				//	+"\n\tEmSize\t"+SelectedFont.EmSize
				//	+"\n\tFamily.Name\t"+SelectedFont.Family.Name
				//	+"\n\tItalic\t"+SelectedFont.Italic
				//	+"\n\tLineGap\t"+SelectedFont.LineGap
				//	+"\n\tLineHeight\t"+SelectedFont.LineHeight
				//	+"\n\tName\t"+SelectedFont.Name
				//	+"\n\tSize\t"+SelectedFont.Size
				//);
			}
			else if (!String.IsNullOrWhiteSpace(Options.FontFile)) {
				if (!File.Exists(Options.FontFile)) {
					Log.Error("Cannot find font file '"+Options.FontFile+"'");
					return false;
				}
				var col = new FontCollection().Install(Options.FontFile);
				SelectedFont = col.CreateFont(Options.FontSize,Options.StyleOfFont);
			}
			else {
				Log.Error("no font specified");
				return false;
			}

			//Log.Debug("Options.WhichSet = "+Options.WhichSet);
			if (Options.WhichSet != CharSets.Set.None) {
				SelectedCharSet = CharSets.Get(Options.WhichSet);
			}
			else if (!String.IsNullOrWhiteSpace(Options.CharFile)) {
				if (!File.Exists(Options.CharFile)) {
					Log.Error("cannot find file "+Options.CharFile);
					return false;
				}
				var uniqChars = new HashSet<char>();
				const int buffLen = 256;
				using(var fs = File.Open(Options.CharFile,FileMode.Open,FileAccess.Read,FileShare.Read))
				using(var sr = new StreamReader(fs)) {
					char[] buff = new char[buffLen];
					int count = int.MaxValue;
					while(count >= buffLen) {
						count = sr.Read(buff,0,buffLen);
						uniqChars.UnionWith(buff);
					}
				}
				SelectedCharSet = new char[uniqChars.Count];
				uniqChars.CopyTo(SelectedCharSet);
			}
			else {
				Log.Error("char set not specified");
				return false;
			}

			if (!String.IsNullOrWhiteSpace(Options.InputName)) {
				if (!File.Exists(Options.InputName)) {
					Log.Error("cannot find file "+Options.InputName);
					return false;
				}
				var data = File.ReadAllBytes(Options.InputName);
				SourceImage = Image.Load(data);
			}

			return true;
		}

		/// <summary>
		/// processes an image into an image made of character sprites
		/// </summary>
		/// <param name="charW">width of image in characters</param>
		/// <param name="charH">height of image in characters</param>
		/// <param name="dim">size of source image</param>
		/// <returns>output image</returns>
		static Image<Rgba32> RenderArtAsImage(int charW, int charH, Size dim)
		{
			var img = new Image<Rgba32>(charW * dim.Width,charH * dim.Height);

			RenderArtClient(charW,charH,(int x,int y,int ci) => {
				var simg = SpriteList[ci];
				img.Mutate(ctx => {
					ctx.DrawImage(simg,1.0f,new Point(x * dim.Width,y * dim.Height));
				});
			});

			//RenderArtClientAlt(charW,charH,sgmax,sgmin,(int x,int y,char c) => {
			//	var simg = SpriteList[CharSpriteMap[c]];
			//	img.Mutate(ctx => {
			//		ctx.DrawImage(simg,1.0f,new Point(x * dim.Width,y * dim.Height));
			//	});
			//});

			//RenderArtClientDiff(charW,charH,dim,(int x,int y,char c) => {
			//	var simg = CharSpriteMap[c];
			//	img.Mutate(ctx => {
			//		ctx.DrawImage(simg,1.0f,new Point(x * dim.Width,y * dim.Height));
			//	});
			//});

			return img;
		}

		/// <summary>
		/// Trasforms the source image to a text string
		/// </summary>
		/// <param name="charW">width of ouput in characters</param>
		/// <param name="charH">height of output in characters</param>
		/// <param name="dim">size of source image</param>
		/// <returns>output text</returns>
		static string RenderArtAsText(int charW, int charH, Size dim)
		{
			char[,] arr = new char[charW,charH];

			RenderArtClient(charW,charH,(int x,int y,int ci) => {
				arr[x,y] = SelectedCharSet[ci];
			});

			StringBuilder sb = new StringBuilder();
			for(int y=0; y<charH; y++) {
				for(int x=0; x<charW; x++) {
					sb.Append(arr[x,y]);
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}

		//TODO could just have this return a char[,] instead of using a callback. not sure which is better.
		/// <summary>
		/// Converts the source image to characters
		/// </summary>
		/// <param name="charW">width of output in characters</param>
		/// <param name="charH">width of output in characters</param>
		/// <param name="visitor">call back that is given the x,y coordinates along with the index of the character to render</param>
		static void RenderArtClient(int charW, int charH, Action<int,int,int> visitor)
		{
			using (var picker = new SimplePicker(SourceImage,SpriteList,charW,charH))
			{
				for(int y=0; y<charH; y++)
				{
					for(int x=0; x<charW; x++)
					{
						int index = picker.PickSprite(x,y);
						visitor(x,y,index);
					}
				}
			}
		}

		static void Dispose()
		{
			if (SourceImage != null) {
				SourceImage.Dispose();
			}
		}

		static Image<Rgba32> SourceImage = null;
		static List<Image<Rgba32>> SpriteList = new List<Image<Rgba32>>();
		static char[] SelectedCharSet = null;
		static Font SelectedFont = null;
	}
}

