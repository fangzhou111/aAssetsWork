using System;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;
using SuperMobs.Core;
using UniRx;
using UnityEngine;
using SuperMobs.AssetManager.Assets;
using System.IO;

namespace SuperMobs.AssetManager.Loader
{
	/// <summary>
	/// 同步加载manifest文件
	/// </summary>
	internal class AssetManifestLoader : SingletonLoader<AssetManifestLoader, AssetManifest>
	{
		public override object Load(string _)
		{
			// manifest ab name is "manifest.ab"
			// so here to load manifest alone.
			var ab = AssetBundleSyncLoader.Instance.Load(AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX) as AssetBundle;
			if (ab != null)
			{
				var text = ab.LoadAllAssets()[0] as TextAsset;
				var bytes = text != null ? text.bytes : null;
				if (bytes == null)
				{
					ab.Unload(false);
					AssetLogger.LogException("cant load manifest,ab is ok,but cant load asset.");
					return null;
				}

				var _manifest = new AssetManifest();
				_manifest.FromStreamBytes(bytes);
				_manifest.InitContent(bytes);
				ab.Unload(false);

				return _manifest;
			}
			else
			{
				AssetLogger.LogException("cant load manifest!");
				return null;
			}
		}
	}
}
