/*
 * @Author: chiuan wei 
 * @Date: 2017-11-21 02:05:51 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-11-21 02:09:31
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SuperMobs.AssetManager.Core;
using UnityEngine;

namespace SuperMobs.AssetManager.Assets
{
	/// <summary>
	/// manifest信息文件，这个文件是个二进制，储存一系列的Bundles的信息
	/// NOTE:其他单个对象信息通过collector获取
	/// 二进制排序:
	/// > bundles crcs collector **
	/// > bundles collector      **
	/// > bundles items
	/// > assets collector       **
	/// > assets items
	/// 
	/// </summary>
	public class AssetManifest : StreamAsset
	{
		// 这个资源占用的stream长度
		public const int MANIFEST_LENGTH = 4 * 5;
		public int bundleCrcCollectorIndex;
		public int bundleCollectorIndex;
		public int bundleIndex;
		public int assetCollectorIndex;
		public int assetIndex;

		// this manifest files bytes include 4 long index first.
		private byte[] fileBytes = new byte[0];

		private AssetCollector<Asset> _assetCollector;
		private AssetCollector<Bundle> _bundleCollector;
		private BundleCrcCollector _bundleCrcCollector;

		public void InitContent(byte[] fileBytes)
		{
			this.fileBytes = fileBytes;
		}

		public string ToIndexString()
		{
			return bundleCrcCollectorIndex + " | " + bundleCollectorIndex +
				" | " + bundleIndex + " | " + assetCollectorIndex + " | " + assetIndex +
				"\nfile length = " + fileBytes.Length;
		}

		public override void FromStream(BinaryReader br)
		{
			bundleCrcCollectorIndex = br.ReadInt32();
			bundleCollectorIndex = br.ReadInt32();
			bundleIndex = br.ReadInt32();
			assetCollectorIndex = br.ReadInt32();
			assetIndex = br.ReadInt32();
		}

		public override void ToStream(BinaryWriter bw)
		{
			bw.Write(bundleCrcCollectorIndex);
			bw.Write(bundleCollectorIndex);
			bw.Write(bundleIndex);
			bw.Write(assetCollectorIndex);
			bw.Write(assetIndex);
		}

		private T LoadStreamAsset<T>(int pos, int len) where T : IStreamAsset
		{
			var asset = Activator.CreateInstance<T>();
			try
			{
				using(MemoryStream ms = new MemoryStream(fileBytes, pos, len, false))
				{
					using(BinaryReader br = new BinaryReader(ms))
					{
						asset.FromStream(br);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError("LoadStreamAsset at " + pos + "," + len + " excpetion = " + e.Message);
				return default(T);
			}

			return asset;
		}

		public AssetCollector<Asset> GetAssetCollector()
		{
			if (_assetCollector == null)
			{
				int pos = assetCollectorIndex;
				int len = assetIndex - assetCollectorIndex;
				_assetCollector = LoadStreamAsset<AssetCollector<Asset>>(pos, len);
			}

			return _assetCollector;
		}

		public AssetCollector<Bundle> GetBundleCollector()
		{
			if (_bundleCollector == null)
			{
				int pos = bundleCollectorIndex;
				int len = bundleIndex - bundleCollectorIndex;
				_bundleCollector = LoadStreamAsset<AssetCollector<Bundle>>(pos, len);
			}

			return _bundleCollector;
		}

		public BundleCrcCollector GetBundleCrcCollector()
		{
			if (_bundleCrcCollector == null)
			{
				int pos = bundleCrcCollectorIndex;
				int len = bundleCollectorIndex - bundleCrcCollectorIndex;
				_bundleCrcCollector = LoadStreamAsset<BundleCrcCollector>(pos, len);
			}

			return _bundleCrcCollector;
		}

		/// <summary>
		/// 获取当前版本所有的 bundles
		/// </summary>
		public ICollection<Bundle> GetBundles()
		{
			var collector = GetBundleCollector();
			var names = collector.names;
			foreach (var item in names)
			{
				FindAssetInternal<Bundle>(bundleIndex, item, ref collector);
			}
			return collector.GetCurrentAssetCollection();
		}

		/// <summary>
		/// 获取当前版本所有的 assets
		/// </summary>
		public ICollection<Asset> GetAssets()
		{
			var collector = GetAssetCollector();
			var names = collector.names;
			foreach (var item in names)
			{
				FindAssetInternal<Asset>(assetIndex, item, ref collector);
			}
			return collector.GetCurrentAssetCollection();
		}

		private T FindAssetInternal<T>(int offset, uint name, ref AssetCollector<T> collector) where T : IStreamAsset
		{
			if (collector == null) return default(T);

			var asset = collector.GetAsset(name);
			if (asset != null) return asset;

			int len;
			int pos = collector.GetAssetIndex(name, out len);
			if (pos == -1 || len <= 0) return default(T);

			asset = LoadStreamAsset<T>(offset + pos, len);
			if (asset != null) collector.AddAsset(name, ref asset);

			return asset;
		}

		public Asset FindAsset(string sourcePath)
		{
			return FindAsset(Crc32.GetStringCRC32(sourcePath));
		}

		public Asset FindAsset(uint sourcePathCrc)
		{
			var collector = GetAssetCollector();
			return FindAssetInternal<Asset>(assetIndex, sourcePathCrc, ref collector);
		}

		public Bundle FindBundle(string bundleName)
		{
			return FindBundle(Crc32.GetStringCRC32(bundleName));
		}

		public Bundle FindBundle(uint bundleNameCrc)
		{
			var collector = GetBundleCollector();
			return FindAssetInternal<Bundle>(bundleIndex, bundleNameCrc, ref collector);
		}

		// todo 先废弃index的判断
		/// <summary>
		/// 传某个asset，以及这个asset当前获取buildPath的某个index参数来决定用哪个打包的资源
		/// 例如: lua = 0,lua64 = 1
		/// </summary>
		public Bundle FindBundle(ref Asset asset, out string buildPath)
		{
			return FindBundle(ref asset, 0, out buildPath);
		}

		public Bundle FindBundle(ref Asset asset, int index, out string buildPath)
		{
			buildPath = string.Empty;
			if (asset == null)
			{
				return null;
			}

			if (asset.buildPaths.Length <= index)
			{
				AssetLogger.LogError("FindBundle with out of array index " + index + " for " + asset.buildPaths.Single());
				return null;
			}
			buildPath = asset.buildPaths[index];
			return FindBundle(asset.buildBundleNames[index]);
		}

		/// <summary>
		/// 获取这个Bundle以及它所有依赖Bundle的列表
		/// bundle本身在末尾
		/// 仅仅是dependence的bundle
		/// </summary>
		public List<Bundle> FindDependenceBundles(Bundle bundle)
		{
			List<Bundle> ret = new List<Bundle>();

			foreach (var bn in bundle.dependencies)
			{
				ret.Add(FindBundle(bn));
			}

			// 最后添加自己(主资源)
			ret.Add(bundle);

			return ret;
		}

		/// <summary>
		/// 遍历跟这个资源相关的所有bundle
		/// </summary>
		public void EachBundleForAsset(Asset asset, Bundle bundle, Action<Bundle> call)
		{
			var bundles = FindDependenceBundles(bundle);
			foreach (var b in bundles)
			{
				call(b);
			}

			foreach (var link in asset.linkSingleAssets)
			{
				var linkAsset = FindAsset(link);
				if (linkAsset != null)
				{
					string _;
					var linkBundle = FindBundle(ref linkAsset, out _);
					EachBundleForAsset(linkAsset, linkBundle, call);
				}
			}
		}

	}
}