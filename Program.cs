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
		= add option to output to text (probably should be default ?)
			= this would remove the need to require a font (maybe..)
		= add option to reduce space embetween characters (basically crop more)
		= add option to select font size
		= maybe add way to not use single dim parameter but treat x and y components independently
			= aka use rectagles instead of squares
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

			double smax, smin;
			int dim = CreateCharSprites(SelectedCharSet,SelectedFont, out smax,out smin);

			//If the character render width/height are not set use the image dimensions
			if (Options.CharWidth < 1 || Options.CharHeight < 1) {
				Options.CharWidth = SourceImage.Width / dim;
				Options.CharHeight = SourceImage.Height / dim;
			}

			using(var img = RenderArt(Options.CharWidth,Options.CharHeight, dim, smax, smin)) {
				if (img == null) { return; }
				using(var fs = File.Open(Options.OutputName,FileMode.Create,FileAccess.ReadWrite,FileShare.Read)) {
					img.SaveAsPng(fs);
				}
			}
		}

		static int CreateCharSprites(char[] list,Font font, out double max, out double min)
		{
			max = double.MinValue;
			min = double.MaxValue;
			int dim = FindMaxDim(list,font);
			//Log.Debug("dim = "+dim);

			foreach(char c in list)
			{
				//Log.Debug("Spriting "+c);
				var img = RenderCharSprite(c,font,dim);
				using (var fs = File.OpenWrite("sprite-"+((int)c)+".png")) {
					img.SaveAsPng(fs);
				}
				CharSpriteMap.Add(c,img);
				double avg = FindAverageGray(img);
				//Log.Debug("Spriting avg = "+avg);
				SpriteGrayMap.TryAdd(avg,img);
				if (avg > max) { max = avg; }
				if (avg < min) { min = avg; }
			}
			return dim;
		}

		static int FindMaxDim(char[] list,Font font)
		{
			var ro = new RendererOptions(font);
			float max = float.MinValue;
			foreach(char c in list) {
				SizeF size = TextMeasurer.Measure(c.ToString(),new RendererOptions(font));
				if (size.Width > max) { max = size.Width; }
				if (size.Height > max) { max = size.Height; }
			}
			return (int)Math.Ceiling(max);
		}

		static Image<Rgba32> RenderCharSprite(Char c,Font font, int dim)
		{
			int workdim = 2 * dim;
			var img = new Image<Rgba32>(Configuration.Default,workdim,workdim,Rgba32.Black);
			img.Mutate((ctx) => {
				ctx.DrawText(new TextGraphicsOptions(true) {
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},c.ToString(),font,Rgba32.White,new PointF(dim/2.0f,dim/2.0f)
				);
				ctx.Crop(new Rectangle(0,0,dim,dim)); //this is cropping the center.. now sure how
			});
			return img;
		}

		static bool LoadResources()
		{
			if (!String.IsNullOrWhiteSpace(Options.SystemFont)) {
				if (!SystemFonts.TryFind(Options.SystemFont,out var family)) {
					Log.Error("font '"+Options.SystemFont+" not found");
					return false;
				}
				SelectedFont = new Font(family,FontSize,Options.StyleOfFont);
				Log.Debug("FontInfo: "
					+"\n\tAscender\t"+SelectedFont.Ascender
					+"\n\tBold\t"+SelectedFont.Bold
					+"\n\tDescender\t"+SelectedFont.Descender
					+"\n\tEmSize\t"+SelectedFont.EmSize
					+"\n\tFamily.Name\t"+SelectedFont.Family.Name
					+"\n\tItalic\t"+SelectedFont.Italic
					+"\n\tLineGap\t"+SelectedFont.LineGap
					+"\n\tLineHeight\t"+SelectedFont.LineHeight
					+"\n\tName\t"+SelectedFont.Name
					+"\n\tSize\t"+SelectedFont.Size
				);
			}
			else if (!String.IsNullOrWhiteSpace(Options.FontFile)) {
				if (!File.Exists(Options.FontFile)) {
					Log.Error("Cannot find font file '"+Options.FontFile+"'");
					return false;
				}
				var col = new FontCollection().Install(Options.FontFile);
				SelectedFont = col.CreateFont(FontSize,Options.StyleOfFont);
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

		static Image<Rgba32> RenderArt(int charW, int charH, int dim, double sgmax, double sgmin)
		{
			var img = new Image<Rgba32>(charW * dim,charH * dim);

			//TODO resize is the naive implementation - maybe also do a more advanced version
			//humm.. actually i was thinking about grabbing each box of pixels, matching the size of the sprite
			// then finding the one that matches the closest (using diff or something)
			SourceImage.Mutate((ctx) => {
				ctx.Resize(new ResizeOptions {
					Mode = ResizeMode.Stretch,
					Sampler = KnownResamplers.Lanczos3,
					Size = new Size(charW,charH)
				});
			});

			FindGrayMinMax(SourceImage,out double gmin, out double gmax);

			for(int y=0; y<charH; y++)
			{
				for(int x=0; x<charH; x++)
				{
					var sc = SourceImage.GetPixelRowSpan(y)[x];
					var simg = MapGrayToSprite(ToGray(sc),gmax,gmin,sgmax,sgmin);
					img.Mutate(ctx => {
						ctx.DrawImage(simg,1.0f,new Point(x * dim,y * dim));
					});
				}
			}

			return img;
		}

		static Image<Rgba32> MapGrayToSprite(double g, double gmax,double gmin, double sgmax, double sgmin)
		{
			//g is the source gray index
			//gmin and gmax are source range of gray
			//sgmax, sgmin are the sprite range of gray
			double sg = (sgmax - sgmin) / (gmax - gmin) * (g - gmin) + sgmin;
			//Log.Debug(sg+" = ("+sgmax+" - "+sgmin+") / ("+gmax+" - "+gmin+") * "+g);

			//find closest sprite gray to sg
			int index = FindClosestIndex(SpriteGrayMap.Keys, sg);
			//Log.Debug("index = "+index+" "+SpriteGrayMap.Count);
			return SpriteGrayMap[SpriteGrayMap.Keys[index]];
		}

		static int FindClosestIndex(IList<double> list, double target)
		{
			//for(int i=0; i<list.Count; i++) {
			//	Log.Debug(i+" = "+list[i]);
			//}
			int len = list.Count;
			//Log.Debug("FCI len = "+len);
			int left = 0, right = len - 1;
			if (target.CompareTo(list[0]) < 0) {
				//Log.Debug("FCI super left "+list[0]+" - "+target);
				return 0;
			} else if (target.CompareTo(list[right]) > 0) {
				//Log.Debug("FCI super right "+list[right]+" - "+target);
				return right;
			}

			//do a binary search
			int count = 1000;
			while(left <= right && --count > 0) {
				//Log.Debug("FCI l="+left+" r="+right);
				int mid = left + (right - left) / 2;
				double num = list[mid];
				int comp = num.CompareTo(target);
				//Log.Debug("FCI "+target+" comp "+num+" = "+comp);
				if (comp == 0) {
					//Log.Debug("FCI mid");
					return mid;
				}
				if (comp < 0) {
					//Log.Debug("FCI left");
					left = mid + 1;
				} else {
					//Log.Debug("FCI right");
					right = mid - 1;
				}
			}

			//round to the nearest whole index
			if (left >= len - 1) { left = len - 2; }
			double vmid = list[left+1] - list[left];
			int final = target > vmid ? left + 1 : left;
			//Log.Debug("final = "+final);
			return final;

		}

		static void FindGrayMinMax(Image<Rgba32> img,out double min,out double max)
		{
			max = double.MinValue;
			min = double.MaxValue;
			int w = img.Width;
			int h = img.Height;
			for(int y=0; y<h; y++) {
				var row = img.GetPixelRowSpan(y);
				for(int x=0; x<w; x++) {
					var c = row[x];
					double g = ToGray(c);
					if (g > max) { max = g; }
					if (g < min) { min = g; }
				}
			}
		}
		static double ToGray(Rgba32 color)
		{
			//TODO maybe incorporate alpha ?
			return color.R * 0.2126 + color.G * 0.7152 + color.B * 0.0722;
		}

		static double FindAverageGray(Image<Rgba32> img)
		{
			var avgImg = img.Clone((ctx) => {
				ctx.Resize(new ResizeOptions {
					Mode = ResizeMode.Stretch,
					Sampler = KnownResamplers.Lanczos3,
					Size = new Size(1,1)
				});
			});
			var span = avgImg.GetPixelSpan();
			return ToGray(span[0]);
		}
		static void Dispose()
		{
			if (CharSpriteMap != null) {
				foreach(var kvp in CharSpriteMap) {
					kvp.Value?.Dispose();
				}
			}
			if (SourceImage != null) {
				SourceImage.Dispose();
			}
		}

		static Dictionary<char,Image<Rgba32>> CharSpriteMap = new Dictionary<char,Image<Rgba32>>();
		static SortedList<double,Image<Rgba32>> SpriteGrayMap = new SortedList<double,Image<Rgba32>>();
		static Image<Rgba32> SourceImage = null;
		static char[] SelectedCharSet = null;
		static Font SelectedFont = null;
		const float FontSize = 12.0f;
	}
}

