using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace ArtAscii
{
	public interface ISpritePicker
	{
		IList<Image<Rgba32>> SpriteList { get; }
		Image<Rgba32> SourceImage { get; }
		int PickSprite(int x, int y);
	}

	public class SimplePicker : ISpritePicker
	{
		public SimplePicker(Image<Rgba32> source, IList<Image<Rgba32>> list, int charW, int charH)
		{
			WidthInCharacters = charW;
			HeightInCharacters = charH;

			SourceImage = source.Clone((ctx) => {
				ctx.Resize(new ResizeOptions {
					Mode = ResizeMode.Stretch,
					Sampler = KnownResamplers.Lanczos3,
					Size = new Size(charW,charH)
				});
			});

			SpriteList = list;
			Helpers.FindGrayMinMax(SourceImage,out SourceGrayMin, out SourceGrayMax);
			CreateSortedMap();
			FindSpriteGrayRange(); //must be afer CreateSortedMap
		}
		public IList<Image<Rgba32>> SpriteList { get; private set; }
		public Image<Rgba32> SourceImage { get; private set; }
		public int WidthInCharacters { get; private set; }
		public int HeightInCharacters { get; private set; }

		public int PickSprite(int x, int y)
		{
			var sc = SourceImage.GetPixelRowSpan(y)[x];
			double g = Helpers.ToGray(sc);

			double sg =
				  (SpriteGrayMax - SpriteGrayMin)
				/ (SourceGrayMax - SourceGrayMin)
				* (g - SourceGrayMin)
				+ SpriteGrayMin
			;
			//Log.Debug("Pick x="+x+" y="+y+" sg="+sg+" ["
			//	+"("+SpriteGrayMax+" - "+SpriteGrayMin+")"
			//	+" / ("+SourceGrayMax+" - "+SourceGrayMin+")"
			//	+" * ("+g+" - "+SourceGrayMin+") + "+SpriteGrayMin
			//	+"]");
			//Log.Debug(sg+" = ("+SpriteGrayMax+" - "+SpriteGrayMin+") / ("+SourceGrayMax+" - "+SourceGrayMin+") * "+g);

			//find closest sprite gray to sg
			int index = FindClosestIndex(SpriteGrayMap, sg);
			Log.Debug("index = "+index+" "+SpriteGrayMap.Count);
			return SpriteGrayMap[index].Index;
		}

		static int FindClosestIndex(IList<SpriteInfo> list, double target)
		{
			//for(int i=0; i<list.Count; i++) {
			//	Log.Debug(i+" = "+list[i]);
			//}
			int len = list.Count;
			//Log.Debug("FCI len = "+len);
			int left = 0, right = len - 1;
			if (target.CompareTo(list[0].Gray) < 0) {
				//Log.Debug("FCI super left "+list[0]+" - "+target);
				return 0;
			} else if (target.CompareTo(list[right].Gray) > 0) {
				//Log.Debug("FCI super right "+list[right]+" - "+target);
				return right;
			}

			//do a binary search
			int count = 1000; //just in case ;)
			while(left <= right && --count > 0) {
				//Log.Debug("FCI l="+left+" r="+right);
				int mid = left + (right - left) / 2;
				double num = list[mid].Gray;
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
			double vmid = list[left+1].Gray - list[left].Gray;
			int final = target > vmid ? left + 1 : left;
			//Log.Debug("final = "+final);
			return final;
		}

		void CreateSortedMap()
		{
			for(int s=0; s<SpriteList.Count; s++)
			{
				var img = SpriteList[s];
				double avg = Helpers.FindAverageGray(img);
				SpriteGrayMap.Add(new SpriteInfo { Index = s, Gray = avg});
				//SpriteGrayMap.TryAdd(avg,s);
			}
			SpriteGrayMap.Sort(new SpriteInfo());
			foreach(var s in SpriteGrayMap) {
				Log.Debug("s: "+s.Gray+"\t"+s.Index);
			}
		}

		void FindSpriteGrayRange()
		{
			SpriteGrayMin = SpriteGrayMap[0].Gray;
			SpriteGrayMax = SpriteGrayMap[SpriteGrayMap.Count - 1].Gray;
		}

		struct SpriteInfo : IComparer<SpriteInfo>
		{
			public int Index;
			public double Gray;

			public int Compare(SpriteInfo x, SpriteInfo y)
			{
				return Comparer<double>.Default.Compare(x.Gray,y.Gray);
			}
		}

		List<SpriteInfo> SpriteGrayMap = new List<SpriteInfo>();
		double SourceGrayMax;
		double SourceGrayMin;
		double SpriteGrayMax;
		double SpriteGrayMin;
	}
}
