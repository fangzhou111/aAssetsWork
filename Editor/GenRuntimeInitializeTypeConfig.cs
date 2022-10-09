using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SuperMobs.Core.Editor
{
   class GenRuntimeInitializeTypeConfig : AssetPostprocessor
   {
      static void EditorApplication_DelayCall()
      {
         EditorApplication.delayCall -= EditorApplication_DelayCall;
         RefreshConfig();
      }

      const string CONFIG_FOLDER = "Assets/Resources/";

      static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
      {
         if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

         string config = CONFIG_FOLDER + RuntimeInitializeTypes.SAVE_ASSET_NAME + ".asset";
         foreach (string str in deletedAssets)
         {
            if (str.Equals(config, StringComparison.OrdinalIgnoreCase))
            {
               DelayRefreshConfig();
               return;
            }
         }
      }

      static void DelayRefreshConfig()
      {
         EditorApplication.delayCall += EditorApplication_DelayCall;
      }

      [DidReloadScripts]
      static void RefreshConfig()
      {
         if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

         // 收集所有的可能包含启动内容的脚本
         List<Type> allRuntimeTypes = new List<Type>();
         foreach (string path in AssetDatabase.GetAllAssetPaths())
         {
            if (!path.EndsWith(".dll"))
               continue;

            try { allRuntimeTypes.AddRange(Assembly.LoadFile(path).GetTypes()); } catch { }
         }
         try { allRuntimeTypes.AddRange(Assembly.Load("Assembly-CSharp").GetTypes()); } catch { }
         try { allRuntimeTypes.AddRange(Assembly.Load("Assembly-CSharp-Editor").GetTypes()); } catch { }

         // check same func in type
         Func<Dictionary<int, List<RuntimeInitializeTypes.FunctionLocation>>, string, string, string, bool> checkFunc = (dict, asm, fullname, name) =>
         {
            foreach (var list in dict.Values)
            {
               foreach (var func in list)
               {
                  // t.Assembly.FullName, t.FullName, method.Name
                  if (func.assembly == asm && func.type == fullname && func.func == name)
                  {
                     return true;
                  }
               }
            }
            return false;
         };

         // 查找初始化的方法
         Dictionary<RuntimeInitializeType, Dictionary<int, List<RuntimeInitializeTypes.FunctionLocation>>> config = new Dictionary<RuntimeInitializeType, Dictionary<int, List<RuntimeInitializeTypes.FunctionLocation>>>();
         foreach (Type t in allRuntimeTypes)
         {
            foreach (MethodInfo method in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
               object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitialize), true);
               if (attributes.Length == 0)
                  continue;

               RuntimeInitialize attribute = attributes[0] as RuntimeInitialize;

               if (!config.ContainsKey(attribute.initType))
                  config.Add(attribute.initType, new Dictionary<int, List<RuntimeInitializeTypes.FunctionLocation>>());

               if (!config[attribute.initType].ContainsKey(attribute.order))
                  config[attribute.initType].Add(attribute.order, new List<RuntimeInitializeTypes.FunctionLocation>());

               // check if the same type same function
               // dont add again...
               if (checkFunc(config[attribute.initType], t.Assembly.FullName, t.FullName, method.Name))
               {
                  continue;
               }

               config[attribute.initType][attribute.order].Add(new RuntimeInitializeTypes.FunctionLocation(t.Assembly.FullName, t.FullName, method.Name));
            }
         }

         // 创建ScriptableObject文件
         if (RuntimeInitializeTypes.instance == null)
         {
            if (!Directory.Exists(CONFIG_FOLDER)) Directory.CreateDirectory(CONFIG_FOLDER);
            RuntimeInitializeTypes asset = ScriptableObject.CreateInstance<RuntimeInitializeTypes>();
            AssetDatabase.CreateAsset(asset, CONFIG_FOLDER + RuntimeInitializeTypes.SAVE_ASSET_NAME + ".asset");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            RuntimeInitializeTypes.ReInit();
         }

         // for editor load ?
         if (RuntimeInitializeTypes.instance == null)
         {
            var so = AssetDatabase.LoadAssetAtPath<RuntimeInitializeTypes>(CONFIG_FOLDER + RuntimeInitializeTypes.SAVE_ASSET_NAME + ".asset");
            RuntimeInitializeTypes.Init(so);
            Debug.LogWarning(CONFIG_FOLDER + RuntimeInitializeTypes.SAVE_ASSET_NAME + ".asset" + " LoadAssetAtPath = " + so);
         }

         if (RuntimeInitializeTypes.instance == null)
         {
            Debug.LogWarning(CONFIG_FOLDER + RuntimeInitializeTypes.SAVE_ASSET_NAME + ".asset" + " not ready!");
            DelayRefreshConfig();
            return;
         }

         // 记录原有的禁用状态
         List<string> disEnabledFunctions = new List<string>();
         foreach (var location in RuntimeInitializeTypes.instance.beforeSceneLoad)
            if (!location.enable)
               disEnabledFunctions.Add(location.type + ":" + location.func);
         foreach (var location in RuntimeInitializeTypes.instance.afterSceneLoad)
            if (!location.enable)
               disEnabledFunctions.Add(location.type + ":" + location.func);
         foreach (var location in RuntimeInitializeTypes.instance.afterAssetsUpdated)
            if (!location.enable)
               disEnabledFunctions.Add(location.type + ":" + location.func);
         foreach (var location in RuntimeInitializeTypes.instance.onRestart)
            if (!location.enable)
               disEnabledFunctions.Add(location.type + ":" + location.func);

         // 生成新配置
         Dictionary<RuntimeInitializeType, List<RuntimeInitializeTypes.FunctionLocation>> result = new Dictionary<RuntimeInitializeType, List<RuntimeInitializeTypes.FunctionLocation>>();
         foreach (KeyValuePair<RuntimeInitializeType, Dictionary<int, List<RuntimeInitializeTypes.FunctionLocation>>> pair in config)
         {
            List<int> keys = new List<int>(pair.Value.Keys);
            keys.Sort();
            result.Add(pair.Key, new List<RuntimeInitializeTypes.FunctionLocation>());
            foreach (int key in keys)
            {
               for (int i = 0; i < pair.Value[key].Count; i++)
               {
                  var location = pair.Value[key][i];
                  if (disEnabledFunctions.Contains(location.type + ":" + location.func))
                     location.enable = false;
                  result[pair.Key].Add(location);
               }
            }
         }

         // 保存配置
         RuntimeInitializeTypes.instance.beforeSceneLoad = result.ContainsKey(RuntimeInitializeType.BeforeSceneLoad) ? result[RuntimeInitializeType.BeforeSceneLoad].ToArray() : new RuntimeInitializeTypes.FunctionLocation[0];
         RuntimeInitializeTypes.instance.afterSceneLoad = result.ContainsKey(RuntimeInitializeType.AfterSceneLoad) ? result[RuntimeInitializeType.AfterSceneLoad].ToArray() : new RuntimeInitializeTypes.FunctionLocation[0];
         RuntimeInitializeTypes.instance.afterAssetsUpdated = result.ContainsKey(RuntimeInitializeType.AfterAssetsUpdated) ? result[RuntimeInitializeType.AfterAssetsUpdated].ToArray() : new RuntimeInitializeTypes.FunctionLocation[0];
         RuntimeInitializeTypes.instance.onRestart = result.ContainsKey(RuntimeInitializeType.OnRestart) ? result[RuntimeInitializeType.OnRestart].ToArray() : new RuntimeInitializeTypes.FunctionLocation[0];

         EditorUtility.SetDirty(RuntimeInitializeTypes.instance);
         AssetDatabase.SaveAssets();
         AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
      }
   }
}