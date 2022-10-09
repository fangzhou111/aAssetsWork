/*
 * @Author: chiuan wei 
 * @Date: 2017-04-10 14:23:05 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-06-12 16:51:04
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Loader;
using SuperMobs.AssetManager.Package;
using SuperMobs.AssetManager.Update;
using SuperMobs.Core;
using UniRx;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
   public class MenuItems
   {
      const int priority = 99999;

      #region Editor Callbacks

      [OnOpenAssetAttribute(1)]
      static bool OpenLogAllCachInfos(int instanceID, int line)
      {
         //string appPath = Path.GetDirectoryName(Application.dataPath);
         //var selected = EditorUtility.InstanceIDToObject(instanceID);
         //var assetFilePath = Path.Combine(appPath, AssetDatabase.GetAssetPath(selected));
         // Debug.Log("Open Asset step: 2 (" + assetFilePath + ") " + line);
         return false; // we did not handle the open
      }

      #endregion

      [MenuItem("SuperMobs/Utils/Output Update Tip Sample", false, priority)]
      static void OutputUpdateTipSample()
      {
         UpdateTip tip = new UpdateTip();
         tip.Save();
      }

      [MenuItem("SuperMobs/AssetManager/CachService/LogAllCachInfos")]
      static void GenCollectAllCachInfo()
      {
         // check if the cach folder exist.
         if (!Directory.Exists(AssetPath.CachedAssetsPath))
         {
            Debug.LogError("cant found the cachInfo folder > " + AssetPath.CachedAssetsPath);
            return;
         }

         AssetCachInfoCollection collection = new AssetCachInfoCollection();
         List<AssetCachInfo> infoList = new List<AssetCachInfo>();

         string namecontents = "";
         var files = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, "*.info");
         files
         .ToObservable(Scheduler.Immediate)
         .Do(file =>
         {
            AssetCachInfo info = new AssetCachInfo();
            info.FromBytes(File.ReadAllBytes(file));
            infoList.Add(info);

            namecontents += ("\n" + info.sourcePath + " : " + info.sourceCrc + " : " + file);

         })
         .Subscribe();

         collection.infos = infoList.ToArray();

         string path = AssetPath.ProjectRoot + "cachcollection.json";
         collection.Save(path);

         string nPath = path + ".txt";
         if (File.Exists(nPath)) File.Delete(nPath);
         File.WriteAllText(nPath, namecontents);

         Debug.Log("done > " + path);

         string manifestPath = AssetPath.AssetbundlePath + AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX;
         if (File.Exists(manifestPath))
         {
            AssetBundle ab = AssetBundle.LoadFromFile(manifestPath);
            var bytes = (ab.LoadAllAssets()[0] as TextAsset).bytes;
            var _manifest = new AssetManifest();
            _manifest.FromStreamBytes(bytes);
            _manifest.InitContent(bytes);
            ab.Unload(false);

            var bundles = _manifest.GetBundles();
            AssetBundleCollection bs = new AssetBundleCollection();
            bs.bundles = new Bundle[bundles.Count];
            bundles.CopyTo(bs.bundles, 0);
            Debug.Log("bundles count = " + bundles.Count);
            string path2 = AssetPath.ProjectRoot + "bundlecollection.json";
            bs.Save(path2);
         }
      }

      [MenuItem("SuperMobs/AssetManager/CachService/Delete All Builded")]
      static void DeleteAllCachBuilds()
      {
         // check if the cach folder exist.
         if (!Directory.Exists(AssetPath.CachedAssetsPath))
         {
            Debug.LogError("cant found the cachInfo folder > " + AssetPath.CachedAssetsPath);
            return;
         }

         var files = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, "*.kind");
         var kinds = new List<string>();
         foreach (var file in files)
         {
            FileInfo fi = new FileInfo(file);
            kinds.Add(fi.Name.Substring(0, fi.Name.LastIndexOf(".", StringComparison.OrdinalIgnoreCase)));
         }

         AssetBuilder builder = new AssetBuilder(null, null);
         builder.ExecuteClean(kinds.ToArray());
      }

      [MenuItem("SuperMobs/AssetManager/Package/NewServerPackage")]
      static void GenNewServerPackage()
      {
         var sb = ShellBuilder.CreateWithSetting();

         Packager pk = new Packager();
         pk.platform = AssetPath.GetBuildTargetPlatform();
         pk.sdk = sb.sdk;

         pk.GenNewServerPackage();
      }

      [MenuItem("SuperMobs/AssetManager/Package/NewClientPackage")]
      static void GenNewClientPackage()
      {
         var sb = ShellBuilder.CreateWithSetting();

         Packager pk = new Packager();
         pk.platform = AssetPath.GetBuildTargetPlatform();
         pk.sdk = sb.sdk;

         pk.GenNewServerPackage();
         pk.GenNewClientPackage();
      }

      [MenuItem("SuperMobs/AssetManager/Package/ReadyToBuildPlayer")]
      static void GenReadyPackage()
      {
         ShellBuilder.ReadyToBuild();
      }

      [MenuItem("SuperMobs/AssetManager/Player/BuildPlayer")]
      public static void BuildPlayer()
      {
         ShellBuilder.BuildPlayer();
      }

      #region Assets MenuItem

      [MenuItem("Assets/AssetManager/OutputPackage", false, priority)]
      static void ExportPackage()
      {
         if (Selection.objects == null) return;
         List<string> paths = new List<string>();
         foreach (UnityEngine.Object o in Selection.objects)
         {
            paths.Add(AssetDatabase.GetAssetPath(o));
         }

         string savePath = EditorUtility.SaveFilePanel("导出.unitypackage", AssetPath.ProjectRoot, "xxx.unitypackage", ".unitypackage");

         AssetDatabase.ExportPackage(paths.ToArray(), savePath, ExportPackageOptions.IncludeDependencies);
         AssetDatabase.Refresh();
         Debug.Log("ExportPackage:" + savePath);
         EditorUtility.RevealInFinder(savePath);
      }

      [MenuItem("Assets/AssetManager/BuildAssetBundles", false)]
      static void BuildAssetBundles()
      {
         BuildPipeline.BuildAssetBundles(AssetPath.AssetbundlePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
      }

      [MenuItem("Assets/AssetManager/Build Selected Asset", false, priority)]
      static void BuildSelectedAssetBundles()
      {
         if (Selection.activeObject != null)
         {
            AssetBundleBuild abb = new AssetBundleBuild();
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            abb.assetBundleName = AssetEditorHelper.ConvertToRequireName("", assetPath) + AssetPath.ASSETBUNDLE_SUFFIX;
            abb.assetNames = new string[] { assetPath };

            string output = AssetPath.AssetbundlePath;
            if (!Directory.Exists(output))
            {
               Directory.CreateDirectory(output);
            }

            BuildPipeline.BuildAssetBundles(
               output,
               new AssetBundleBuild[] { abb },
               BuildAssetBundleOptions.None,
               EditorUserBuildSettings.activeBuildTarget);

            // 直接打开目录
            EditorUtility.RevealInFinder(output);
         }
         else
         {
            Debug.LogError("选择的资源类型不能输出ab:" + AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
         }
      }

      [MenuItem("Assets/AssetManager/Log Dependencies", false, priority)]
      static void LogDependencies()
      {
         if (Selection.activeObject != null)
         {
            string[] depens = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(Selection.activeObject));
            Debug.Log("all dep : \n" + depens.ToArrayString());
            Dictionary<string, List<string>> sort = new Dictionary<string, List<string>>();
            foreach (var dep in depens)
            {
               FileInfo fi = new FileInfo(dep);
               if (sort.ContainsKey(fi.Extension.ToLower()) == false)
               {
                  sort.Add(fi.Extension.ToLower(), new List<string>());
               }

               sort[fi.Extension.ToLower()].Add(dep);
            }

            foreach (var item in sort)
            {
               string str = item.Key + " count = " + item.Value.Count;
               foreach (var d in item.Value)
               {
                  str += ("\n    " + d);
               }
               Debug.Log(str);
            }
         }
      }

      [MenuItem("Assets/AssetManager/Log AssetType", false, priority)]
      static void DebugAssetType()
      {
         Debug.Log(Selection.activeObject);
      }

      [MenuItem("Assets/AssetManager/Log Meta", false, priority)]
      static void DebugAssetMeta()
      {
         Debug.Log(MetaEditor.GetAssetMetaCrc(AssetDatabase.GetAssetPath(Selection.activeObject)));
      }

      [MenuItem("Assets/AssetManager/Log AssetPath", false, priority)]
      static void DebugAssetPath()
      {
         Debug.Log(AssetDatabase.GetAssetPath(Selection.activeObject) + "\nname=" + Selection.activeObject.name + "\ntype=" + Selection.activeObject +
            "\nguid=" + Selection.assetGUIDs[0]);
      }

      [MenuItem("Assets/AssetManager/Log MemorySize", false, priority)]
      static void DebugObjectSize()
      {
         if (Selection.activeObject == null) return;

         Debug.Log("内存占用：" + EditorUtility.FormatBytes(AssetEditorHelper.GetRuntimeMemorySize(Selection.activeObject)));
         //Debug.Log("硬盘占用：" + EditorUtility.FormatBytes(AssetEditorHelper.GetTextureStorageMemorySize(Selection.activeObject)));
      }

      [MenuItem("Assets/AssetManager/Log BuildCachInfo", false, priority)]
      static void DebugBuildCachInfo()
      {
         if (Selection.activeObject == null) return;

         var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
         string crc = Crc32.GetStringCRC32(assetPath).ToString();
         var cachs = AssetEditorHelper.CollectAllPath(AssetPath.CachedAssetsPath, crc + "*.info");
         Debug.Log(assetPath + " 打包缓存信息有:");
         Debug.Log(cachs.ToArrayString());
         foreach (var item in cachs)
         {
            AssetCachInfo ci = new AssetCachInfo();
            ci.FromBytes(File.ReadAllBytes(item));
            Debug.Log(ci.ToJson());
         }
      }

      [MenuItem("Assets/AssetManager/Clone Mesh", false, priority)]
      static void CloneMesh()
      {
         if (Selection.activeObject is Mesh)
         {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var obj = Selection.activeObject;
            path = path + obj.name + ".asset";

            FileInfo fi = new FileInfo(path);
            if (fi.Exists) fi.Delete();

            AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(obj), path);
            AssetDatabase.ImportAsset(path);
         }
      }

      #endregion

      #region fix DLL is in timestamps but is not known in guidmapper

      /// <summary>
      /// Menu item to manually handle the dreaded "DLL is in timestamps but is not known in guidmapper..." errors that 
      /// pop up from time to time.
      /// </summary>
      /// <remarks>
      /// Adapted from http://forum.unity3d.com/threads/unityengine-ui-dll-is-in-timestamps-but-is-not-known-in-assetdatabase.274492/
      /// </remarks>
      [MenuItem("Assets/AssetManager/Dlls/Reimport Unity Extension Assemblies", false, 100)]
      public static void ReimportUnityExtensionAssemblies()
      {
         // Locate the directory of Unity extensions
         string extensionsPath = System.IO.Path.Combine(EditorApplication.applicationContentsPath, "UnityExtensions");

         // Walk the directory tree, looking for DLLs
         var dllPaths = System.IO.Directory.GetFiles(extensionsPath, "*.dll", System.IO.SearchOption.AllDirectories);

         // Reimport any extension DLLs
         int numReimportedAssemblies = 0;
         foreach (string dllPath in dllPaths)
         {
            //UnityEngine.Debug.LogFormat("Reimport DLL: {0}", dllPath);
            if (ReimportExtensionAssembly(dllPath))
            {
               numReimportedAssemblies++;
            }
         }

         UnityEngine.Debug.LogWarningFormat("Reimported ({0}) Unity extension DLLs." +
            " Please restart Unity for the changes to take effect.", numReimportedAssemblies);
      }

      private static bool ReimportExtensionAssembly(string dllPath)
      {

         // Check to see if this assembly exists in the asset database
         string assemblyAssetID = AssetDatabase.AssetPathToGUID(dllPath);
         if (!string.IsNullOrEmpty(assemblyAssetID))
         {
            // Assembly exists in asset database, so force a reimport
            AssetDatabase.ImportAsset(dllPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
            return true;
         }
         return false;
      }

      #endregion

      #region 检验静态资源引用

      [MenuItem("SuperMobs/Utils/Static Assets Ref Check(Runtime)",false,101)]
      static void StaticRef()
      {
         //静态引用
         CheckStaticAssetLoadAssembly("Assembly-CSharp-firstpass");
         CheckStaticAssetLoadAssembly("Assembly-CSharp");
         CheckStaticAssetLoadAssembly("AssetManager");
      }

      static void CheckStaticAssetLoadAssembly(string name)
      {
         Assembly assembly = null;
         try
         {
            assembly = Assembly.Load(name);
         }
         catch (Exception ex)
         {
            Debug.LogWarning(ex.Message);
         }
         finally
         {
            if (assembly != null)
            {
               foreach (Type type in assembly.GetTypes())
               {
                  try
                  {
                     HashSet<string> assetPaths = new HashSet<string>();
                     FieldInfo[] listFieldInfo = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                     foreach (FieldInfo fieldInfo in listFieldInfo)
                     {
                        if (!fieldInfo.FieldType.IsValueType)
                        {
                           SearchStaticAssetProperties(fieldInfo.GetValue(null), assetPaths);
                        }
                     }
                     if (assetPaths.Count > 0)
                     {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat("{0}.cs\n", type.ToString());
                        foreach (string path in assetPaths)
                        {
                           sb.AppendFormat("\t{0}\n", path);
                        }
                        Debug.LogError(sb.ToString());
                     }

                  }
                  catch (Exception ex)
                  {
                     Debug.LogWarning(ex.Message);
                  }
               }
            }
         }
      }

      static HashSet<string> SearchStaticAssetProperties(object obj, HashSet<string> assetPaths)
      {
         if (obj != null)
         {
            if (obj is UnityEngine.Object)
            {
               UnityEngine.Object[] depen = EditorUtility.CollectDependencies(new UnityEngine.Object[] { obj as UnityEngine.Object });
               foreach (var item in depen)
               {
                  string assetPath = AssetDatabase.GetAssetPath(item);
                  if (!string.IsNullOrEmpty(assetPath))
                  {
                     if (!assetPaths.Contains(assetPath))
                     {
                        assetPaths.Add(assetPath);
                     }
                  }
               }
            }
            else if (obj is IEnumerable)
            {
               foreach (object child in (obj as IEnumerable))
               {
                  SearchStaticAssetProperties(child, assetPaths);
               }
            }
            else if (obj is System.Object)
            {
               if (!obj.GetType().IsValueType)
               {
                  FieldInfo[] fieldInfos = obj.GetType().GetFields();
                  foreach (FieldInfo fieldInfo in fieldInfos)
                  {
                     object o = fieldInfo.GetValue(obj);
                     if (o != obj)
                     {
                        SearchStaticAssetProperties(fieldInfo.GetValue(obj), assetPaths);
                     }
                  }
               }
            }
         }
         return assetPaths;
      }

      #endregion

   }
}