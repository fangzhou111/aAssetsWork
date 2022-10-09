/*
 * @Author: chiuan wei 
 * @Date: 2017-11-21 02:00:12 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-11-21 02:00:39
 */
using System;
using System.Collections.Generic;
using System.IO;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using SuperMobs.Core;
using UniRx;
using UnityEngine;

namespace SuperMobs.AssetManager.Loader
{
    internal class BigFileManifestLoader : SingletonLoader<BigFileManifestLoader, BigFileManifest>
    {
        /// <summary>
        /// 开始加载
        /// </summary>
        public override object Load(string _)
        {
            AssetBundle ab = null;

            // 大文件默认在app里面
            string path = AssetPath.GetPathInAPP(AssetPath.BIG_FILE_MANIFEST);
            Debug.Log(path);
            if (File.Exists(path))
            {
                // note: 大文件这个没有加密
                ab = AssetBundle.LoadFromFile(path,0,AssetPreference.GetAssetBundleOffset());
            }
            else
            {
                // 如果是安卓
                //#if UNITY_ANDROID
                //    ab = AssetBundle.LoadFromFile(path,0,AssetPreference.GetAssetBundleOffset());
                //#else
                //    AssetLogger.Log(Color.yellow, path + "大文件manifest不存在!");
                //#endif
            }

            if (ab != null)
            {
                var text = ab.LoadAllAssets() [0] as TextAsset;
                var bytes = text != null ? text.bytes : null;
                if (bytes == null)
                {
                    ab.Unload(false);
                    AssetLogger.LogException("Cant load bigfile manifest,ab is ok,but cant load asset.");
                    return null;
                }

                var _manifest = new BigFileManifest();
                _manifest.FromStreamBytes(bytes);
                ab.Unload(false);
                return _manifest;
            }
            else
            {
                AssetLogger.Log(Color.yellow, ">>不是大文件加载方式!");
                return null;
            }
        }
    }
}