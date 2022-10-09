using System.Linq;
using SuperMobs.AssetManager.Core;
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
	 * 分离对象、分离依赖对象
	 * 
	 * */

	public partial class AssetBuilder
	{

		// 处理打包资源的分离器接口类
		List<IProcessAssetRelation> splitBuildSourcesAPIs = new List<IProcessAssetRelation>();

		// 记录打包处理的打包的资源产生的linkAssets依赖对象
		// NOTE:这里是需要打包的才进行分离！
		ReactiveDictionary<string, List<string>> buildAssetToLinksDict = new ReactiveDictionary<string, List<string>>();

		// 记录需要打包的资源的打包模式，是单个还是依赖
		ReactiveDictionary<string, AssetBuildType> assetBuildModeDict = new ReactiveDictionary<string, AssetBuildType>();

		void AddLinksForBuildPath(string buildPath, string[] links)
		{
			if (!buildAssetToLinksDict.ContainsKey(buildPath))
			{
				buildAssetToLinksDict.Add(buildPath, new List<string>());
			}

			foreach (string link in links)
			{
				buildAssetToLinksDict[buildPath].AddSafe(link);
			}
		}

		void AddAssetBuildMode(IProcessAssetRelation api, string[] links)
		{
			foreach (var item in links)
			{
				if (assetBuildModeDict.ContainsKey(item) == false)
				{
					assetBuildModeDict[item] = api.GetAssetBuildMode(item);
				}
			}
		}

		void AddLinksToChangedSources(string[] links)
		{
			foreach (var item in links)
			{
				changedSources.AddSafe(item);
			}
		}



		void SyncDoSubscribeSplitAPIs(string buildAsset)
		{
			foreach (var api in splitBuildSourcesAPIs)
			{
				if (api.IsVaild(buildAsset) == false) continue;

				var splitAssets = api.DoSplit(buildAsset);

				AssetBuilderLogger.Log("<color=#" + Color.green.ColorToHex() + ">"
									   + "[" + api.GetType().ToString() + "]"
									   + " split : " + buildAsset + " to \n"
									   + "</color>"
									   + splitAssets.ToArrayString());

                foreach (var one in splitAssets)
                {
                    if (string.IsNullOrEmpty(one))
                    {
                        throw new Exception(buildAsset + " 分离完的资源有个空的！？");
                    }
                }

                AddAssetBuildMode(api, splitAssets);
				AddAssetBuildMode(api, new string[] { buildAsset });

				AddLinksToChangedSources(splitAssets);
				AddLinksForBuildPath(buildAsset, splitAssets);

				SyncProcessSources(new List<string>(splitAssets));

				// only one api process
				break;
			}
		}

		/// <summary>
		/// 剥离掉需要打包的资源的引用资源
		/// source > 
		/// 	after_source
		/// 	split_source[] > put into sources
		/// 		run again process assets
		/// 
		/// </summary>
		void SyncSplitSources(List<string> buildAssets)
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			foreach (var buildAsset in buildAssets)
			{
				if (buildAssetToLinksDict.ContainsKey(buildAsset) == false)
				{
					SyncDoSubscribeSplitAPIs(buildAsset);
				}
			}
		}

	}
}

