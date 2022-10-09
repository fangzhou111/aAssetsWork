using System.Collections.Generic;
using System.Linq;
namespace SuperMobs.AssetManager.Editor
{
	using System;
	using System.IO;
	using UnityEditor;
	using UnityEngine;
	using SuperMobs.AssetManager.Core;
	using UniRx;

	/**
	 * .info单个资源的缓存信息命名格式: assetpath.crc32.bundleName.crc32[]
	 * 
	 * */

	public class AssetCachService
	{
		// 克隆在Unity目录里面的打包资源
		public const string CLONE_PATH_PREFIX = "Assets/BuildCached/";

		// 还原过的资源列表
		// sourcepath - restore result
		public ReactiveDictionary<string, bool> restoredSourcePaths = new ReactiveDictionary<string, bool>();

		internal string GenInfoPath(AssetCachInfo info)
		{
			string sourceCrc32 = Crc32.GetStringCRC32(info.sourcePath).ToString();
			string bundlesCrc32 = "";

			foreach (var bundle in info.buildBundleNames)
			{
				bundlesCrc32 += Crc32.GetStringCRC32(bundle).ToString() + ".";
			}

			return AssetPath.CachedAssetsPath + sourceCrc32 + "." + bundlesCrc32 + "info";
		}

		internal string FindCachInfoBySourcePath(uint sourceCrc32)
		{
			var infoPaths = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, sourceCrc32 + ".*.info");
			if (infoPaths.Length > 1)
			{
				string str = "";
				foreach (var infoPath in infoPaths)
				{
					AssetCachInfo info = new AssetCachInfo();
					info.FromBytes(File.ReadAllBytes(infoPath));
					str += (info.ToJson() + "\n");
				}
				str = "存在同一个资源打包进两个不同的Bundle的，请检查BuildMap设置AB名称: : " + sourceCrc32 + "\n" + infoPaths.ToArrayString() + str;
				//for (int i = 1; i < infoPaths.Length; i++)
				//{
				//    File.Delete(infoPaths[i]);
				//}
				throw new Exception(str);
			}

			return infoPaths.Length > 0 ? infoPaths[0] : string.Empty;
		}

		internal string FindCachInfoBySourcePath(string sourcePath)
		{
			return FindCachInfoBySourcePath(Crc32.GetStringCRC32(sourcePath));
		}

		internal AssetCachInfo FindAndLoadCachInfo(string sourcePath)
		{
			return FindAndLoadCachInfo(Crc32.GetStringCRC32(sourcePath));
		}

		internal AssetCachInfo FindAndLoadCachInfo(uint sourcePathCrc)
		{
			var infoPath = FindCachInfoBySourcePath(sourcePathCrc);
			if (string.IsNullOrEmpty(infoPath)) return null;

			AssetCachInfo info = new AssetCachInfo();
			info.FromBytes(File.ReadAllBytes(infoPath));
			return info;
		}

		/// <summary>
		/// FIX：提供给打包时候，需要保证缓存库里面的某个资源clone的是否能正确还原
		/// </summary>
		internal bool CheckSourceContainCloneAssets(AssetCachInfo info)
		{
			foreach (string build in info.buildPaths)
			{
				if (!build.StartsWith(CLONE_PATH_PREFIX, StringComparison.Ordinal)) continue;
				return true;
			}
			return false;
		}

		public bool RestoreFromCach(string sourcePath)
		{
			return RestoreFromCach(sourcePath, true);
		}

