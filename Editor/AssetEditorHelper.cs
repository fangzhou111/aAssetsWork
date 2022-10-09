using System.Reflection;
namespace SuperMobs.AssetManager.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using System.Linq;
    using SuperMobs.AssetManager.Core;
    using UniRx;

    public class AssetEditorHelper
    {
        #region Collect Assets

        // This method loads all files at a certain path and
        // returns a list of specific assets.
        public static List<T> CollectAll<T>(string path) where T : Object
        {
            lock (new object())
            {
                List<T> l = new List<T>();
                if (!Directory.Exists(path))
                {
                    Debug.LogError("CollectAll Error path is not exist:" + path);
                    return l;
                }

                string[] files = Directory.GetFiles(path, "*.*");

                files
                    .AsSafeEnumerable()
                    .ToObservable(Scheduler.Immediate)
                    .Subscribe(file =>
                    {
                        if (file.Contains(".meta")) return;

                        if (file.StartsWith(Application.dataPath, StringComparison.Ordinal))
                        {
                            file = file.Replace(Application.dataPath, "Assets");
                        }

                        // T asset = (T)AssetDatabase.LoadAssetAtPath(file, typeof(T));
                        var asset = AssetDatabase.LoadAssetAtPath<T>(file);
                        if (asset == null)
                        {
                            Debug.LogError("Asset is not " + typeof(T) + ": " + file);
                            return;
                        }
                        l.Add(asset);
                    });

                return l;
            }
        }

        public static List<T> CollectAllDeep<T>(string path, string pattern) where T : Object
        {
            lock (new object())
            {
                List<T> l = new List<T>();
                if (!Directory.Exists(path))
                {
                    Debug.LogError("CollectAllDeep Error path is not exist:" + path);
                    return l;
                }

                string fileEnd = string.IsNullOrEmpty(pattern) ? string.Empty : pattern.Split('.')[1];

                string[] files = new string[0];
                if (string.IsNullOrEmpty(pattern))
                    files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                else
                    files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(fileEnd.ToLower())).ToArray<string>();

                for (int i = 0; files != null && i < files.Length; i++)
                {
                    string file = files[i];
                    if (file.Contains(".meta")) continue;
                    if (!string.IsNullOrEmpty(fileEnd) && !(file.ToLower().EndsWith(fileEnd.ToLower()))) continue;

                    if (file.StartsWith(Application.dataPath, StringComparison.Ordinal))
                    {
                        file = file.Replace(Application.dataPath, "Assets");
                    }

                    var asset = AssetDatabase.LoadAssetAtPath<T>(file);
                    if (asset == null)
                    {
                        //throw new Exception("Asset is not " + typeof(T) + ": " + file);
                        Debug.LogError("Asset is not " + typeof(T) + ": " + file);
                        continue;
                    }
                    l.Add(asset);
                }
                return l;
            }
        }

        public static string[] CollectAllPath(string path, string pattern)
        {
            lock (new object())
            {
                if (!Directory.Exists(path))
                {
                    Debug.LogException(new Exception("CollectAllDeep Error path is not exist:" + path));
                    return new string[0];
                }

                string fileEnd = string.IsNullOrEmpty(pattern) ? string.Empty : pattern.Split('.').Last();

                string[] files = null;
                if (string.IsNullOrEmpty(pattern))
                    files = Directory.GetFiles(path, "*.*");
                else
                    files = Directory.GetFiles(path, pattern)
                    .Where(s => s.ToLower().EndsWith(fileEnd.ToLower(), StringComparison.Ordinal)).ToArray<string>();
                return files;
            }
        }

        /// <summary>
        /// FullPath
        /// </summary>
        public static string[] CollectAllPathDeep(string path, string pattern)
        {
            lock (new object())
            {
                if (!Directory.Exists(path))
                {
                    Debug.LogException(new Exception("CollectAllDeep Error path is not exist:" + path));
                    return new string[0];
                }

                string fileEnd = string.IsNullOrEmpty(pattern) ? string.Empty : pattern.Split('.').Last();

                string[] files = null;
                if (string.IsNullOrEmpty(pattern))
                    files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                else
                    files = Directory.GetFiles(path, pattern, SearchOption.AllDirectories)
                    .Where(s => s.ToLower().EndsWith(fileEnd.ToLower(), StringComparison.Ordinal)).ToArray<string>();

                return files;
            }
        }

        public static List<T> CollectAll<T>(string path, string pattern) where T : Object
        {
            lock (new object())
            {
                List<T> l = new List<T>();
                if (!Directory.Exists(path))
                {
                    Debug.LogError("CollectAll Error path is not exist:" + path);
                    return l;
                }

                string fileEnd = string.IsNullOrEmpty(pattern) ? string.Empty : pattern.Split('.')[1];

                string[] files = new string[0];
                if (string.IsNullOrEmpty(pattern))
                    files = Directory.GetFiles(path, "*.*");
                else
                    files = Directory.GetFiles(path, "*.*").Where(s => s.ToLower().EndsWith(fileEnd.ToLower())).ToArray<string>();

                for (int i = 0; files != null && i < files.Length; i++)
                {
                    string file = files[i];
                    if (file.Contains(".meta")) continue;
                    if (!file.ToLower().EndsWith(fileEnd)) continue;

                    if (file.StartsWith(Application.dataPath, StringComparison.Ordinal))
                    {
                        file = file.Replace(Application.dataPath, "Assets");
                    }

                    var asset = AssetDatabase.LoadAssetAtPath<T>(file);
                    if (asset == null)
                    {
                        //throw new Exception("Asset is not " + typeof(T) + ": " + file);
                        Debug.LogError("Asset is not " + typeof(T) + ": " + file + "\n" + "this type = " + asset.GetType().ToString());
                        continue;
                    }
                    l.Add(asset);
                }
                return l;
            }
        }

        #endregion

        #region Help Api

        /// <summary>
        /// 获取require的名字，用.代替/
        /// removePrefix: Assets/Lua/
        /// 例如Lua/test/ai.lua
        /// test.ai
        /// </summary>
        /// <param name="removePrefix">需要移除的路径前缀</param>
        /// <param name="asset">目标对象</param>
        /// <returns></returns>
        public static string ConvertToRequireName(string removePrefix, Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return ConvertToRequireName(removePrefix, path);
        }

        public static string ConvertToRequireName(string removePrefix, string path)
        {
            string fileEnd = path.Substring(path.LastIndexOf('.'));
            string pathTrim = string.IsNullOrEmpty(removePrefix) ? path : path.Replace(removePrefix, "");
            //pathTrim = !isLower ? pathTrim.Replace(fileEnd,"") : pathTrim.ToLower().Replace(fileEnd, "");
            pathTrim = pathTrim.Replace(fileEnd, "");
            string[] splits = pathTrim.Split('/');
            string ret = splits[0];
            for (int i = 1; splits.Length > 1 && i < splits.Length; i++)
            {
                ret += "." + splits[i];
            }
            return ret
                .Replace("@", "_")
                .Replace(" ", "_")
                .Replace("#", "_")
                .Replace("$", "_")
                .Replace("&", "_");
        }

        public static long GetRuntimeMemorySize(Object o)
        {
#if UNITY_5_6_OR_NEWER || UNITY_2017_2_OR_NEWER
			return UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(o);
#else
            return (long)Profiler.GetRuntimeMemorySize(o);
#endif
        }

        //public static int GetTextureStorageMemorySize(Object o)
        //{
        //	if (o is Texture)
        //	{
        //		var type = Types.GetType("UnityEditor.TextureUtil", "UnityEditor.dll");
        //		MethodInfo methodInfo = type.GetMethod("GetStorageMemorySize", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
        //		return (int)methodInfo.Invoke(null, new object[] { o });
        //	}
        //	else
        //	{
        //		throw new Exception("GetTextureStorageMemorySize input is not texture,just " + o.GetType().ToString());
        //	}
        //}

        public static void SetDefineSymbols(string syms)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, syms);
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, syms);
            else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, syms);
        }

        public static string GetDefineSymbols()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            else
                return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        }

        #endregion

        #region editor process

        public static void BuildTextAssetToAssetBundle(string toABPath, string fromPath)
        {
            if (File.Exists(fromPath))
            {
                BuildTextAssetToAssetBundle(toABPath, File.ReadAllBytes(fromPath));
            }
            else
            {
                throw new Exception("BuildTextAssetToAssetBundle :" + fromPath + " not exist!");
            }
        }

        public static void BuildTextAssetToAssetBundle(string toABPath, byte[] fromContent)
        {
            FileInfo fi = new FileInfo(toABPath);
            if (fi.Exists)
            {
                fi.Delete();
            }

            // build assetbundle压缩 
            string fileName = fi.Name.Substring(0, fi.Name.LastIndexOf(".", StringComparison.Ordinal));
            string assetPath = Application.dataPath + "/" +
                fileName +
                ".bytes";

            if (File.Exists(assetPath)) File.Delete(assetPath);
            File.WriteAllBytes(assetPath, fromContent);
            AssetDatabase.ImportAsset("Assets/" + fileName + ".bytes", ImportAssetOptions.ForceSynchronousImport);

            AssetBundleBuild abb = new AssetBundleBuild();
            abb.assetBundleName = fi.Name;
            abb.assetNames = new string[] { "Assets/" + fileName + ".bytes" };
            BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.DisableWriteTypeTree;
            BuildPipeline.BuildAssetBundles(fi.DirectoryName, new AssetBundleBuild[] { abb }, assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

            // cleanup 
            AssetEditorHelper.CleanUnityBuildAssetBundleManifest(fi.DirectoryName);
            if (File.Exists(assetPath)) { File.Delete(assetPath); File.Delete(assetPath + ".meta"); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// 输入一个目录，清理在这个目录打包的u5产生的manifest文件
        /// </summary>
        public static void CleanUnityBuildAssetBundleManifest(string dir)
        {
            if (Directory.Exists(dir))
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                string file = Path.Combine(dir, di.Name);
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
                if (File.Exists(file + ".manifest"))
                {
                    File.Delete(file + ".manifest");
                }

                string[] files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileName(files[i]);
                    if (!fileName.EndsWith("manifest", StringComparison.Ordinal)) continue;
                    File.Delete(files[i]);
                }
            }
        }

        #endregion

    }
}