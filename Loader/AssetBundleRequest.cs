/*
 * @Author: chiuan wei 
 * @Date: 2017-11-21 00:58:25 
 * @Last Modified by:   chiuan wei 
 * @Last Modified time: 2017-11-21 00:58:25 
 */
namespace SuperMobs.AssetManager.Loader
{
	using System;
	using UnityEngine;
	using SuperMobs.AssetManager.Core;
	using SuperMobs.Core;
	using System.IO;

	/*
	 * 在“包”里面加载东西 *可能是Editor下加载Client Package的资源哦
	 * 需要考虑是否制作了大文件 *大文件只在包里存在
	 * */

	internal class AppAssetBundleAsync : SingletonLoader<AppAssetBundleAsync, AssetBundleCreateRequest>
	{
		public override object Load(string fileName)
		{
			if (isBigFileMode == false)
			{
				string path = AssetPath.GetPathInAPP(fileName);
				return AssetBundle.LoadFromFileAsync(path,0,AssetPreference.GetAssetBundleOffset());
			}
			else
			{
				// 调用大文件Loader加载
				return Service.Get<LoaderService>().GetBigFileLoader().Load(fileName);
			}
		}
	}

	internal class AppAssetBundleSync : SingletonLoader<AppAssetBundleSync, AssetBundle>
	{
		public override object Load(string fileName)
		{
			if (isBigFileMode == false)
			{
                Debug.Log(fileName);
				string path = AssetPath.GetPathInAPP(fileName);
                Debug.Log(path);
				return AssetBundle.LoadFromFile(path,0,AssetPreference.GetAssetBundleOffset());
			}
			else
			{
				// 调用大文件Loader加载
				return Service.Get<LoaderService>().GetBigFileLoader().LoadImmediate(fileName);
			}
		}
	}

	/// <summary>
	/// Android asset bundle async.
	/// </summary>

	internal class AndroidAssetBundleAsync : SingletonLoader<AndroidAssetBundleAsync, AssetBundleCreateRequest>
	{
		public override object Load(string fileName)
		{
			if (isBigFileMode == false)
			{
				string path = AssetPath.GetPathInAPP(fileName);
				return AssetBundle.LoadFromFileAsync(path,0,AssetPreference.GetAssetBundleOffset());
			}
			else
			{
				// 调用大文件Loader加载
				return Service.Get<LoaderService>().GetBigFileLoader().Load(fileName);
			}
		}
	}

	internal class AndroidAssetBundleSync : SingletonLoader<AndroidAssetBundleSync, AssetBundle>
	{
		public override object Load(string fileName)
		{
			if (isBigFileMode == false)
			{
				string path = AssetPath.GetPathInAPP(fileName);
				return AssetBundle.LoadFromFile(path,0,AssetPreference.GetAssetBundleOffset());
			}
			else
			{
				// 调用大文件Loader加载
				return Service.Get<LoaderService>().GetBigFileLoader().LoadImmediate(fileName);
			}
		}
	}



	internal class DownloadAssetBundleAsync : SingletonLoader<DownloadAssetBundleAsync, AssetBundleCreateRequest>
	{
		public override object Load(string fileName)
		{
			string path = AssetPath.GetPathInDownLoaded(fileName);
			if (File.Exists(path))
			{
				return AssetBundle.LoadFromFileAsync(path,0,AssetPreference.GetAssetBundleOffset());
			}
			else
			{
				return null;
			}
		}
	}

	internal class DownloadAssetBundleSync : SingletonLoader<DownloadAssetBundleSync, AssetBundle>
	{
		public override object Load(string fileName)
		{
			string path = AssetPath.GetPathInDownLoaded(fileName);
			if (File.Exists(path))
			{
				return AssetBundle.LoadFromFile(path,0,AssetPreference.GetAssetBundleOffset());
			}
			else
			{
				//AssetLogger.Log(Color.gray, "DownloadAssetBundleSync cant found at " + path);
				return null;
			}
		}
	}




}