		/// <summary>
		/// 还原某个打包的原始文件
		/// 原始文件导入时候需要考虑：同一个Bundle变化的话其他也要处理
		/// </summary>
		public bool RestoreFromCach(string sourcePath, bool mustRestore)
		{
			if (restoredSourcePaths.ContainsKey(sourcePath))
			{
				return restoredSourcePaths[sourcePath];
			}

			string infoPath = FindCachInfoBySourcePath(sourcePath);
			if (File.Exists(infoPath))
			{
				AssetCachInfo info = new AssetCachInfo();
				info.FromBytes(File.ReadAllBytes(infoPath));

				uint sourceMetaCrc = MetaEditor.GetAssetMetaCrc(sourcePath);

				// 如果不关心crc是否匹配，只需要文件确保有还原就行
				if (sourceMetaCrc == info.sourceCrc || mustRestore == false)
				{
					foreach (string build in info.buildPaths)
					{
						if (!build.StartsWith(CLONE_PATH_PREFIX, StringComparison.Ordinal)) continue;

						uint crc = Crc32.GetStringCRC32(build);

						string cachfilepath = AssetPath.CachedAssetsPath + crc + ".cach";
						string cachfilemetapath = AssetPath.CachedAssetsPath + crc + ".meta.cach";

						if (File.Exists(cachfilepath) == false || File.Exists(cachfilemetapath) == false)
						{
							//restoredSourcePaths[sourcePath] = false;
							//return false;
							goto FAILED;
						}

						string targetFile = AssetPath.ProjectRoot + build;
						string targetFileMeta = AssetPath.ProjectRoot + build + ".meta";

						FileInfo fi = new FileInfo(targetFile);
						if (fi.Directory.Exists == false)
							fi.Directory.Create();

						// 删除旧的
						if (File.Exists(targetFile)) File.Delete(targetFile);
						if (File.Exists(targetFileMeta)) File.Delete(targetFileMeta);

						// 把Assets资源备份到备份目录
						FileUtil.CopyFileOrDirectory(cachfilepath, targetFile);
						FileUtil.CopyFileOrDirectory(cachfilemetapath, targetFileMeta);
					}

					// TODO 是否需要判断如果link资源没有还原到是否也算这个主sources还原不正确
					foreach (var item in info.linkSourcePaths)
					{
						RestoreFromCach(item, mustRestore);
					}

					// crc pass check if bundle all exist.
					foreach (var item in info.buildBundleNames)
					{
						if (File.Exists(AssetPath.AssetbundlePath + item) == false)
						{
							AssetBuilderLogger.LogError("crc pass BUT cant found bundle path = " + AssetPath.AssetbundlePath + item);
							goto FAILED;
						}
					}

					restoredSourcePaths[sourcePath] = true;
                    //AssetBuilderLogger.Log(Color.green, "restore file from cach success : " + sourcePath + ", mustRestore = " + mustRestore);
					return true;
				} // meta crc equa

			// the cached is changed.
			// delete the cached files
			FAILED:
				DeleteCachFile(info);
			}

			restoredSourcePaths[sourcePath] = false;
			return false;
		}

		void DeleteCachFile(AssetCachInfo info)
		{
			DeleteCachFile(info, null);
		}


		void DeleteCachFile(AssetCachInfo info, List<string> validBundles)
		{
			foreach (var item in info.buildPaths)
			{
				if (!item.StartsWith(CLONE_PATH_PREFIX, StringComparison.Ordinal)) continue;

				uint crc = Crc32.GetStringCRC32(item);

				string cachfilepath = AssetPath.CachedAssetsPath + crc + ".cach";
				string cachfilemetapath = AssetPath.CachedAssetsPath + crc + ".meta.cach";

				if (File.Exists(cachfilepath)) File.Delete(cachfilepath);
				if (File.Exists(cachfilemetapath)) File.Delete(cachfilemetapath);

				string targetFile = AssetPath.ProjectRoot + item;
				string targetFileMeta = AssetPath.ProjectRoot + item + ".meta";

				// 删除旧的
				if (File.Exists(targetFile)) File.Delete(targetFile);
				if (File.Exists(targetFileMeta)) File.Delete(targetFileMeta);
			}

			// 为了删掉这个缓存info的信息
			// 也需要把它所属的bundle删掉？重新生成?
			if (validBundles != null)
			{
				foreach (var item in info.buildBundleNames)
				{
					if (validBundles != null && validBundles.Contains(item) == true)
					{
						continue;
					}
					string bundlePath = AssetPath.AssetbundlePath + item;
					if (File.Exists(bundlePath)) File.Delete(bundlePath);
				}
			}

			string cachInfoPath = GenInfoPath(info);
			AssetBuilderLogger.Log(Color.red, "删掉" + info.sourcePath + " CachInfo:" + cachInfoPath);
			if (File.Exists(cachInfoPath)) File.Delete(cachInfoPath);
		}

