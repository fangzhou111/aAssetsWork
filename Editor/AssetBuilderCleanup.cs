using SuperMobs.Core;
namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using UniRx;
	using UnityEditor;
	using System;
	using Object = UnityEngine.Object;

	using SuperMobs.AssetManager.Core;

	using System.IO;

	public partial class AssetBuilder
	{
		void DeleteUnityOutputManifestFile()
		{
			string file = AssetPath.AssetbundlePath + AssetPath.GetBuildTargetPlatform();
			if (File.Exists(file))
			{
				File.Delete(file);
			}
			if (File.Exists(file + ".manifest"))
			{
				File.Delete(file + ".manifest");
			}

			string[] files = Directory.GetFiles(AssetPath.AssetbundlePath);
			for (int i = 0; i < files.Length; i++)
			{
				string fileName = Path.GetFileName(files[i]);
				if (!fileName.EndsWith("manifest", StringComparison.Ordinal)) continue;
				File.Delete(files[i]);
			}
		}

		private void DeleteOldAssetBundles(AssetBundleBuild[] buildMap)
		{
			for (int i = 0; buildMap != null && i < buildMap.Length; i++)
			{
				DeleteOldAssetBundle(buildMap[i].assetBundleName);
			}
		}

		private void DeleteOldAssetBundle(string assetBundleName)
		{
			string file = AssetPath.AssetbundlePath + assetBundleName;

			if (File.Exists(file))
			{
				File.Delete(file);
			}

			if (File.Exists(file + ".manifest"))
			{
				File.Delete(file + ".manifest");
			}
		}

		private void DeleteAssetsBuildCachedFolder()
		{
			DirectoryInfo di = new DirectoryInfo("Assets/BuildCached");
			if (di.Exists)
			{
				di.Delete(true);
			}
		}

		/// <summary>
		/// Deletes the asset bundle not exist.
		/// 检查AssetBundles把不需要的ab删掉
		/// </summary>
		private void DeleteAssetBundleNotExist()
		{
			var allAssetBundles = AssetEditorHelper.CollectAllPath(AssetPath.AssetbundlePath, "*" + AssetPath.ASSETBUNDLE_SUFFIX);
			foreach (var bundle in allAssetBundles)
			{
				FileInfo fi = new FileInfo(bundle);

				if (fi.Name.Equals(AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX)) continue;

				string bundleCrc = Crc32.GetStringCRC32(fi.Name).ToString();
				var cachs = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, "*." + bundleCrc + "*.info");
				if (cachs.Length <= 0)
				{
					AssetBuilderLogger.Log(Color.magenta, "[DeleteNotUseAssetBundle] >> " + fi.Name);
					File.Delete(bundle);
				}
			}
		}

		/// <summary>
		/// Cleanup AssetBundles Builded manifest and BuildCached folder.
		/// run this after build assetbundles
		/// </summary>
		private void SyncCleanup()
		{
			// delete all unity output manifest files
			// because we cached manifest info and we dont need thoes
			DeleteUnityOutputManifestFile();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 删掉空引用的AssetBundles
            var cachService = Service.Get<AssetCachService>();
			cachService.DeleteEmptyLinkAssetBundles();

            // 不在Assets下面显示缓存的对象??
            DeleteAssetsBuildCachedFolder();

			Debug.Log("Cleanup over!");

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}

	}
}
