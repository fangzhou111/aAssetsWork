using System;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;
using SuperMobs.Core;
using UniRx;
using UnityEngine;


namespace SuperMobs.AssetManager.Loader
{
	internal class ByteSyncLoader : SingletonLoader<ByteSyncLoader, byte[]>
	{
		readonly ILoader[] loaders;

		public ByteSyncLoader()
		{
			loaders = new ILoader[] {
			DownloadIO.Instance,

			// 先下载地址io后app里面
#if UNITY_ANDROID && !UNITY_EDITOR
			AndroidIO.Instance,
#else
			AppIO.Instance,
#endif
			};
		}

		/// <summary>
		/// 开始加载
		/// </summary>
		public override object Load(string fileName)
		{
			object ret = null;
			foreach (var loader in loaders)
			{
				if (loader == null) continue;

				ret = loader.Load(fileName);
				if (ret != null) break;
			}
			return ret;
		}
	}
}