		/// <summary>
		/// 把打包过程中生成的某个资源打包信息记录到本地
		/// 	判断是否有BuildCached生成过的对象，则缓存进缓存库
		/// </summary>
		public void SaveCachInfo(AssetCachInfo info)
		{
			//bool somethingcach = false;

			// 只有在BuildCached目录才需要缓存
			foreach (string build in info.buildPaths)
			{
				if (!build.StartsWith("Assets/BuildCached", StringComparison.Ordinal)) continue;

				uint crc = Crc32.GetStringCRC32(build);

				string cachfilepath = AssetPath.CachedAssetsPath + crc + ".cach";
				string cachfilemetapath = AssetPath.CachedAssetsPath + crc + ".meta.cach";

				// 删除旧的
				if (File.Exists(cachfilepath)) File.Delete(cachfilepath);
				if (File.Exists(cachfilemetapath)) File.Delete(cachfilemetapath);

				string targetFile = AssetPath.ProjectRoot + build;
				string targetFileMeta = AssetPath.ProjectRoot + build + ".meta";

				// 把Assets资源备份到备份目录
				FileUtil.CopyFileOrDirectory(targetFile, cachfilepath);
				FileUtil.CopyFileOrDirectory(targetFileMeta, cachfilemetapath);

				//somethingcach = true;
			}

			//if (somethingcach)
			{
				info.Save(GenInfoPath(info));
			}
		}

		/// <summary>
		/// 搜索缓存打包文件信息，获取相同bundle的打包资源构成同一个abb
		/// </summary>
		private ReactiveDictionary<string, AssetBundleBuild> _cachSameABBs = new ReactiveDictionary<string, AssetBundleBuild>();
		public AssetBundleBuild SearchSameAssetBundleBuildInCach(string assetBundleName)
		{
			var abb = new AssetBundleBuild();

			// 缓存和加速
			if (_cachSameABBs.TryGetValue(assetBundleName, out abb))
			{
				return abb;
			}
			else
				abb.assetNames = new string[0];

			// 搜索同一个bundle的其他所有的info信息
			string bundleCrc = Crc32.GetStringCRC32(assetBundleName).ToString();
			var files = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, "*." + bundleCrc + "*.info");
			//AssetBuilderLogger.Log(">>>>got same bundle {" + assetBundleName + "} old files count = " + files.Length);
			foreach (var file in files)
			{
				var info = new AssetCachInfo();
				info.FromBytes(File.ReadAllBytes(file));

				// FIX:需要考虑当获取之前打过的AB里面的其他资源
				// 需要考虑它们是否已经不存在了（例如换了目录）
				if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(info.sourcePath) == null)
				{
					AssetBuilderLogger.LogError("打包" + assetBundleName + "的时候之前的资源:" + info.sourcePath
											   + "已经不存在了！ 这里会删掉它的缓存记录！如果改了文件路径记得把它相关资源也打一次！");

					DeleteSourceCachInfo(info.sourcePath);
					continue;
				}

				// FIX：不考虑不同，因为可能当前场景并没有build这个资源
				// 当时需要同时build，例如shader，所以只关心还原,而且一定要有东西还原
				if (RestoreFromCach(info.sourcePath, false) == false && CheckSourceContainCloneAssets(info))
				{
					AssetBuilderLogger.LogError("why wanna build the same bundle > "
										   + assetBundleName + " but other some source > "
										   + info.sourcePath + " contain clone asset can't restore! please check!");
				}

				abb.assetBundleName = assetBundleName;

