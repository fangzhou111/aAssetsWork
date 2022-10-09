using System;
using System.IO;
using SuperMobs.AssetManager.Core;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace SuperMobs.AssetManager.Assets
{
	/// <summary>
	/// 记录资源的储存信息及在文件里面的位置
	/// **储存管理加载后的对象(StreamAsset)
	/// </summary>
	public class AssetCollector<T> : StreamAsset where T : IStreamAsset
	{
		public uint[] names;
		public int[] position;

		// total stream length
		public int streamLength;

		// 当前数量
		private int count = 0;

		// 缓存名称的index
		Dictionary<uint, int> _nameDict = new Dictionary<uint, int>();

		// 缓存加载的对象,这个管理器负责管理的资源
		Dictionary<uint, T> _assetDict = new Dictionary<uint, T>();

		public AssetCollector()
		{
			names = new uint[0];
			position = new int[0];
		}

		public int GetAssetIndex(uint name, out int len)
		{
			int index = 0;
			if (_nameDict.TryGetValue(name, out index))
			{
				int pos = position[index];
				len = index + 1 == count ? streamLength - position[index] : position[index + 1] - pos;
				return pos;
			}
			else
			{
				len = 0;
				return -1;
			}
		}

		/// <summary>
		/// 获取当前加载的T对象数组
		/// NOTE:这个不建议运行时调用
		/// </summary>
		public T[] GetCurrentAssets()
		{
			return _assetDict.Values.ToArray();
		}

		/// <summary>
		/// 获取当前加载出来的asset
		/// </summary>
		public ICollection<T> GetCurrentAssetCollection()
		{
			return _assetDict.Values;
		}

		// 这个接口获取到东西需要先AddAsset添加过
		public T GetAsset(uint name)
		{
			T asset;
			if (_assetDict.TryGetValue(name, out asset))
			{
				return asset;
			}
			else
			{
				return default(T);
			}
		}

		public void AddAsset(uint name, ref T asset)
		{
			T _old;
			if (_assetDict.TryGetValue(name, out _old))
			{
				AssetLogger.LogError("AddAsset with exist before > " + name + " " + asset.GetType().ToString());
			}
			_assetDict[name] = asset;
		}

		public override void ToStream(BinaryWriter bw)
		{
			bw.WriteArray(names);
			bw.WriteArray(position);
			bw.Write(streamLength);
		}

		public override void FromStream(BinaryReader br)
		{
			names = br.ReadArrayUint();
			position = br.ReadArrayInt();
			streamLength = br.ReadInt32();

			count = names.Length;
			for (int i = 0; i < count; i++)
			{
				_nameDict[names[i]] = i;
			}

		}



	}
}
