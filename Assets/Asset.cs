using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.IO;
using SuperMobs.AssetManager.Core;

namespace SuperMobs.AssetManager.Assets
{
	/// <summary>
	/// 这是每个打包资源sourcePath对应的一个结构
	/// 用来代表是那个资源，可以require它的资源类型
	/// </summary>
	public class Asset : IStreamAsset
	{
		// source资源路径取得crc
		public uint sourcePathCrc;

		// 该资源怎么打包
		public AssetBuildType buildType;

		// 这个原始资源可以变成打包的资源路径(真正打包的资源路径)
		// NOTE:当前仅支持用1个。简单点好!
		public string[] buildPaths;

		// 这个 buildPaths 对应的打包bundle名称
		// NOTE:当前只支持1个。考虑优化掉，实际很少2个情况。
		public string[] buildBundleNames;

		// 关联的独立的资源
		// 不是默认依赖的
		public string[] linkSingleAssets;

        public Asset()
        {
            linkSingleAssets = new string[0];
        }

        public void ToStream(BinaryWriter bw)
		{
			bw.Write(sourcePathCrc);
			bw.Write((char)buildType);
			bw.WriteArray(buildPaths);
			bw.WriteArray(buildBundleNames);
			bw.WriteArray(linkSingleAssets);
		}

		public void FromStream(BinaryReader br)
		{
			sourcePathCrc = br.ReadUInt32();
			buildType = (AssetBuildType)br.ReadChar();
			buildPaths = br.ReadArrayString();
			buildBundleNames = br.ReadArrayString();
			linkSingleAssets = br.ReadArrayString();
		}
	}
}
