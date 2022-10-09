using System;
using System.IO;

namespace SuperMobs.AssetManager.Assets
{
	public interface IStreamAsset
	{
		void ToStream(BinaryWriter bw);
		void FromStream(BinaryReader br);
	}
}
