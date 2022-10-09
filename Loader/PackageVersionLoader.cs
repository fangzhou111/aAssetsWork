using System;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;
using SuperMobs.Core;
using UniRx;
using UnityEngine;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Package;
using System.IO;

namespace SuperMobs.AssetManager.Loader
{
	static class PackageVersionExtension
	{
		public static PackageVersion LoadPackageVersion(this AssetBundle ab)
		{
			if (ab == null) return null;

			var text = ab.LoadAllAssets()[0] as TextAsset;
			var bytes = text != null ? text.bytes : null;
			if (bytes == null)
			{
				ab.Unload(false);
				return null;
			}

			var _manifest = new PackageVersion();
			_manifest.FromStreamBytes(bytes);
			ab.Unload(false);

			return _manifest;
		}
	}

	/// <summary>
	/// 从app里面的pv加载
	/// </summary>
	internal class PackageVersionInAppLoader : SingletonLoader<PackageVersionInAppLoader, PackageVersion>
	{
		public override object Load(string _)
		{
			var ab = AppAssetBundleSync.Instance.Load(AssetPath.PACKAGE_VERSION_FILE) as AssetBundle;
			var m = ab != null ? ab.LoadPackageVersion() : null;
			if (m == null)
			{
				AssetLogger.LogException("cant load package version inside app!");
			}
			return m;
		}
	}

	internal class PackageVersionInDownloadLoader : SingletonLoader<PackageVersionInDownloadLoader, PackageVersion>
	{
		public override object Load(string _)
		{
			var ab = DownloadAssetBundleSync.Instance.Load(AssetPath.PACKAGE_VERSION_FILE) as AssetBundle;
			var m = ab != null ? ab.LoadPackageVersion() : null;
			if (m == null)
			{
				AssetLogger.LogWarning("cant load package version in download,maybe havnt update anything before!");
			}
			return m;
		}
	}
}
