using System;
using System.IO;
using SuperMobs.AssetManager.Core;

namespace SuperMobs.AssetManager.Assets
{
	public abstract class StreamAsset : IStreamAsset
	{
		public abstract void FromStream(BinaryReader br);
		public abstract void ToStream(BinaryWriter bw);

		public byte[] ToBytes()
		{
			using (var ms = new MemoryStream())
			{
				using (BinaryWriter bw = new BinaryWriter(ms))
				{
					ToStream(bw);
					bw.BaseStream.Position = 0;
					return bw.GetBytes();
				}
			}
		}

	}
}
