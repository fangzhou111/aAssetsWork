/*
 * @Author: chiuan wei 
 * @Date: 2017-07-05 18:06:06 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-11-21 02:01:23
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
    /*
     * 同步用LoadImmediate
     * 异步用Load
     * */

    internal class BigFileAssetBundleLoader : SingletonLoader<BigFileAssetBundleLoader, AssetBundle>
    {
        string bigFilePath = string.Empty;
        FileStream _fs = null;
        private BigFileManifest _bigManifest;

        internal override void Init()
        {
            bigFilePath = AssetPath.GetPathInAPP(AssetPath.BIG_FILE);
            _bigManifest = Service.Get<LoaderService>().GetBigFileManifest();
        }

        FileStream fileStream
        {
            get
            {
                if (_fs == null)
                {
                    _fs = new FileStream(bigFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }

                return _fs;
            }
        }

        byte[] LoadBytesContent(BigFileInfo info)
        {
            if (Application.isMobilePlatform && Application.platform == RuntimePlatform.Android)
            {
                throw new Exception("cant load big file bytes immediate in android.");
            }

            //TTDebuger.Log("[LoadContent] Read fileID = " + info.id + " , beginIndex = " + info.beginIndex + " , length = " + info.length);

            int i = 0;
            while (i < 5)
            {
                try
                {
                    byte[] result = new byte[info.length - (int)AssetPreference.GetAssetBundleOffset()];
                    fileStream.Seek((int) info.beginIndex + (int)AssetPreference.GetAssetBundleOffset(), SeekOrigin.Begin);
                    int readed = 0;
                    int n = 0;
                    while ((n = fileStream.Read(result, readed, result.Length - readed)) > 0)
                    {
                        readed += n;
                    }
                    if (n < 0)
                        AssetLogger.LogError("[BigFile] Read error, code = " + n + " , fileID = " + info.id);

                    return result;
                }
                catch (Exception e)
                {
                    AssetLogger.LogError("[BigFile] LoadContent Exception:" + e.Message + "\n at try time = " + i);
                }
                i++;
            }

            return null;
        }

        byte[] LoadContent(uint fileID)
        {
            BigFileInfo info = _bigManifest.GetFileInfo(fileID);
            if (info != null)
            {
                return LoadBytesContent(info);
            }
            else
            {
                AssetLogger.LogError("[BigFile] :" + fileID + "cant found big file info.");
                return null;
            }
        }

        AssetBundle LoadAssetBundle(uint fileID)
        {
            BigFileInfo info = _bigManifest.GetFileInfo(fileID);
            if (info != null)
            {
                //TTDebuger.Log("大文件加载 LoadAssetBundle > " + info.ToString());
                // note : 忘记是不是ios有问题
                AssetBundle ab = AssetBundle.LoadFromFile(bigFilePath, 0, info.beginIndex + AssetPreference.GetAssetBundleOffset());
                return ab;

                // load from memory
                // byte[] content = LoadBytesContent(info);
                // AssetBundle ab = AssetBundle.LoadFromMemory(content);
                // return ab;
            }
            else
            {
                AssetLogger.LogError("[BigFile] LoadAssetBundle:" + fileID + "找不到信息.");
                return null;
            }
        }

        AssetBundleCreateRequest LoadAssetBundleAsync(uint fileID)
        {
            BigFileInfo info = _bigManifest.GetFileInfo(fileID);
            if (info != null)
            {
                AssetLogger.Log("大文件异步加载文件大小:" + (info.length / (1024 * 1024)).ToString("F") + "mb");
                // 原始单个文件方式加载
                AssetBundleCreateRequest abr = AssetBundle.LoadFromFileAsync(bigFilePath, 0, info.beginIndex + AssetPreference.GetAssetBundleOffset());
                return abr;

                // load from memory
                //byte[] content = LoadBytesContent(info);
                //AssetBundleCreateRequest abr = AssetBundle.LoadFromMemoryAsync(content);
                //return abr;
            }
            else
            {
                AssetLogger.LogError("大文件加载LoadAssetBundleAsync:" + fileID + "找不到信息.");
                return null;
            }
        }

        public override object LoadImmediate(string fileName)
        {
            return LoadAssetBundle(Crc32.GetStringCRC32(fileName));
        }

        public override object Load(string fileName)
        {
            return LoadAssetBundleAsync(Crc32.GetStringCRC32(fileName));
        }
    }
}