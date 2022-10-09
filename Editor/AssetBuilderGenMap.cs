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
	using SuperMobs.AssetManager.Assets;

	public partial class AssetBuilder
	{
		// gen build apis
		List<IGenBuildmap> genbuildmapAPIs = new List<IGenBuildmap>();

		// all asset bundle build
		AssetBundleBuildMap buildmap = new AssetBundleBuildMap();

		// 需要打包的对象缓存信息
		List<AssetCachInfo> cachInfos = new List<AssetCachInfo>();

		// buildPath > ABB
		Dictionary<string, AssetBundleBuild> buildAssetToABB = new Dictionary<string, AssetBundleBuild>();

		// bundleName > build mode
		Dictionary<string, AssetBuildType> bundleToBuildMode = new Dictionary<string, AssetBuildType>();

		// assetBundleName > links
		// Dictionary<string, List<string>> bundleToLinksDict = new Dictionary<string, List<string>>();

		// source asset to bundles[]
		Dictionary<string, List<string>> sourceToBundleDict = new Dictionary<string, List<string>>();

		AssetCachInfo GenCachedInfo(string sourcePath, List<string> buildSources)
		{
			AssetCachInfo ci = new AssetCachInfo();
			ci.sourcePath = sourcePath;
			ci.sourceCrc = MetaEditor.GetAssetMetaCrc(sourcePath);

			// 这个对象产生的真正要打包的对象路径（可能是需要clone的)
			ci.buildPaths = buildSources.ToArray();

			return ci;
		}

		void SyncDoSubscribeBuildMapAPIs(string sourcePath,string buildPath)
		{
			foreach (var api in genbuildmapAPIs)
			{
				if (api.IsVaild(buildPath) == false) continue;
				var abb = api.GenAssetBundleBuild(buildPath);
                abb.assetBundleName = abb.assetBundleName.ToLower();

                // 保护这个资源曾经是否也打包过，但是bundle却不一样？
                var cachInfo = Service.Get<AssetCachService>().FindAndLoadCachInfo(sourcePath);
                if(cachInfo != null && cachInfo.GetBuildName(buildPath) != abb.assetBundleName)
                {
                    AssetBuilderLogger.LogError("同一个资源，不同的BundleName,改成历史的哦(*^__^*)：" + sourcePath + "\n历史打包:"+ cachInfo.GetBuildName(buildPath)
                        +"\n当前:"+abb.assetBundleName);
                    abb.assetBundleName = cachInfo.GetBuildName(buildPath);
                }
				buildAssetToABB[buildPath] = abb;

				AssetBuilderLogger.Log(Color.green, "[" + api.GetType().ToString() + "]"
									   + " genmap : " + buildPath + " to " + abb.assetBundleName);

				if (assetBuildModeDict.ContainsKey(buildPath))
				{
					// just for test tip.
					if (bundleToBuildMode.ContainsKey(buildPath) && bundleToBuildMode[buildPath] != assetBuildModeDict[buildPath])
					{
						throw new Exception("why the same bundle with difference build mode ?? > " + buildPath);
					}

					bundleToBuildMode[abb.assetBundleName] = assetBuildModeDict[buildPath];
				}
				buildmap.Add(abb);
				break;
			}
		}

		private void SyncGenbuildmap()
		{
			AssetBuilderLogger.Log("SyncGenbuildmap sourceToBuildPathDict count > " + sourceToBuildPathDict.Count);
			AssetBuilderLogger.Log(LogSourceToBuilds());

			foreach (var one in sourceToBuildPathDict)
			{
				foreach (var buildPath in one.Value)
				{
					SyncDoSubscribeBuildMapAPIs(one.Key,buildPath);
				}

				var cachInfo = GenCachedInfo(one.Key, one.Value);
				cachInfos.Add(cachInfo);
				foreach (var buildPath in cachInfo.buildPaths)
				{
					if (buildAssetToABB.ContainsKey(buildPath))
					{
						cachInfo.AddBundle(buildAssetToABB[buildPath].assetBundleName);

						// mark
						if (sourceToBundleDict.ContainsKey(cachInfo.sourcePath) == false)
						{
							sourceToBundleDict.Add(cachInfo.sourcePath, new List<string>());
						}
						sourceToBundleDict[cachInfo.sourcePath].AddSafe(buildAssetToABB[buildPath].assetBundleName);
					}

					if (buildAssetToLinksDict.ContainsKey(buildPath))
					{
						cachInfo.AddLinks(buildAssetToLinksDict[buildPath]);
					}

					if (assetBuildModeDict.ContainsKey(buildPath))
					{
						cachInfo.buildType = assetBuildModeDict[buildPath];
					}
				}

				var service = Service.Get<AssetCachService>();
				var arr = buildmap.ToArray();
				foreach (var abb in arr)
				{
					var sameAbb = service.SearchSameAssetBundleBuildInCach(abb.assetBundleName);
					if (string.IsNullOrEmpty(sameAbb.assetBundleName) == false && sameAbb.assetNames.Length > 0)
					{
						buildmap.Add(sameAbb);
					}
				}
			}

			AssetBuilderLogger.Log(buildmap.Log());
		}

	}
}