using System;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Assets;
using System.IO;

namespace SuperMobs.AssetManager.Package
{
	/// <summary>
	/// 当前包资源信息
	/// 用于更新校验
	/// </summary>
	[Serializable]
	public class PackageVersion : SuperJsonObject, IStreamAsset
	{
		// the package creation time
		public long time;

		// package manifest file version
		public uint version;

		// asset version
		public string svnVer = "0";

		public override void ToStream(BinaryWriter bw)
		{
			bw.Write(time);
			bw.Write(version);
			bw.Write(svnVer);
		}

		public override void FromStream(BinaryReader br)
		{
			time = br.ReadInt64();
			version = br.ReadUInt32();
			svnVer = br.ReadString();
		}
	}
}
