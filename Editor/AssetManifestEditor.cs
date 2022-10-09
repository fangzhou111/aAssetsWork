/*
 * @Author: chiuan wei 
 * @Date: 2017-06-21 13:04:24 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-06-21 14:15:39
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using SuperMobs.Core;
using UniRx;
using UnityEditor;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
   /// <summary>
   /// 负责通过遍历当前bundles库中的信息
   /// 并且生成manifest信息文件，这个文件是个二进制，储存一系列的Bundles的信息
   /// 
   /// 二进制排序:
   /// > bundles crcs collector
   /// > bundles collector
   /// > bundles items
   /// > assets collector
   /// > assets items
   /// 
   /// </summary>
   public class AssetManifestEditor
   {
      // 记录当前打包过程中处理的资源id - 缓存信息
      // 避免每次都去文件系统里面加载
      static Dictionary<uint, AssetCachInfo> assetToCachInfo = null;
      static AssetCachInfo GetCachInfoByAssetSourcePath(uint source)
      {
         AssetCachInfo ci = null;
         if (assetToCachInfo.TryGetValue(source, out ci))
         {
            return ci;
         }
         else
         {
            ci = Service.Get<AssetCachService>().FindAndLoadCachInfo(source);
            assetToCachInfo[source] = ci;
            return ci;
         }
      }

      static AssetLoadType GetAssetLoadType(string assetPath)
      {
         if (assetPath.EndsWith(".prefab", StringComparison.Ordinal))
         {
            return AssetLoadType.GameObject;
         }
         else if (AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) != null)
         {
            return AssetLoadType.Sprite;
         }
         else if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) is Texture)
         {
            return AssetLoadType.Texture;
         }
         else if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) is TextAsset)
         {
            return AssetLoadType.TextAsset;
         }
         else if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) is Mesh)
         {
            return AssetLoadType.Mesh;
         }
         else
         {
            return AssetLoadType.Unknown;
         }
      }

      /// <summary>
      /// 根据当前库打包的文件，生成加载需要的manifest
      /// </summary>
      public static void GenManifestFile()
      {
         var col = new Color(0.5f, 0.25f, 0.25f);
         AssetBuilderLogger.Log(col, "start gen manifest file....");

         assetToCachInfo = new Dictionary<uint, AssetCachInfo>();

         var assets = CollectAllAssets();
         if (assets.Length == 0)
         {
            AssetBuilderLogger.Log(col, "no asset to gen manifest file.");
            var path = AssetPath.AssetbundlePath + AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX;
            if (File.Exists(path))
            {
               File.Delete(path);
            }
            return;
         }

         var bundles = CollectAllBundlesFromAssets(assets);
         var bundlecrc = CollectBundleCrcsFromBundles(bundles);
         AssetBuilderLogger.Log(col, "assets count = " + assets.Length);
         AssetBuilderLogger.Log(col, "bundles count = " + bundles.Length);
         AssetBuilderLogger.Log(col, "bundlecrc count = " + bundlecrc.names.Length);

         byte[] bundlecrcBytes = bundlecrc.ToBytes();
         AssetBuilderLogger.Log(col, "bundle crc bytes length = " + bundlecrcBytes.Length);

         byte[] bundleBytes = null;
         var bundleCollector = ConvertAssetsToCollector<Bundle>(bundles, arg => (arg as Bundle).bundleNameCrc, out bundleBytes);
         byte[] bundleCollectorBytes = bundleCollector.ToBytes();
         AssetBuilderLogger.Log(col, "bundle collector count = " + bundleCollector.names.Length + "\nbytes length = " + bundleCollectorBytes.Length);

         byte[] assetBytes = null;
         var assetCollector = ConvertAssetsToCollector<Asset>(assets, arg => (arg as Asset).sourcePathCrc, out assetBytes);
         byte[] assetCollectorBytes = assetCollector.ToBytes();
         AssetBuilderLogger.Log(col, "asset collector count = " + assetCollector.names.Length + "\nbytes length = " + assetCollectorBytes.Length);

         // begin is asset manifest
         AssetManifest manifest = new AssetManifest();
         manifest.bundleCrcCollectorIndex = AssetManifest.MANIFEST_LENGTH;
         manifest.bundleCollectorIndex = manifest.bundleCrcCollectorIndex + bundlecrcBytes.Length;
         manifest.bundleIndex = manifest.bundleCollectorIndex + bundleCollectorBytes.Length;
         manifest.assetCollectorIndex = manifest.bundleIndex + bundleBytes.Length;
         manifest.assetIndex = manifest.assetCollectorIndex + assetCollectorBytes.Length;

         byte[] manifestBytes = manifest.ToBytes();

         List<byte> manifestFileBytes = new List<byte>();
         manifestFileBytes.AddRange(manifestBytes);
         manifestFileBytes.AddRange(bundlecrcBytes);
         manifestFileBytes.AddRange(bundleCollectorBytes);
         manifestFileBytes.AddRange(bundleBytes);
         manifestFileBytes.AddRange(assetCollectorBytes);
         manifestFileBytes.AddRange(assetBytes);

         // 写入这个manifest文件
         var sourceBytePath = AssetPath.AssetbundlePath + AssetPath.MANIFEST_FILE + ".bytes";
         File.WriteAllBytes(sourceBytePath, manifestFileBytes.ToArray());
         AssetBuilderLogger.Log(col, "gen manifest done with > " + manifestFileBytes.Count + " bytes");

         // build assetbundle压缩
         string assetPath = Application.dataPath + "/" + AssetPath.MANIFEST_FILE + ".bytes";
         if (File.Exists(assetPath)) File.Delete(assetPath);
         File.Move(sourceBytePath, assetPath);
         AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
         AssetBundleBuild abb = new AssetBundleBuild();
         abb.assetBundleName = AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX;
         abb.assetNames = new string[] { "Assets/" + AssetPath.MANIFEST_FILE + ".bytes" };
         BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.DisableWriteTypeTree;
         BuildPipeline.BuildAssetBundles(AssetPath.AssetbundlePath, new AssetBundleBuild[] { abb }, assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

         // cleanup
         AssetEditorHelper.CleanUnityBuildAssetBundleManifest(AssetPath.AssetbundlePath);
         if (File.Exists(assetPath)) { File.Delete(assetPath); File.Delete(assetPath + ".meta"); }
         AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
         AssetBuilderLogger.Log(col, "build manifest.ab done!");
      }

      static AssetCollector<T> ConvertAssetsToCollector<T>(IStreamAsset[] assets, Func<IStreamAsset, uint> fGetName, out byte[] bytes) where T : IStreamAsset
      {
         var collector = new AssetCollector<T>();
         using (var ms = new MemoryStream())
         {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
               foreach (var asset in assets)
               {
                  collector.names = collector.names.AddUnSafe(fGetName(asset));
                  collector.position = collector.position.AddUnSafe((int)bw.BaseStream.Position);
                  asset.ToStream(bw);
               }
               collector.streamLength = (int)bw.BaseStream.Length;
               bw.BaseStream.Position = 0;
               bytes = bw.GetBytes();
            }
         }

         return collector;
      }

      /// <summary>
      /// 根据输入的bundles的信息获取crc集合
      /// 会判断文件是否存在
      /// </summary>
      static BundleCrcCollector CollectBundleCrcsFromBundles(Bundle[] bundles)
      {
         BundleCrcCollector collector = new BundleCrcCollector();

         Dictionary<uint, string> dict = new Dictionary<uint, string>();
         var bundleFiles = AssetEditorHelper.CollectAllPath(AssetPath.AssetbundlePath, "*.ab");
         bundleFiles
            .ToObservable(Scheduler.Immediate)
            .Do(bundlePath =>
            {
               FileInfo fi = new FileInfo(bundlePath);
               //AssetBuilderLoger.Log("found and hash bundle name = " + fi.Name);
               var crc = Crc32.GetStringCRC32(fi.Name);
               dict[crc] = bundlePath;
            })
            .Subscribe();

         bundles
            .ToObservable(Scheduler.Immediate)
            .Do(bundle =>
            {
               string path = null;
               if (dict.TryGetValue(bundle.bundleNameCrc, out path))
               {
                  collector.names = collector.names.AddUnSafe(bundle.bundleNameCrc);
                  collector.crcs = collector.crcs.AddUnSafe(Crc32.GetFileCRC32(path));
               }
               else
               {
                  throw new Exception("找不到这个打包好的文件?请检查打包的IBuildAB是否对 : BundleName = " +
                     bundle.bundleNameCrc + " : " + bundle.bundleName +
                     "\n" + bundle.assets.ToArrayString());
               }
            })
            .Subscribe();

         return collector;
      }

      /// <summary>
      /// 根据assets把bundles分组出来
      /// </summary>
      static Bundle[] CollectAllBundlesFromAssets(Asset[] assets)
      {
         Dictionary<uint, Bundle> bundleDict = new Dictionary<uint, Bundle>();
         List<Bundle> list = new List<Bundle>();
         assets
            .ToObservable(Scheduler.Immediate)
            .Do(asset =>
            {
               int index = -1;
               asset
                  .buildBundleNames
                  .ToObservable(Scheduler.Immediate)
                  .Do(_ => index++)
                  .Do(bundleName =>
                  {
                     uint bundleCrc = Crc32.GetStringCRC32(bundleName);
                     Bundle bundle = null;
                     if (bundleDict.TryGetValue(bundleCrc, out bundle) == false)
                     {
                        bundle = new Bundle();
                        //AssetBuilderLoger.Log("got bundle from asset > " + bundleName + ":" + bundleCrc);
                        bundle.bundleName = bundleName;
                        bundle.bundleNameCrc = bundleCrc;
                        bundle.bundleType = asset.buildType;
                        bundle.assetType = GetAssetLoadType(asset.buildPaths[index]);
                        bundle.assets = new string[0];
                        bundleDict[bundleCrc] = bundle;
                        list.Add(bundle);
                     }

                     // 把这个资源对应的打包路径添加进去
                     bundle.assets = bundle.assets.AddSafe(asset.buildPaths[index]);

                     // 检查错误
                     if (bundle.bundleType != asset.buildType)
                     {
                        if ((int)(bundle.bundleType & asset.buildType) != (int)asset.buildType)
                           bundle.bundleType = bundle.bundleType | asset.buildType;

                        //Debug.LogException(new Exception(asset.buildBundleNames.First() + " 在同一个包:" + bundle.bundleName
                        //                    + " 但是它们打包类型不一致!请检查Relation处理里面关于GetAssetBuildMode的判断!!" + "\n" + bundle.bundleType.ToString()
                        //                    + "\n"+asset.buildPaths[index]+":"+asset.buildType.ToString()));
                     }

                     // 资源Load时候的类型
                     var loadType = GetAssetLoadType(asset.buildPaths[index]);
                     if (bundle.assetType != loadType)
                     {
                        //Debug.LogException(new Exception(asset.buildBundleNames.First() + " 在同一个包:" + bundle.bundleName
                        //                            + " 但是它们资源有不一样的加载类型!?不同类型资源不能打一块！\n"
                        //                    + "bundle: " + bundle.assetType.ToString() + " asset: " + loadType.ToString()));

                        bundle.assetType = AssetLoadType.Unknown;
                     }

                     // 把一个包关联的默认依赖的Bundle获取
                     var ci0 = GetCachInfoByAssetSourcePath(asset.sourcePathCrc);
                     foreach (var link in ci0.linkSourcePaths)
                     {
                        var linkid = Crc32.GetStringCRC32(link);
                        var ci = GetCachInfoByAssetSourcePath(linkid);
                        if (ci == null)
                        {
                           Debug.LogException(new Exception("加载不到关联bundle的cachInfo报错! source=" + ci0.sourcePath + " link=" + link));
                        }
                        else
                        {
                           if (ci.buildType == AssetBuildType.dependence)
                              bundle.dependencies = bundle.dependencies.AddSafe(ci.buildBundleNames);
                        }
                     }
                  })
                  .Subscribe();
            })
            .Subscribe();

         return list.ToArray();
      }

      /// <summary>
      /// 把缓存库中的缓存信息转成单个资源对象
      /// </summary>
      static Asset[] CollectAllAssets()
      {
         var ret = new List<Asset>();
         var cachInfos = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, "*.info");
         if (cachInfos == null)
         {
            throw new Exception("生成manifest时候CollectAllAssets在" + AssetPath.CachedAssetsPath + " 没有找到任何东西*.info !?");
         }
         else
         {
            AssetBuilderLogger.Log("找到资源info数量:" + cachInfos.Length);
         }

         foreach (var item in cachInfos)
         {
            var cachInfo = new AssetCachInfo();
            cachInfo.FromBytes(File.ReadAllBytes(item));

            Asset asset = new Asset();
            asset.sourcePathCrc = Crc32.GetStringCRC32(cachInfo.sourcePath);
            asset.buildType = cachInfo.buildType;
            asset.buildPaths = cachInfo.buildPaths;
            asset.buildBundleNames = cachInfo.buildBundleNames;

            assetToCachInfo[asset.sourcePathCrc] = cachInfo;

            // 把这个资源关联的独立资源获取
            // 用于加载这个Asset时候独立载入准备好
            foreach (var link in cachInfo.linkSourcePaths)
            {
               var linkCi = GetCachInfoByAssetSourcePath(Crc32.GetStringCRC32(link));
               if (linkCi == null)
               {
                  AssetBuilderLogger.LogError("加载不到link资源的cachInfo,这个link对象还原不成功.sourcePath=" + cachInfo.sourcePath +
                     " not found link=" + link + "  |  " + Crc32.GetStringCRC32(link));
                  continue;
               }

               if (linkCi.buildType == AssetBuildType.single)
               {
                  asset.linkSingleAssets = asset.linkSingleAssets.AddSafe(link);
               }
            }

            ret.Add(asset);
         }

         return ret.ToArray();
      }
   }
}