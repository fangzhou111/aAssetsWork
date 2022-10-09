using System;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;
using SuperMobs.Core;
using UniRx;
using UnityEngine;
using SuperMobs.AssetManager.Assets;
using System.IO;
using SuperMobs.AssetManager.Package;

namespace SuperMobs.AssetManager.Loader
{
	static class PackageManifestExtension
	{
		public static PackageManifest LoadPackageManifest(this AssetBundle ab)
		{
			if (ab == null) return null;

			var text = ab.LoadAllAssets()[0] as TextAsset;
			var bytes = text != null ? text.bytes : null;
			if (bytes == null)
			{
				ab.Unload(false);
				return null;
			}

			var _manifest = new PackageManifest();
			_manifest.FromStreamBytes(bytes);
			ab.Unload(false);

			return _manifest;
		}
	}

	internal class PackageManifestInAppLoader : SingletonLoader<PackageManifestInAppLoader ,PackageManifest>
	{
		public override object Load(string _)
		{
			var ab = AppAssetBundleSync.Instance.Load(AssetPath.PACKAGE_MANIFEST_FILE) as AssetBundle;
			var m = ab != null ? ab.LoadPackageManifest() : null;
			if (m == null)
			{
				AssetLogger.LogException("cant load packagemanifest inside app!");
			}
			return m;
		}
	}

	internal class PackageManifestInDownloadLoader : SingletonLoader<PackageManifestInDownloadLoader ,PackageManifest>
	{
		public override object Load(string _)
		{
			var ab = DownloadAssetBundleSync.Instance.Load(AssetPath.PACKAGE_MANIFEST_FILE) as AssetBundle;
			var m = ab != null ? ab.LoadPackageManifest() : null;
			if (m == null)
			{
				AssetLogger.LogWarning("cant load packagemanifest in download,maybe havnt update anything before!");
			}
			return m;
		}
	}



}