				for (int i = 0; i < info.buildBundleNames.Length; i++)
				{
					if (info.buildBundleNames[i].Equals(assetBundleName) == false) continue;

					// 把同一个需要打包的资源添加进去
					string buildPath = info.buildPaths[i];
					abb.assetNames = abb.assetNames.AddSafe(buildPath);
				}
			}

			// cached
			_cachSameABBs[assetBundleName] = abb;

			return abb;
		}

		public AssetKindCachInfo GetAssetCollectionOfAssetKind(string assetKind)
		{
			string file = AssetPath.CachedAssetsPath + assetKind + ".kind";
			if (File.Exists(file))
			{
				AssetKindCachInfo info = new AssetKindCachInfo();
				info.FromBytes(File.ReadAllBytes(file));
				return info;
			}
			return null;
		}

		public void DeleteAssetCollectionOfAssetKind(string assetKind)
		{
			string file = AssetPath.CachedAssetsPath + assetKind + ".kind";
			if (File.Exists(file))
			{
				File.Delete(file);
			}
		}

		/// <summary>
		/// 删除某个老的资源相关的缓存信息
		/// NOTE:不考虑删除的bundle是否有别的链接
		/// </summary>
		public void DeleteSourceCachInfo(string sourcePath)
		{
			var infoPath = FindCachInfoBySourcePath(sourcePath);
			if (File.Exists(infoPath) == false) return;

			var info = new AssetCachInfo();
			info.FromBytes(File.ReadAllBytes(infoPath));
			DeleteCachFile(info);

			AssetBuilderLogger.Log(Color.red, "[REM]Delete old sourcePath :" + sourcePath + " with \n" + infoPath);
		}

		public void SaveAssetCollection(AssetKindCachInfo collection)
		{
			string file = AssetPath.CachedAssetsPath + collection.kind + ".kind";
			if (File.Exists(file)) File.Delete(file);
			collection.Save(file);
			AssetBuilderLogger.Log(Color.green, "[Save] save collection > " + file + " with " + collection.sources.Length + " files");
		}

		/// <summary>
		/// 删掉空链接的资源
		/// 获取所有主资源，然后判断它关联的links
		/// 然后判断当前库中没有关联的LINK资源就删掉 
		/// </summary>
		public void DeleteEmptyLinkAssetBundles()
		{
			var kindInfos = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, "*.kind");
			List<string> mainAssets = new List<string>();
			foreach (var item in kindInfos)
			{
				var kindInfo = new AssetKindCachInfo();
				kindInfo.FromBytes(File.ReadAllBytes(item));
				mainAssets.AddSafe(kindInfo.sources);
			}

			// 获取有效的info路径和bundle名称
			List<string> validInfoPaths = new List<string>();
			List<string> validBundles = new List<string>();

			foreach (var item in mainAssets)
			{
				//string sourceCrc32 = Crc32.GetStringCRC32(item).ToString();
				var infoPath = FindCachInfoBySourcePath(item);
				var cachInfo = new AssetCachInfo();
				if (string.IsNullOrEmpty(infoPath))
				{
					AssetBuilderLogger.LogError("DeleteEmptyLinkAssetBundles 处理mainAsset: " + item + "找不到cachInfo:" + Crc32.GetStringCRC32(item) + ".*.info");
					continue;
				}
				cachInfo.FromBytes(File.ReadAllBytes(infoPath));

				validInfoPaths.AddSafe(infoPath);
				validBundles.AddSafe(cachInfo.buildBundleNames);

				foreach (var link in cachInfo.linkSourcePaths)
				{
					var linkInfoPath = FindCachInfoBySourcePath(link);
					if (string.IsNullOrEmpty(linkInfoPath))
					{
						AssetBuilderLogger.LogError("DeleteEmptyLinkAssetBundles时候处理主资源:" + item
													+ "的linkSource:" + link + "并没有找到打包的记录。可能改文件已经不存在了:" + Crc32.GetStringCRC32(link) + ".*.info");

						continue;
					}

					var linkCachInfo = new AssetCachInfo();
					linkCachInfo.FromBytes(File.ReadAllBytes(linkInfoPath));

					validInfoPaths.AddSafe(linkInfoPath);
					validBundles.AddSafe(linkCachInfo.buildBundleNames);
				}
			}

			// 获取所有asset资源 
			var allInfoPaths = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, "*.info");
			foreach (var item in allInfoPaths)
			{
				if (validInfoPaths.Contains(item) == false)
				{
					AssetBuilderLogger.Log(Color.red, "[DeleteEmptyAssetBuiled] >> " + item);
					var cachInfo = new AssetCachInfo();
					cachInfo.FromBytes(File.ReadAllBytes(item));
					DeleteCachFile(cachInfo, validBundles);
				}
			}

			// 判断assetbundles中如果不存在的ab文件呢
			var allAssetBundles = AssetEditorHelper.CollectAllPath(AssetPath.AssetbundlePath, "*" + AssetPath.ASSETBUNDLE_SUFFIX);
			foreach (var bundle in allAssetBundles)
			{
				FileInfo fi = new FileInfo(bundle);

				if (fi.Name.Equals(AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX)) continue;

				if (validBundles.Contains(fi.Name) == false)
				{
					AssetBuilderLogger.Log(Color.magenta, "[DeleteNotUseAssetBundle] >> " + fi.Name);
					File.Delete(bundle);
				}
			}
		}
	}
}
