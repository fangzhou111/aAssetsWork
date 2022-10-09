namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using UniRx;
	using UnityEditor;
	using System;
	using Object = UnityEngine.Object;
	using SuperMobs.AssetManager.Assets;

	/**
	 * 	提供组合过简单的方法直接编译某类资源
	 * */


	public partial class AssetBuilder
	{
		public void Execute(Action callback = null)
		{
			DateTime d0 = DateTime.Now;

			SyncPreprocessSources();
			SyncRestoreSourcesFromCachedServer();
			SyncProcessSources();
			SyncGenbuildmap();

			// remove default-fbx shader before building
			RemoveDefaultFBXMat();

			SyncBuildAssetBundles();

			// restore default-fbx shader after building 
			RestoreDefaultFBXMat();

			SyncStoreSourcesToCachedServer();
			SyncCleanup();

			if (rebuildABCount > 0)
			{
				AssetManifestEditor.GenManifestFile();
			}

			AssetBuilderLogger.Log(Color.green, assetKind + " (*^__^*)yeh! Rebuild AB count =" + rebuildABCount
								   + " asset =" + changedSources.Count
								   + " cost:" + (DateTime.Now - d0).TotalMinutes.ToString("F2") + "分钟");
			if (callback != null) callback();

		}

		public void ExecuteClean(string[] kinds = null)
		{
			string[] ks = kinds ?? new string[] { assetKind };
			if (DeleteCachedWithAssetKind(ks))
			{
				AssetManifestEditor.GenManifestFile();
			}
			DeleteAssetBundleNotExist();
		}
	}
}
