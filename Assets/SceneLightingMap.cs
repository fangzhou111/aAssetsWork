/*
 * @Author: chiuan wei 
 * @Date: 2017-07-05 10:45:33 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-06 16:35:23
 */
using System;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SuperMobs.AssetManager.Assets
{
   /// <summary>
   /// 场景光照
   /// 负责管理场景Renderer的光照信息
   /// 负责还原光照信息
   /// 
   /// NOTE:某个光照信息通过assetPath记录，还原场景时候，通过这个光照路径加载来还原采用（兼容多个切换）
   /// </summary>
   public class SceneLightingMap : MonoBehaviour
   {
      public GameObject[] staticRenderGameObjects;
      public Renderer[] staticRenders;
      public string lightingDataPath = "";
      public bool isNeedBatchingWhenStart = true;
      public SceneLightingData currentData = null;

      /// <summary>
      /// 需要在外面加载器里面设置控制当前的加载方式
      /// </summary>
      static int _NeedABFlag = -1;
      public static bool isNeedABLoad
      {
         get
         {
            return _NeedABFlag == 1;
         }
         set
         {
            // 设置过就mark,就是外部人手设置了.
            _NeedABFlag = value ? 1 : 0;
         }
      }

      void Awake()
      {
         var b = CheckIfFromAssetBundle();

         // 当外部没有人设置这个变量,将采用自动设置
         if (_NeedABFlag == -1 && b)
         {
            isNeedABLoad = b;
         }

         if (isNeedABLoad)
         {
            AssetLogger.Log("场景光照信息将从AB里面加载.");
         }
         else
         {
            AssetLogger.Log("场景光照信息将不从AB加载,编辑器模拟方式.");
         }
      }

      GameObject[] fixStaticRenderGameObjects()
      {
         List<GameObject> list = new List<GameObject>();
         foreach(var go in staticRenderGameObjects)
         {
            if(go != null)
            {
               list.Add(go);
            }
         }

         return list.ToArray();
      }

      void Start()
      {
         var light = SetActiveLighting(lightingDataPath);

         AssetLogger.Log(Color.green, "设置光照 FromAssetBundle = " + isNeedABLoad + "  :  lightData = " + (light != null));

         if (light != null &&
            isNeedBatchingWhenStart &&
            staticRenderGameObjects != null && staticRenderGameObjects.Length > 0)
         {
            DateTime d0 = DateTime.Now;

             
            StaticBatchingUtility.Combine(fixStaticRenderGameObjects(), gameObject);

            AssetLogger.Log(Color.grey, "StaticBatching Cost " + (DateTime.Now - d0).TotalSeconds);
         }
      }

      public SceneLightingData SetActiveLighting(string assetPath)
      {
         SceneLightingData light = LoadLightingData(assetPath);
         currentData = light;

         if (light != null)
         {
            light.Apply(staticRenders);
         }
         return light;
      }

      // load from asset manager
      SceneLightingData LoadLightingData(string assetPath)
      {
         if (isNeedABLoad == false)
         {
#if UNITY_EDITOR
            // 可能Editor模式下prefab方式载入
            // 如果是FromAB的情况 
            if (CheckIfFromAssetBundle())
            {
               return AssetDatabase.LoadAssetAtPath<SceneLightingData>(assetPath);
            }
#endif
            return null;
         }

         if (AssetManager.Instance.Exist(assetPath) == false)
         {
            AssetLogger.LogWarning("这个场景没有找到光照数据:" + assetPath);
            return null;
         }

         var asset = AssetManager.Instance.Load(assetPath);
         if (asset == null) return null;
         var light = asset.Require<SceneLightingData>(this.gameObject);
         if (light == null)
         {
            AssetLogger.LogError(assetPath + "存在当时载不出来!");
         }

         return light;
      }

      bool CheckIfFromAssetBundle()
      {
         var scene = SceneManager.GetActiveScene();
         if (scene.buildIndex == -1)
         {
            return true;
         }
         else
         {
            // the lighting data maybe cached in prefab. if not steam scene build
            if (this.transform.root != this.transform)
            {
               return true;
            }
            return false;
         }
      }

   }
}