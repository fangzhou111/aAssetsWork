/*
 * @Author: chiuan wei 
 * @Date: 2017-11-21 00:56:20 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-11-21 01:53:34
 */
using System;
using System.Collections.Generic;
using SuperMobs.AssetManager.Assets;
using SuperMobs.Core;
using UnityEngine;

namespace SuperMobs.AssetManager.Loader
{
	/// <summary>
	/// assetbundle 同步加载器
	/// </summary>
	internal class AssetBundleSyncLoader : SingletonLoader<AssetBundleSyncLoader, AssetBundle>
	{
		ILoader[] loaders;
		AssetBundle ab;

		public AssetBundleSyncLoader()
		{
			loaders = new ILoader[]
			{
				DownloadAssetBundleSync.Instance,
					#if UNITY_ANDROID && !UNITY_EDITOR
					AndroidAssetBundleSync.Instance,
					#else
					AppAssetBundleSync.Instance,
					#endif
			};
		}

		public override object Load(string fileName)
		{
			ab = null;
			foreach (var loader in loaders)
			{
				if (loader == null) continue;

				ab = loader.Load(fileName) as AssetBundle;
				if (ab != null) break;
			}

			if (ab == null)
			{
				error = "[同步]AssetBundleSyncLoader Load " + fileName + " with null result. please check the file is ok.";
			}

			// async wont return result in here.
			return ab;
		}

		public override bool isDone
		{
			get
			{
				return true;
			}
		}

		public override void Stop()
		{
			ab = null;
		}

		public override object Require()
		{
			return ab;
		}
	}

	/// <summary>
	/// assetbundle 异步加载器
	/// </summary>
	internal class AssetBundleAsyncLoader : AssetLoader<AssetBundle>
	{
		ILoader[] loaders;
		AssetBundleCreateRequest abcr;
		bool _stop = false;

		public AssetBundleAsyncLoader()
		{
			loaders = new ILoader[]
			{
				DownloadAssetBundleAsync.Instance,
					#if UNITY_ANDROID && !UNITY_EDITOR
					AndroidAssetBundleAsync.Instance,
					#else
					AppAssetBundleAsync.Instance,
					#endif
			};
		}

		public override object Load(string fileName)
		{
			foreach (var loader in loaders)
			{
				if (loader == null) continue;

				abcr = loader.Load(fileName) as AssetBundleCreateRequest;
				if (abcr != null) break;
			}

			if (abcr == null)
			{
				error = "[异步]BundleAsyncLoader Load " + fileName + " with null request. please check the file is ok.";
			}

			// async wont return result in here.
			return null;
		}

		public override bool isDone
		{
			get
			{
				// todo: 这个不能被停止
				if (_stop == true) return true;

				return abcr != null ? abcr.isDone : true;
			}
		}

		public override void Stop()
		{
			//if (isDone && abcr != null && abcr.assetBundle != null)
			//{
			//	abcr.assetBundle.Unload(false);
			//}
			//abcr = null;

			_stop = true;
		}

		public override object Require()
		{
			return isDone && abcr != null && abcr.isDone ? abcr.assetBundle : null;
		}
	}
}