using System;
using Lime;

namespace Robot.Core.Common.Extensions
{
	public static class TextureExtensions
	{
		public static bool TryGetSerializationPath(this ITexture texture, out string serializationPath)
		{
			if (texture is SerializableTexture serializableTexture) {
				serializationPath = serializableTexture.SerializationPath;
				return true;
			}
			serializationPath = null;
			return false;
		}

		public static string GetSerializationPath(this ITexture texture)
		{
			return TryGetSerializationPath(texture, out var serializationPath)
				? serializationPath
				: throw new ArgumentException("Argument is not a serializable texture", nameof(texture));
		}
	}
}
