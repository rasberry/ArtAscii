using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace ArtAscii
{
	public static class Helpers
	{
		/// <summary>
		/// determine the min and max grays for an image
		/// </summary>
		/// <param name="img">input image</param>
		/// <param name="min">output min gray</param>
		/// <param name="max">output max gray</param>
		public static void FindGrayMinMax(Image<Rgba32> img,out double min,out double max)
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
		
		public static double ToGray(Rgba32 color)
		{
			//TODO maybe incorporate alpha ?
			double gray = color.R * 0.2126 + color.G * 0.7152 + color.B * 0.0722;
			return gray * color.A / 255.0;
		}

		public static double FindAverageGray(Image<Rgba32> img)
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
	}
}
