using System.Collections.Generic;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ArtAscii
{
	public class CharMap
	{
		public CharMap(char[] list,Font font,double points)
		{
			CreateCharMap(list,font,points);
		}

		void CreateCharMap(char[] list,Font font,double points)
		{

		}

		//char to sprite map
		Dictionary<char,Image<Rgba32>> CharSpriteMap = new Dictionary<char,Image<Rgba32>>();
		//gray to char map
		//SortedList<double,char> SpriteGrayMap = new SortedList<double,char>();

	}
}