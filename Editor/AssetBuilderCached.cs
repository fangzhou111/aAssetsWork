using SuperMobs.Core;
using System.Linq;
namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using UniRx;
	using UnityEditor;
	using System;
	using Object = UnityEngine.Object;


	/**
	 * 还原、缓存打包资源信息
	 * 
	 * 出库：
	 * 	对比当前和库里面，删除旧的
	 * 	对比库信息是否匹配一样的校验crc并还原正确
	 * 	递归还原依赖对象时候增加依赖打包对象
	 * 
	 * 入库：
	 * 	刷新当前打包后的信息入库
	 * 
	 * */

	public partial class AssetBuilder
	{
		/// <summary>
		/// 对需要打包的资源进行还原处理
		/// 对比缓存库信息：去掉该类型资源不存在的老的资源，并且判断crc是否一致还原是否正确，增加新的依赖打包资源
		/// </summary>
		private void SyncRestoreSourcesFromCachedServer()
		{
			AssetCachService cachService = Service.Get<AssetCachService>();

			cachService
				.restoredSourcePaths
				.ObserveAdd()
				//.Do((obj) => AssetBuilderLoger.Log("restore:" + obj.Key + " = " + obj.Value))
				// 如果还原失败，但是没有添加进“改变记录”则添加进去
				.Where(obj => obj.Value == false && changedSources.Contains(obj.Key) == false)
				.Do((obj) => changedSources.Add(obj.Key))
			   	.Subscribe();

			//cachService
			//	.restoredSourcePaths
			//	.ObserveReplace()

			//	.Subscribe();

			// 监听中途增加的变化的资源
			changedSources
				.ObserveAdd()
				//.Do(_ => AssetBuilderLogger.Log(Color.yellow, "change sources add >" + _.Value))
				.Subscribe();

			// 监听中途移除的变化资源
			changedSources
				.ObserveRemove()
				//.Do(_ => AssetBuilderLogger.Log(Color.red, "change sources remove >" + _.Value))
				.Subscribe();

			// first remove old!!!!
			SyncRefreshSourcesAndRemoveOld();


			// 执行还原操作，如果还原正确则移除！
			var cloneSources = changedSources.CopyAsSafeEnumerable();
			foreach (var sourcePath in cloneSources)
			{
				if (cachService.RestoreFromCach(sourcePath))
				{
					changedSources.Remove(sourcePath);
				}
			}
		}

		// remove old and dont needed.
		void SyncRefreshSourcesAndRemoveOld()
		{
			AssetCachService cachService = Service.Get<AssetCachService>();

			bool changed = false;
			var collection = cachService.GetAssetCollectionOfAssetKind(this.assetKind);
			if (collection == null)
			{
				AssetBuilderLogger.Log(this.assetKind + " 's collection is not exist!");

				collection = new AssetKindCachInfo();
				collection.kind = this.assetKind;
				collection.sources = changedSources.ToArray();
				changed = true;
			}
			else
			{
				var oldList = new List<string>(collection.sources);

				foreach (var old in collection.sources)
				{
					if (changedSources.Contains(old) == false)
					{
						cachService.DeleteSourceCachInfo(old);
						oldList.Remove(old);
						changed = true;
					}
				}

				foreach (var current in changedSources)
				{
					if (oldList.Contains(current) == false)
					{
						oldList.Add(current);
						changed = true;
					}
				}

				collection.sources = oldList.ToArray();
			}

			if (changed)
				cachService.SaveAssetCollection(collection);
			else
				AssetBuilderLogger.Log(Color.yellow, "[WARN] collection of " + assetKind + " was not changed.");
		}

		/// <summary>
		/// 当打包完毕后，储存处理好的打包对象的信息
		/// </summary>
		private void SyncStoreSourcesToCachedServer()
		{
			AssetCachService cachService = Service.Get<AssetCachService>();
			foreach (var info in cachInfos)
			{
				//AssetBuilderLogger.Log(Color.gray, "save cachInfo > " + info.sourcePath);
				cachService.SaveCachInfo(info);
			}
			AssetBuilderLogger.Log("store source cach info over.");
		}

		/// <summary>
		/// 删除指定类型资源的打包
		/// 需要判断依赖资源是否存在别的kind才删掉哦
		/// </summary>
		private bool DeleteCachedWithAssetKind(string[] kinds)
		{
			if (kinds == null || kinds.Length == 0) return false;

			AssetCachService cachService = Service.Get<AssetCachService>();

			foreach (var kind in kinds)
			{
				var collection = cachService.GetAssetCollectionOfAssetKind(kind);
				if (collection == null)
				{
					continue;
				}

				AssetBuilderLogger.Log("Delete All Build with " + kind + " begin!");

				foreach (var old in collection.sources)
				{
					cachService.DeleteSourceCachInfo(old);
				}

				cachService.DeleteAssetCollectionOfAssetKind(kind);

				AssetBuilderLogger.Log(Color.green, "Delete All Build with " + kind + " done!");
			}

			cachService.DeleteEmptyLinkAssetBundles();

			return true;
		}

	}
}