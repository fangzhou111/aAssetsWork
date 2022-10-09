using System;
using SuperMobs.AssetManager.Package;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using System.IO;

namespace SuperMobs.AssetManager.Editor
{
	public static class PackageManifestEditor
	{
		public static void AddBundle(this PackageManifest pm, Bundle bundle)
		{
			pm.assets = pm.assets ?? new System.Collections.Generic.List<PackageAsset>();

			foreach (var item in pm.assets)
			{
				if (item.nameCrc == bundle.bundleNameCrc)
				{
					throw new Exception("add the same bundle " + bundle.bundleName + " in package manifest!");
				}
			}

			string path = AssetPath.AssetbundlePath + bundle.bundleName;
			FileInfo fi = new FileInfo(path);
			if (fi.Exists == false) throw new Exception("add bundle " + bundle.bundleName + " in package manifest,but file dont exist " + path);

			PackageAsset pa = new PackageAsset();
			pa.nameCrc = bundle.bundleNameCrc;
			pa.fileCrc = Crc32.GetFileCRC32(path);
			pa.fileLength = (int)fi.Length;

			pm.assets.Add(pa);
		}

		/// <summary>
		/// 其实是把AssetManifest文件的信息记录到pm里面
		/// </summary>
		public static void AddAssetManifest(this PackageManifest pm)
		{
			string path = AssetPath.AssetbundlePath + AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX;
			FileInfo fi = new FileInfo(path);
			PackageAsset pa = new PackageAsset();
			pa.nameCrc = Crc32.GetStringCRC32(AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX);
			pa.fileCrc = Crc32.GetFileCRC32(path);
			pa.fileLength = (int)fi.Length;

			pm.assets.Add(pa);
		}
	}
}
