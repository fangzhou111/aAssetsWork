using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using SuperMobs.AssetManager.Core;

namespace SuperMobs.AssetManager.Editor
{
	[InitializeOnLoad]
	class AssetEditorPath : AssetPathEditorAPI
	{
		static AssetEditorPath()
		{
			AssetPath.editorApi = new AssetEditorPath();
		}

		// 获取打包assetbundle的根目录
		internal const string ASSETBUNDLE_OUT_FOLDER = "Assetbundles";
		internal const string CLONE_FOLDER = "Assets/CloneAssets/";
		internal const string CACHED_FOLDER = "CachedAssets/";

		static string _AssetBundlesFolder = string.Empty;

		/// <summary>
		/// 这里找到存放AssetBundles的目录，搜索规则
		/// 	1：在Assets同级目录查找
		/// 没找到提示exception
		/// </summary>
		private string GetEditorVersionBuilderFolder()
		{
			if (string.IsNullOrEmpty(_AssetBundlesFolder))
			{
				if (Directory.Exists(AssetPath.ProjectRoot + ASSETBUNDLE_OUT_FOLDER))
				{
					_AssetBundlesFolder = AssetPath.ProjectRoot + ASSETBUNDLE_OUT_FOLDER;
				}
				//else if (Directory.Exists(Application.dataPath + "/" + ASSETBUNDLE_OUT_FOLDER))
				//{
				//	_AssetBundlesFolder = Application.dataPath + "/" + ASSETBUNDLE_OUT_FOLDER;
				//}
				else
				{
					throw new Exception("Cant found any " + ASSETBUNDLE_OUT_FOLDER + " in your project.\n" +
									   AssetPath.ProjectRoot + ASSETBUNDLE_OUT_FOLDER);
				}
			}

			return _AssetBundlesFolder + "/" + Platform + "/";
		}

		public string AssetbundlePath
		{
			get
			{
				string path = GetEditorVersionBuilderFolder();
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				return path;
			}
		}

		public string CachedAssetsPath
		{
			get
			{
				string path = AssetbundlePath + CACHED_FOLDER;
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				return path;
			}
		}

		public string Platform
		{
			get
			{
				switch (EditorUserBuildSettings.activeBuildTarget)
				{
					case BuildTarget.Android:
						return AssetPreference.PLATFORM_ANDROID;
					case BuildTarget.iOS:
						return AssetPreference.PLATFORM_IOS;
					case BuildTarget.StandaloneWindows:
					case BuildTarget.StandaloneWindows64:
#if UNITY_2017_2_OR_NEWER
                    case BuildTarget.StandaloneOSX:
#else
                    case BuildTarget.StandaloneOSXIntel:
					case BuildTarget.StandaloneOSXIntel64:
#endif
                        return AssetPreference.PLATFORM_STANDARD;
					default:
						return string.Empty;
				}
			}
		}

		/// <summary>
		/// 在Editor下发包时候的ab存放目录
		/// USE FOR SIMULATE LOADING..SIM_PLAY
		/// </summary>
		public string AssetBundleEditorDevicePath
		{
			get
			{
				if (Platform == AssetPreference.PLATFORM_ANDROID)
				{
					//return Application.dataPath + AssetPath.DirectorySeparatorChar + "Plugins/Android/assets" + AssetPath.DirectorySeparatorChar + AssetPath.ASSETBUNDLE_DEVICE_FOLDER + AssetPath.DirectorySeparatorChar;

					return Application.dataPath + AssetPath.DirectorySeparatorChar + "StreamingAssets" + AssetPath.DirectorySeparatorChar + AssetPath.ASSETBUNDLE_DEVICE_FOLDER + AssetPath.DirectorySeparatorChar;
				}
				else if (Platform == AssetPreference.PLATFORM_IOS)
					return AssetPath.ProjectRoot + AssetPath.ASSETBUNDLE_DEVICE_FOLDER + AssetPath.DirectorySeparatorChar;
				else
					return Application.dataPath + AssetPath.DirectorySeparatorChar + "Resources/" + AssetPath.ASSETBUNDLE_DEVICE_FOLDER + AssetPath.DirectorySeparatorChar;
			}
		}

		//public static string GetClonePath(string assetPath)
		//{
		//    // 原始文件路径："Assets/UI/Map.prefab"
		//    // 目标文件路径：CLONE_FOLDER + "UI/Map.prefab"
		//    string to = CLONE_FOLDER + assetPath.Substring(assetPath.IndexOf('/') + 1);
		//    return to;
		//}

		///// <summary>
		///// 把某个路径文件复制到编译处理目录下
		///// </summary>
		//public static string CopyToBuildFolder(string assetPath, bool isReplace)
		//{
		//    bool isNeedCopyNew = false;

		//    // 原始文件路径："Assets/UI/Map.prefab"
		//    // 目标文件路径：CLONE_FOLDER + "UI/Map.prefab"
		//    string to = CLONE_FOLDER + assetPath.Substring(assetPath.IndexOf('/') + 1);

		//    // 判断目标目录是否存在
		//    string toFolder = to.Substring(0, to.LastIndexOf('/') + 1);
		//    if (!Directory.Exists(toFolder))
		//    {
		//        Directory.CreateDirectory(toFolder);
		//    }

		//    if (isReplace && File.Exists(to))
		//    {
		//        // TODO:检查是否和之前copy的一致，如果不一致才需要拷贝
		//        AssetDatabase.DeleteAsset(to);
		//        isNeedCopyNew = true;
		//    }
		//    else if (!File.Exists(to))
		//    {
		//        isNeedCopyNew = true;
		//    }

		//    if (isNeedCopyNew)
		//    {
		//        FileUtil.CopyFileOrDirectory(assetPath, to);
		//        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		//    }

		//    return to;
		//}

	}
}
