using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ArtAscii
{
	public interface ICharSprite
	{
		Image<Rgba32> Sprite { get; }
	}

	public class CharSprite
	{
	}
}
