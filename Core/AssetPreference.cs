/*
 * @Author: chiuan wei 
 * @Date: 2017-07-13 10:53:18 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-11-21 02:23:08
 */
using System;
using SuperMobs.AssetManager.Loader;
using UnityEngine;

namespace SuperMobs.AssetManager.Core
{
    /// <summary>
    /// 资源系统的常用配置信息
    /// </summary>
    public class AssetPreference
    {

#if SIM_PLAY
        public static bool simulate = true;
#else
        public static bool simulate = false;
#endif

        // public const int LOAD_TIME_OUT = 60;
        public const string PLATFORM_IOS = "ios";
        public const string PLATFORM_ANDROID = "android";
        public const string PLATFORM_STANDARD = "standard";

        // 异步同时加载执行数量 
#if UNITY_IOS
        public static int MAX_ASYNC_LOADING_COUNT = 30;
#elif UNITY_ANDROID
        public static int MAX_ASYNC_LOADING_COUNT = 30;
#else
        public static int MAX_ASYNC_LOADING_COUNT = 20;
#endif

        /// <summary>
        /// 是否编辑器下开启了TT_PLAY真机宏模拟真机加载方式
        /// </summary>
        public static bool isEditorAndNotSimulate
        {
            get
            {
                //#if UNITY_EDITOR
                return Application.isPlaying == false || (Application.isEditor && simulate == false);
                //#else
                //return false;
                //#endif
            }
        }

        /// <summary>
        /// 提供给SceneLightingEditor自动生成LigtingMap在场景保存的时候
        /// </summary>
        public static bool LIGHTING_DATA_AUTO_SAVE = true;

        /// <summary>
        /// 是否开启自动优化分辨率,提高性能
        /// </summary>
        public static bool AUTO_FIX_RESOLUTION = false;

        /// <summary>
        /// 是否加密ab
        /// 如果打包时候设置这个为true
        /// 那么,运行时也需要第一时间设置这个
        /// </summary>
        public static bool ENCRYPT_AB = true;

        public static ulong GetAssetBundleOffset()
        {
            if(isEditorAndNotSimulate) return 0;

            if(ENCRYPT_AB) return (ulong)10;
            else return 0;
        }

        public static byte[] ConvertAssetBundleContent(byte[] content)
        {
            if(ENCRYPT_AB)
            {
                var news = new byte[content.Length - (int)GetAssetBundleOffset()];
                for (int i = 0; i < news.Length; i++)
                {
                    news[i] = content[i + (int) GetAssetBundleOffset()];
                }
                return news;
            }
            else
            {
                return content;
            }
        }

        // 获取加载器异步载入bundle时候的超时时间
        public static int GetLoaderTimeout()
        {
#if UNITY_EDITOR
            return 30;
#elif UNITY_ANDROID
            return 60;
#elif UNITY_IOS
            return 60;
#else
            return 30;
#endif	
        }

    }
}