﻿using System;
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
		= add option to reduce space embetween characters (basically crop more)
		= add option to select font size
		= maybe allow a set of input images instead of using a font
			= this would be another render mode - for collage art
			= would need to put back color matching instead of always using ToGray
			= could also allow a single 'source' image that gets cut up and used as the sprites
			= hummm.. so if you used a single image as both input and output
			 - it would just be puzzling the pieces back together as they were ?
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
			Size dim = CreateCharSprites(SelectedCharSet,SelectedFont, out smax,out smin);

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
				string txt = RenderArtAsText(Options.CharWidth,Options.CharHeight, dim, smax, smin);
				if (txt == null) { return; }
				using(var fs = File.Open(Options.OutputName,FileMode.Create,FileAccess.ReadWrite,FileShare.Read)) {
					var buff = Encoding.UTF8.GetBytes(txt);
					fs.Write(buff,0,buff.Length);
				}
			}
			else
			{
				using(var img = RenderArtAsImage(Options.CharWidth,Options.CharHeight, dim, smax, smin)) {
					if (img == null) { return; }
					using(var fs = File.Open(Options.OutputName,FileMode.Create,FileAccess.ReadWrite,FileShare.Read)) {
						img.SaveAsPng(fs);
					}
				}
			}
		}

		static Size CreateCharSprites(char[] list,Font font, out double max, out double min)
		{
			max = double.MinValue;
			min = double.MaxValue;
			Size dim = FindMaxDim(list,font);
			Log.Debug("dim = ["+dim.Width+"x"+dim.Height+"]");

			foreach(char c in list)
			{
				//Log.Debug("Spriting "+c);
				var img = RenderCharSprite(c,font,dim);
				//#if DEBUG
				//using (var fs = File.OpenWrite("sprite-"+((int)c)+".png")) {
				//	img.SaveAsPng(fs);
				//}
				//#endif
				CharSpriteMap.Add(c,img);
				double avg = FindAverageGray(img);
				//Log.Debug("Spriting avg = "+avg);
				SpriteGrayMap.TryAdd(avg,c);
				if (avg > max) { max = avg; }
				if (avg < min) { min = avg; }
			}
			return dim;
		}

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

		static Image<Rgba32> RenderCharSprite(Char c,Font font, Size dim)
		{
			const int workingscale = 2;
			int w = dim.Width * workingscale;
			int h = dim.Height * workingscale;
			int x = w * (workingscale - 1)/4;
			int y = h * (workingscale - 1)/4;
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

		static bool LoadResources()
		{
			if (!String.IsNullOrWhiteSpace(Options.SystemFont)) {
				if (!SystemFonts.TryFind(Options.SystemFont,out var family)) {
					Log.Error("font '"+Options.SystemFont+" not found");
					return false;
				}
				SelectedFont = new Font(family,FontSize,Options.StyleOfFont);
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

		static Image<Rgba32> RenderArtAsImage(int charW, int charH, Size dim, double sgmax, double sgmin)
		{
			var img = new Image<Rgba32>(charW * dim.Width,charH * dim.Height);

			RenderArtClient(charW,charH,sgmax,sgmin,(int x,int y,char c) => {
				var simg = CharSpriteMap[c];
				img.Mutate(ctx => {
					ctx.DrawImage(simg,1.0f,new Point(x * dim.Width,y * dim.Height));
				});
			});

			return img;
		}

		static string RenderArtAsText(int charW, int charH, Size dim, double sgmax, double sgmin)
		{
			char[,] arr = new char[charW,charH];

			RenderArtClient(charW,charH,sgmax,sgmin,(int x,int y,char c) => {
				arr[x,y] = c;
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
		static void RenderArtClient(int charW, int charH, double sgmax, double sgmin, Action<int,int,char> visitor)
		{
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
				for(int x=0; x<charW; x++)
				{
					var sc = SourceImage.GetPixelRowSpan(y)[x];
					var c = MapGrayToChar(ToGray(sc),gmax,gmin,sgmax,sgmin);
					visitor(x,y,c);
				}
			}
		}

		static char MapGrayToChar(double g, double gmax,double gmin, double sgmax, double sgmin)
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
			int count = 1000; //just in case ;)
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

		//char to sprite map
		static Dictionary<char,Image<Rgba32>> CharSpriteMap = new Dictionary<char,Image<Rgba32>>();
		//gray to char map
		static SortedList<double,char> SpriteGrayMap = new SortedList<double,char>();
		static Image<Rgba32> SourceImage = null;
		static char[] SelectedCharSet = null;
		static Font SelectedFont = null;
		const float FontSize = 12.0f;
	}
}

