using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System.Linq;

namespace ArtAscii
{
	public class GridPicker : ISpritePicker
	{
		public GridPicker(Image<Rgba32> source, IList<Image<Rgba32>> list, int charW, int charH)
		{
			WidthInCharacters = charW;
			HeightInCharacters = charH;

			SpriteList = list;
			CreateSortedMap();
			FindSpriteGrayRange(); //must be afer CreateSortedMap

			SourceImage = source.Clone((ctx) => {
				Size newSize = new Size(charW * GridAspect,charH * GridAspect);
				ctx.Resize(new ResizeOptions {
					Mode = ResizeMode.Stretch,
					Sampler = KnownResamplers.Lanczos3,
					Size = newSize
				});
			});

			Helpers.FindGrayMinMax(SourceImage,out SourceGrayMin, out SourceGrayMax);

			SourceImage.ForEachPixel((int x,int y,Rgba32 color) => {
				byte g = (byte)ToNormalizedGray(color);
				return new Rgba32(g,g,g,color.A);
			});

			// SourceImage.Save("graypick.png");

			Log.Debug(
				"SpriteGrayMax = "+SpriteGrayMax
				+"\nSpriteGrayMin = "+SpriteGrayMin
				+"\nSourceGrayMax = "+SourceGrayMax
				+"\nSourceGrayMin = "+SourceGrayMin
			);

			int len = SpriteGrayMap.Count;
			for(int s=0; s<len; s++) {
				Log.Debug(s.ToString("0000")+" "+SpriteGrayMap[s]);
			}
		}
		public IList<Image<Rgba32>> SpriteList { get; private set; }
		public Image<Rgba32> SourceImage { get; private set; }
		public int WidthInCharacters { get; private set; }
		public int HeightInCharacters { get; private set; }

		public int PickSprite(int x, int y)
		{
			var sc = new GridInfo {
				Index = -1,
				Grid = GetImageChunk(SourceImage,x,y,GridAspect)
			};

			Log.Debug("Pick "+sc);

			//find closest sprite gray to sc
			int index = FindClosestIndex(SpriteGrayMap, sc);
			if (index < 0 || index >= SpriteGrayMap.Count) {
				throw new Exception("bad index found "+index);
			}
			int si = SpriteGrayMap[index].Index;
			return si;
		}

		public void Dispose()
		{
			if (SourceImage != null) {
				SourceImage.Dispose();
				SourceImage = null;
			}
		}

		static int FindClosestIndex(IList<GridInfo> list, GridInfo target)
		{
			//for(int i=0; i<list.Count; i++) {
			//	Log.Debug(i+" = "+list[i]);
			//}
			int len = list.Count;
			int left = 0, right = len - 1;
			if (target.CompareTo(list[0]) < 0) {
				Log.Debug("FCI super left "+list[0]+" - "+target);
				return 0;
			} else if (target.CompareTo(list[right]) > 0) {
				Log.Debug("FCI super right "+list[right]+" - "+target);
				return right;
			}

			//do a binary search
			int count = 1000; //just in case ;)
			while(left <= right && --count > 0) {
				//Log.Debug("FCI l="+left+" r="+right);
				int mid = left + (right - left) / 2;
				var num = list[mid];
				int comp = num.CompareTo(target);
				//Log.Debug("FCI "+target+" comp "+num+" = "+comp);
				if (comp == 0) {
					Log.Debug("FCI mid return "+mid);
					return mid;
				}
				if (comp < 0) {
					Log.Debug("FCI left");
					left = mid + 1;
				} else {
					Log.Debug("FCI right");
					right = mid - 1;
				}
			}

			Log.Debug("FCI return "+Math.Min(left,len - 1));
			return Math.Min(left,len - 1);
		}

		void CreateSortedMap()
		{
			for(int s=0; s<SpriteList.Count; s++)
			{
				var img = SpriteList[s];
				double avg = Helpers.FindAverageGray(img);
				SpriteGrayMap.Add(new GridInfo {
					Index = s,
					Grid = ImageToGrid(img,GridAspect)
				});
			}
			SpriteGrayMap.Sort(new GridInfo());
		}

		void FindSpriteGrayRange()
		{
			Log.Debug("FSGR count="+SpriteGrayMap.Count);
			Log.Debug("FSGR 0="+SpriteGrayMap[0]);
			Log.Debug("FSGR L="+SpriteGrayMap[SpriteGrayMap.Count - 1]);
			SpriteGrayMin = SpriteGrayMap[0].Grid.Min();
			SpriteGrayMax = SpriteGrayMap[SpriteGrayMap.Count - 1].Grid.Max();
		}

		double[] ImageToGrid(Image<Rgba32> img, int aspect)
		{
			var gridImg = img.Clone((ctx) => {
				ctx.Resize(new ResizeOptions {
					Mode = ResizeMode.Stretch,
					Sampler = KnownResamplers.Lanczos3,
					Size = new Size(aspect,aspect)
				});
			});
			using (gridImg) {
				return ExtractGridFromImage(gridImg,aspect);
			}
		}

		double[] GetImageChunk(Image<Rgba32> img, int x, int y, int aspect)
		{
			var chunk = img.Clone((ctx) => {
				ctx.Crop(new Rectangle {
					X = x * aspect,
					Y = y * aspect,
					Width = aspect,
					Height = aspect
				});
			});
			using (chunk) {
				return ExtractGridFromImage(chunk,aspect);
			}
		}

		double[] ExtractGridFromImage(Image<Rgba32> img, int aspect)
		{
			double[] grid = new double[aspect * aspect];
			for (int y=0; y<aspect; y++) {
				for(int x=0; x<aspect; x++) {
					var cell = img.GetPixelRowSpan(y)[x];
					int i = y * aspect + x;
					grid[i] = Helpers.ToGray(cell);
				}
			}
			return grid;
		}

		double ToNormalizedGray(Rgba32 color)
		{
			double g = Helpers.ToGray(color);

			double ng =
				  (SpriteGrayMax - SpriteGrayMin)
				/ (SourceGrayMax - SourceGrayMin)
				* (g - SourceGrayMin)
				+ SpriteGrayMin;

			//Log.Debug("TNG c="+color+" g="+g+" ng="+ng);
			return ng;
		}

		struct GridInfo : IComparer<GridInfo>
		{
			public int Index;
			public double[] Grid;

			public int Compare(GridInfo x, GridInfo y)
			{
				int len = x.Grid.Length;
				double sum = 0;

				for(int c=0; c<len; c++) {
					double diff = x.Grid[c] - y.Grid[c];
					sum += diff;
				}

				int final = Math.Abs(sum) <= double.Epsilon
					?  0 : sum < 0.0
					? -1 : 1;
				return final;
			}

			public int CompareTo(GridInfo info)
			{
				return this.Compare(this,info);
			}

			public override string ToString()
			{
				return "["+String.Join(',',Grid)+"]";
			}
		}

		List<GridInfo> SpriteGrayMap = new List<GridInfo>();
		double SourceGrayMax;
		double SourceGrayMin;
		double SpriteGrayMax;
		double SpriteGrayMin;
		const int GridAspect = 2;
	}
}
