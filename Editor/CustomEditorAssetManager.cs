/*
 * @Author: chiuan wei 
 * @Date: 2017-06-12 16:00:10 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-06-12 16:26:35
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Loader;
using SuperMobs.Core;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AssetManager))]
public class CustomEditorAssetManager : Editor
{
   AssetManager am;
   Bundle[] loadedBundles;

   void OnEnable()
   {
      am = target as AssetManager;
   }

   public override void OnInspectorGUI()
   {
      EditorGUILayout.LabelField("当前正在异步加载数量:" + GetCurrentAsyncLoadingCount() + "/" + GetMaxAsyncLoadingLength());

      loadedBundles = am.manifest.GetBundleCollector().GetCurrentAssets();
      EditorGUILayout.LabelField("曾经加载过的Bundle数量=" + (loadedBundles != null ? loadedBundles.Length : 0));

      // 显示当前加载的对象
      int loadedCount = 0;
      foreach (var item in loadedBundles)
      {
         if (item.isLoaded == false) continue;

         int count = item.GetReferCount(true, false);
         var oldCol = GUI.color;
         if (count == 0)
         {
            GUI.color = Color.red;
         }
         string ownerInfo = count.ToString();
         if (count == int.MaxValue)
         {
            ownerInfo = "第一次加载";
         }
         else if (count == 666)
         {
            ownerInfo = "异步加载引用中";
         }
         else if (count == 888)
         {
            ownerInfo = "依赖父对象存活";
         }
         EditorGUILayout.LabelField(" >" + item.bundleName);
         EditorGUILayout.LabelField("  owner:" + ownerInfo +
            "   AB:" + (item.assetBundle != null));
         GUI.color = oldCol;
         loadedCount++;
      }

      EditorGUILayout.LabelField("当前存活的Bundle数量=" + loadedCount);

      // 全局静态资源的引用
      EditorGUILayout.Space();

      Repaint();
   }

   public int GetMaxAsyncLoadingLength()
   {
      return AssetPreference.MAX_ASYNC_LOADING_COUNT;
   }

   public int GetCurrentAsyncLoadingCount()
   {
      if (Service.Get<LoaderService>() != null)
      {
         return Service.Get<LoaderService>().GetBundleAsyncCount();
      }

      return -1;
   }
}

