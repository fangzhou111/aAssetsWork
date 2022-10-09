using System;
using System.IO;
using SuperMobs.AssetManager.Assets;
namespace SuperMobs.AssetManager.Package
{
	/// <summary>
	/// 包里面每个文件对应的crc + length信息
	/// 提供给更新器校验用的
	/// 
	/// NOTE: 服务器文件命名 : namecrc + "_" + filecrc + ".ab"
	/// </summary>
	public class PackageAsset : IStreamAsset
	{
		public uint nameCrc;
		public uint fileCrc;
		public int fileLength;

		public void FromStream(BinaryReader br)
		{
			nameCrc = br.ReadUInt32();
			fileCrc = br.ReadUInt32();
			fileLength = br.ReadInt32();
		}

		public void ToStream(BinaryWriter bw)
		{
			bw.Write(nameCrc);
			bw.Write(fileCrc);
			bw.Write(fileLength);
		}
	}
}
