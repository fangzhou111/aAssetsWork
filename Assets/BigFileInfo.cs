
namespace SuperMobs.AssetManager.Assets
{
	using System;
	using System.IO;
	using SuperMobs.AssetManager.Core;

	[Serializable]
	public class BigFileInfo : IStreamAsset
	{
		// convert from bundleName to hash id
		public uint id;

		// begin index from bigfile
		public ulong beginIndex;

		// thoes bundle length to read from bigfile
		public int length;

		public void ToStream(BinaryWriter bw)
		{
			bw.Write(id);
			bw.Write(beginIndex);
			bw.Write(length);
		}

		public void FromStream(BinaryReader br)
		{
			id = br.ReadUInt32();
			beginIndex = br.ReadUInt64();
			length = br.ReadInt32();
		}

		public override string ToString()
		{
			return "id = " + id + "  beginIndex = " + beginIndex + "  length = " + length;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

	}
}
