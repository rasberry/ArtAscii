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

namespace ArtAscii
{
	class Program
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

			//using (var img = new Image<Rgba32>(200,200))
			//{
			//	var ff = new FontCollection().Install("Roboto-Regular.ttf");
			//	var font = ff.CreateFont(24);
			//	img.Mutate(ctx => {
			//		ctx.DrawText("test",font,Rgba32.Black,new PointF(0,0));
			//	});
			//	using(var fs = File.Open("image.png",FileMode.Create,FileAccess.ReadWrite,FileShare.Read)) {
			//		img.SaveAsPng(fs);
			//	}
			//}
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
			CreateCharSprites(SelectedCharSet,SelectedFont);
			using(var img = RenderArt(Options.CharWidth,Options.CharHeight)) {
				if (img == null) { return; }
				using(var fs = File.Open(Options.OutputName,FileMode.Create,FileAccess.ReadWrite,FileShare.Read)) {
					img.SaveAsPng(fs);
				}
			}
		}

		static void CreateCharSprites(char[] list,Font font)
		{
			foreach(char c in list)
			{
				var img = RenderCharSprite(c,font);
				CharSpriteMap.Add(c,img);
			}
		}

		static Image<Rgba32> RenderCharSprite(Char c,Font font)
		{
			string sChar = c.ToString();
			SizeF size = TextMeasurer.Measure(sChar,new RendererOptions(font));
			int dim = (int)Math.Ceiling(Math.Max(size.Width,size.Height));
			var img = new Image<Rgba32>(Configuration.Default,dim,dim,Rgba32.Black);
			img.Mutate((ctx) => {
				ctx.DrawText(new TextGraphicsOptions(true) {
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},sChar,font,Rgba32.White,new PointF(dim/2.0f,dim/2.0f)
				);
			});
			return img;
/*
        public static IImageProcessingContext<TPixel> ApplyScalingWaterMarkSimple<TPixel>(this IImageProcessingContext<TPixel> processingContext, Font font, string text, TPixel color, float padding)
            where TPixel : struct, IPixel<TPixel>
        {
            return processingContext.Apply(img =>
            {
                float targetWidth = img.Width - (padding * 2);
                float targetHeight = img.Height - (padding * 2);

                // measure the text size
                SizeF size = TextMeasurer.Measure(text, new RendererOptions(font));

                //find out how much we need to scale the text to fill the space (up or down)
                float scalingFactor = Math.Min(img.Width / size.Width, img.Height / size.Height);

                //create a new font 
                Font scaledFont = new Font(font, scalingFactor * font.Size);

                var center = new PointF(img.Width / 2, img.Height / 2);
                var textGraphicOptions = new TextGraphicsOptions(true) {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                img.Mutate(i => i.DrawText(textGraphicOptions, text, scaledFont, color, center));
            });
		}
*/
		}

		static bool LoadResources()
		{
			if (!String.IsNullOrWhiteSpace(Options.SystemFont)) {
				if (!SystemFonts.TryFind(Options.SystemFont,out var family)) {
					Log.Error("font '"+Options.SystemFont+" not found");
					return false;
				}
				SelectedFont = new Font(family,FontSize,Options.StyleOfFont);
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

		static Image<Rgba32> RenderArt(int charW, int charH)
		{
			int maxSpriteW = int.MinValue;
			int maxSpriteH = int.MinValue;
			foreach(var kvp in CharSpriteMap) {
				var size = kvp.Value.Size();
				if (size.Width > maxSpriteW) {
					maxSpriteW = size.Width;
				}
				if (size.Height > maxSpriteH) {
					maxSpriteH = size.Height;
				}
			}
			if (maxSpriteH < 1 || maxSpriteW < 1) {
				Log.Error("character sprite sizes are broken ["+maxSpriteW+","+maxSpriteH+"]");
				return null;
			}

			var img = new Image<Rgba32>(charW * maxSpriteW,charH * maxSpriteH);

			//TODO resize is the naive implementation - maybe also do a more advanced version
			SourceImage.Mutate((ctx) => {
				ctx.Resize(new ResizeOptions {
					Mode = ResizeMode.Stretch,
					Sampler = KnownResamplers.Lanczos3,
					Size = new Size(charW,charH)
				});
			});

			//TODO convert to grayscale - wait.. we need charmap number of levels..
			//	might have to do a manual calculation unless i can use float based image
			//TODO read each pixel value and assign corresponding character
			
			//humm.. actually i was thinking about grabbing each box of pixels, matching the size of the sprite
			// then finding the one that matches the closest (using diff or something)
			//

		}

		static void Dispose()
		{
			if (CharSpriteMap != null) {
				foreach(var kvp in CharSpriteMap) {
					kvp.Value?.Dispose();
				}
			}
		}

		static Dictionary<char,Image<Rgba32>> CharSpriteMap = new Dictionary<char,Image<Rgba32>>();
		static Image<Rgba32> SourceImage = null;
		static char[] SelectedCharSet = null;
		static Font SelectedFont = null;
		const float FontSize = 12.0f;
	}
}

