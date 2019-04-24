using Lime;

namespace Robot.Core.Common.Extensions
{
	public static class ImageExtensions
	{
		public static void FitToImageSize(this Image image, string texturePath)
		{
			var texture = new SerializableTexture(texturePath);

			image.Texture = texture;
			image.Width = texture.ImageSize.Width;
			image.Height = texture.ImageSize.Height;
		}
	}
}
