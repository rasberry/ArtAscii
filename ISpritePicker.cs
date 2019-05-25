using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ArtAscii
{
	public interface ISpritePicker : IDisposable
	{
		IList<Image<Rgba32>> SpriteList { get; }
		Image<Rgba32> SourceImage { get; }
		int PickSprite(int x, int y);
	}
}