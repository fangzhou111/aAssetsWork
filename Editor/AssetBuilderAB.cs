namespace SuperMobs.AssetManager.Editor
{
	using System.Collections.Generic;
	using System.Collections;
	using System;
	using UniRx;
	using UnityEditor;
	using UnityEngine;
	using Object = UnityEngine.Object;
	using System.IO;
	using Core;
	using SuperMobs.AssetManager.Assets;
	using UnityEditor.Sprites;

	/**
	 * 打包规则
	 * 	单个bundle资源，查找相同bundle的其他资源进行一块打包
	 * 	依赖的资源，不自己打包，放到主资源打包时候一起打包（依赖资源变化，必然主资源需要一起重新处理打包）
	 * 	
	 * */

	public partial class AssetBuilder
	{
		int rebuildABCount = 0;

		List<IBuildAssetBundle> buildAssetAPIs = new List<IBuildAssetBundle>();

		/// <summary>
		/// 重新pack sprites
		/// Fix sprite rebuild error
		/// 弃用的接口，2022.6.8 BY ZF
		/// </summary>
		public void PackSprite()
		{
			Debug.LogError("为什么会调用这个?");
			//delete the old atla in Library\AtlasCache..
			//string atlacachePath = AssetPath.ProjectRoot + "Library/AtlasCache/";
			//if (Directory.Exists(atlacachePath))
			//{
			//	Directory.Delete(atlacachePath, true);
			//}

			//Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget, false, Packer.Execution.Normal);
		}

		string PrintABB(AssetBundleBuild abb)
		{
			string ret = "abb : " + abb.assetBundleName;
			abb.assetNames
				.ToObservable(Scheduler.Immediate)
				.Do(asset => ret += "\n>>>" + asset)
				.Subscribe();
			return ret;
		}

		private void SyncBuildAssetBundles()
		{
			var arr = buildmap.ToArray();

			// 优化不需要任何依赖关联的资源,编译加速
			Dictionary<IBuildAssetBundle, List<AssetBundleBuild>> multiBuilds = new Dictionary<IBuildAssetBundle, List<AssetBundleBuild>>();

			foreach (var abb in arr)
			{
                // 如果是依赖不在这里Build
                // NOTE:依赖应该在别的包含资源里面打包了？
                if (bundleToBuildMode.ContainsKey(abb.assetBundleName) &&
                    bundleToBuildMode[abb.assetBundleName] == AssetBuildType.dependence)
                {
                    //AssetBuilderLogger.Log("bundle = " + abb.assetBundleName + " was dependence dont need to triggle build single here");
                    continue;
                }

				// 如果是single需要打包
				// 那么需要获取这个bundle的依赖bundle
				List<AssetBundleBuild> abbs = new List<AssetBundleBuild>();

				foreach (var buildPath in abb.assetNames)
				{
					if (buildAssetToLinksDict.ContainsKey(buildPath) == false) continue;

					var links = buildAssetToLinksDict[buildPath];
					foreach (var link in links)
					{
						if (sourceToBundleDict.ContainsKey(link) == false) continue;

						var bundles = sourceToBundleDict[link];
						foreach (var bundle in bundles)
						{
							var _abb = buildmap.GetByAssetBundleName(bundle);
							if (bundleToBuildMode.ContainsKey(_abb.assetBundleName) && bundleToBuildMode[_abb.assetBundleName] == AssetBuildType.dependence)
							{
								if (abbs.Contains(_abb) == false)
								{
									//AssetBuilderLogger.Log("abb " + _abb.assetBundleName + " add into " + abb.assetBundleName);
									abbs.Add(_abb);
								}
							}
						}
					}
				}

				// 别漏了自己
				abbs.Add(abb);

				// 找到build的api就开始build
				foreach (var api in buildAssetAPIs)
				{
					if (api.IsVaild(abb))
					{
						DeleteOldAssetBundles(abbs.ToArray());
						rebuildABCount++;

						if (api.GetBuildAssetBundleMode() == BuildAssetBundleMode.Single)
						{
							AssetBuilderLogger.Log(Color.green, "[" + api.GetType().ToString() + "]" +
								" build : " + abb.assetBundleName + " with " + abbs.Count + " abbs");

							api.Build(abbs);
						}
						else
						{
							AssetBuilderLogger.Log(Color.green, "[" + api.GetType().ToString() + "]" +
								" build : " + abb.assetBundleName + " with " + abbs.Count + " abbs" + " mode = multi");
							if (multiBuilds.ContainsKey(api) == false)
							{
								multiBuilds[api] = new List<AssetBundleBuild>();
							}
							var list = multiBuilds[api];
							foreach (var item in abbs)
							{
								bool _exist = false;
								foreach (var exitItem in list)
								{
									if (exitItem.assetBundleName.Equals(item.assetBundleName))
									{
										_exist = true;
										break;
									}
								}
								if (_exist == false)
								{
									list.Add(item);
								}
							}
						}

						break;
					}
				}
			}

			// 开始编译允许一起编译的独立ab
			foreach (var item in multiBuilds)
			{
				item.Key.Build(item.Value);
			}

			AssetBuilderLogger.Log("build bundle over.");
		}

	}
}