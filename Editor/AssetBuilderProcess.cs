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

	public partial class AssetBuilder
	{

		// 提前处理和优化原始资源
		// 例如：对地图文件、ui预制物、角色对象进行优化和文件设置的检测
		List<IOptimizeBuildAsset> preOptimizeBuildSourcesAPIs = new List<IOptimizeBuildAsset>();

		// 改变的原始资源的assetPaths
		// 包括分离的原始资源
		ReactiveCollection<string> changedSources = new ReactiveCollection<string>();

		// 处理打包资源的接口
		List<IProcessBuildAsset> processBuildSourcesAPIs = new List<IProcessBuildAsset>();

		// 就是需要打包的资源已经被处理过了，记录一下
		ReactiveCollection<string> sourcesHasProcessed = new ReactiveCollection<string>();

		// 把进行打包的资源 > 产生真正打包的资源[]
		// 这里也就是所有打包的资源都要进来
		ReactiveDictionary<string, List<string>> sourceToBuildPathDict = new ReactiveDictionary<string, List<string>>();
		string LogSourceToBuilds()
		{
			string ret = "All Source To Build :\n";
			foreach (var item in sourceToBuildPathDict)
			{
				string str = item.Key + "\n";
				foreach (var build in item.Value)
				{
					str += "   > " + build + "\n";
				}
				str += "\n";

				ret += str;
			}
			return ret;
		}

		void AddBuildPathForSource(string source, string[] builds)
		{
			if (!sourceToBuildPathDict.ContainsKey(source))
			{
				sourceToBuildPathDict.Add(source, new List<string>());
			}

			foreach (string build in builds)
			{
				sourceToBuildPathDict[source].AddSafe(build);
			}
		}

		void SyncDoSubscribePreOptimizeAPIs(string sourcePath)
		{
			foreach (var api in preOptimizeBuildSourcesAPIs)
			{
				if (api.IsVaild(sourcePath))
				{
					AssetBuilderLogger.Log("<color=#" + Color.green.ColorToHex() + ">"
										   + "[" + api.GetType().ToString() + "]"
										   + " process : " + sourcePath + " to \n"
										   + "</color>");

					api.DoOptimize(sourcePath);

                    // optimize可以多个api同时处理
					// break;
				}
			}

		}

		/// <summary>
		/// 打包前先对原始资源进行处理
		/// 例如：对资源进行统一优化，贴图压缩格式的处理等
		/// </summary>
		void SyncPreprocessSources()
		{
			foreach (var sourcePath in changedSources)
			{
				SyncDoSubscribePreOptimizeAPIs(sourcePath);
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			AssetBuilderLogger.Log("pre process over.");
		}



		// process sources

		void SyncDoSubscribeProcessAPIs(string source)
		{
			foreach (var api in processBuildSourcesAPIs)
			{
				if (api.IsValid(source))
				{
					var buildAssets = api.DoProcess(source);

					AssetBuilderLogger.Log("<color=#" + Color.green.ColorToHex() + ">"
										   + "[" + api.GetType().ToString() + "]"
										   + " process : " + source + " to \n"
										   + "</color>"
										   + buildAssets.ToArrayString());

                    AddBuildPathForSource(source, buildAssets);
					SyncSplitSources(new List<string>(buildAssets));

					// this is only process once!
					break;
				}
			}
		}

		/// <summary>
		/// 打包的变化的“原始”资源
		/// NOTE:
		/// 	这里也可以提供给过程中分离的新的原始资源打包
		/// 	也要把分离的资源添加进changeSources里面再process一次	
		/// 
		/// </summary>
		private void SyncProcessSources()
		{
			AssetBuilderLogger.Log("wanna process change sources count > " + changedSources.Count
								   + "\n" + changedSources.ToArrayString());

			SyncProcessSources(changedSources.ToList());
		}

		void SyncProcessSources(List<string> sources)
		{
			foreach (var source in sources)
			{
				if (sourcesHasProcessed.Contains(source) == false)
				{
					sourcesHasProcessed.Add(source);
					SyncDoSubscribeProcessAPIs(source);
				}
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}

	}
}