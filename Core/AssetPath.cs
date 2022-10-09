/*
 * @Author: chiuan wei 
 * @Date: 2017-05-23 15:03:49 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-05 21:57:22
 */
namespace SuperMobs.AssetManager.Core
{
    using System.Collections;
    using System.IO;
    using System;
    using SuperMobs.Core;
    using UnityEngine;

    // editor api bind.
    public interface AssetPathEditorAPI
    {
        string Platform { get; }
        string AssetbundlePath { get; }
        string CachedAssetsPath { get; }
        string AssetBundleEditorDevicePath { get; }
    }

    public static class AssetPath
    {
        public const string ASSETBUNDLE_SUFFIX = ".ab";

        // 包里面的文件夹
        // 下载更新的文件夹(有效用于ios用户文件夹云同步过滤掉)
        public const string ASSETBUNDLE_DEVICE_FOLDER = "res";

        // 记录这个游戏所有文件的信息
        public const string MANIFEST_FILE = "manifest";

        // 大文件
        public const string BIG_FILE = "supermobs.big";

        // 大文件信息
        public const string BIG_FILE_MANIFEST = "supermobs.big.manifest";

        // 记录每次版本发布的bundles信息
        // 就是记录这个包有多少文件，以及文件的校验码等信息
        // 所有文件包括AssetManifest文件哦!
        public const string PACKAGE_MANIFEST_FILE = "package";

        // 记录这个包的版本信息情况
        // 服务器版本 & 客户端都应该有一份负责第一时间校验
        public const string PACKAGE_VERSION_FILE = "package.version";

        public static AssetPathEditorAPI editorApi;

        private static string mDirectorySeparatorChar = string.Empty;
        public static string DirectorySeparatorChar
        {
            get
            {
                if (string.IsNullOrEmpty(mDirectorySeparatorChar))
                {
                    if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                        mDirectorySeparatorChar = "/";
                    else
                        mDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
                }
                return mDirectorySeparatorChar;
            }
        }

        public static string GetBuildTargetPlatform()
        {
            if (editorApi != null)
            {
                return editorApi.Platform;
            }
            else
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        return AssetPreference.PLATFORM_ANDROID;
                    case RuntimePlatform.IPhonePlayer:
                        return AssetPreference.PLATFORM_IOS;
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        return AssetPreference.PLATFORM_STANDARD;
                    default:
                        return string.Empty;
                }
            }
        }

        private static string mPersistentDataPath = string.Empty;
        public static string persistentDataPath
        {
            get
            {
                if (string.IsNullOrEmpty(mPersistentDataPath))
                {
                    if (Application.isEditor || Application.platform != RuntimePlatform.WindowsPlayer)
                    {
                        if (Application.platform == RuntimePlatform.Android)
                        {
#if UNITY_ANDROID && !UNITY_EDITOR
                            try
                            {

                                IntPtr obj_context = AndroidJNI.FindClass("android/content/ContextWrapper");
                                IntPtr method_getFilesDir = AndroidJNIHelper.GetMethodID(obj_context, "getFilesDir", "()Ljava/io/File;");

                                using (AndroidJavaClass cls_UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                                {
                                    using (AndroidJavaObject obj_Activity = cls_UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                                    {
                                        IntPtr file = AndroidJNI.CallObjectMethod(obj_Activity.GetRawObject(), method_getFilesDir, new jvalue[0]);
                                        IntPtr obj_file = AndroidJNI.FindClass("java/io/File");
                                        IntPtr method_getAbsolutePath = AndroidJNIHelper.GetMethodID(obj_file, "getAbsolutePath", "()Ljava/lang/String;");

                                        mPersistentDataPath = AndroidJNI.CallStringMethod(file, method_getAbsolutePath, new jvalue[0]);

                                        if (string.IsNullOrEmpty(mPersistentDataPath) == false)
                                        {
                                            Debug.Log("Got android internal path: " + mPersistentDataPath);
                                        }
                                        else
                                        {
                                            Debug.Log("Using fallback path");
#if UNITY_5_6_OR_NEWER
                                            mPersistentDataPath = "/data/data/" + Application.identifier + "/files";
#else
                                 mPersistentDataPath = "/data/data/" + Application.bundleIdentifier + "/files";
#endif
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                mPersistentDataPath = Application.persistentDataPath;
                            }
#else
                     mPersistentDataPath = Application.persistentDataPath;
#endif
                        }
                        else
                            mPersistentDataPath = Application.persistentDataPath;
                    }
                    else if (Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        mPersistentDataPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/rawData";
                    }
                }
                return mPersistentDataPath;
            }
        }

        /// <summary>
        /// record the sd external path  ****/
        /// </summary>
        private static string m_sdExternalPath = string.Empty;
        public static string SDExternalPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_sdExternalPath))
                {
                    // fuck this protection?
                    if(Service.IsSet<Android>() == false)
                    {
                       AssetLogger.LogError("读取SDEXternal路径时候,Android服务是空的?");
                       Android.Init();
                    }

                    var sd = Service.Get<Android>().GetSDFolder() + "Supermobs" + DirectorySeparatorChar;
                    if (Directory.Exists(sd) == false)
                    {
                        Directory.CreateDirectory(sd);
                    }
                    m_sdExternalPath = sd;
                }
                return m_sdExternalPath;
            }
        }

        /// <summary>
        /// 项目目录D:\UnityProject\
        /// 末尾带分隔符
        /// </summary>
        public static string ProjectRoot
        {
            get
            {
                string dataPath = Application.dataPath;
                string projectRoot = dataPath.Substring(0, dataPath.LastIndexOf('/') + 1);
                return projectRoot;
            }
        }

        private static string mRelativeWWW = string.Empty;
        public static string WWWRelativePath
        {
            get
            {
                if (string.IsNullOrEmpty(mRelativeWWW))
                {
                    if (Application.isEditor)
                        mRelativeWWW = System.Environment.CurrentDirectory.Replace("\\", "/"); // Use the build output folder directly.
#if !UNITY_2017_2_OR_NEWER
                    else if (Application.isWebPlayer)
                        mRelativeWWW = System.IO.Path.GetDirectoryName(Application.absoluteURL).Replace("\\", "/") + "/StreamingAssets";
#endif
                    else if (Application.isMobilePlatform || Application.isConsolePlatform)
                        mRelativeWWW = Application.streamingAssetsPath;
                    else // For standalone player.
                        mRelativeWWW = Application.streamingAssetsPath;
                }
                return mRelativeWWW;
            }
        }

        public static DirectoryInfo AssetDir = new DirectoryInfo(Application.dataPath);
        public readonly static string ApplicationDataFullPath = AssetDir.FullName;

        static string mDownLoadFolder;
        public static string DownloadAssetBundlesPath
        {
            get
            {
                if (string.IsNullOrEmpty(mDownLoadFolder))
                {
                    mDownLoadFolder = persistentDataPath + DirectorySeparatorChar + ASSETBUNDLE_DEVICE_FOLDER + DirectorySeparatorChar;
                }
                return mDownLoadFolder;
            }
        }

        public static string GetPathInDownLoaded(uint fileNameHash)
        {
            return DownloadAssetBundlesPath + fileNameHash + ASSETBUNDLE_SUFFIX;
        }

        public static string GetPathInDownLoaded(string fileName)
        {
            // 如果在编辑器并且！没有模拟真机加载的情况
            if (AssetPreference.isEditorAndNotSimulate && editorApi != null)
            {
                // got from output bundles path
                return editorApi.AssetbundlePath + fileName;
            }
            else
            {
                fileName = Crc32.GetStringCRC32(fileName) + ASSETBUNDLE_SUFFIX;
                return DownloadAssetBundlesPath + fileName;
            }
        }

        public static string GetPathInAndroidAssetManager(string fileName)
        {
            fileName = Crc32.GetStringCRC32(fileName) + ASSETBUNDLE_SUFFIX;
            return ASSETBUNDLE_DEVICE_FOLDER + DirectorySeparatorChar + fileName;
        }

        /// <summary>
        /// 如果是真机运行模式
        /// 那么真机当然找ipa、apk里面的目录咯
        /// Editor就找打包后，默认资源目录
        /// </summary>
        public static string GetPathInAPP(string fileName)
        {
            fileName = Crc32.GetStringCRC32(fileName) + ASSETBUNDLE_SUFFIX;

            //不同平台根目录不一样
            if (Application.platform == RuntimePlatform.Android)
            {
                //return Application.dataPath + "!assets/" + ASSETBUNDLE_DEVICE_FOLDER + DirectorySeparatorChar + fileName;
                return Application.streamingAssetsPath + DirectorySeparatorChar + ASSETBUNDLE_DEVICE_FOLDER + DirectorySeparatorChar + fileName;
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return Application.dataPath + DirectorySeparatorChar + ASSETBUNDLE_DEVICE_FOLDER + DirectorySeparatorChar + fileName;
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                return Application.dataPath + DirectorySeparatorChar + ASSETBUNDLE_DEVICE_FOLDER + DirectorySeparatorChar + fileName;
            }
            else if (Application.isEditor && editorApi != null)
            {
                return editorApi.AssetBundleEditorDevicePath + fileName;
            }
            else
                return "没有找到相关平台" + Application.platform.ToString() + "的路径:" + fileName;
        }

        public static string CachedAssetsPath
        {
            get
            {
                if (editorApi != null)
                {
                    return editorApi.CachedAssetsPath;
                }
                else
                {
                    throw new Exception("这个接口只有在Editor下有效果!");
                }
            }
        }

        /// <summary>
        /// Editor Usage
        /// ab输出的目录:xxxx/AssetBundles/
        /// </summary>
        public static string AssetbundlePath
        {
            get
            {
                if (editorApi != null)
                {
                    return editorApi.AssetbundlePath;
                }
                else
                {
                    throw new Exception("这个接口只有在Editor下有效果!");
                }
            }
        }

        public static string AssetBundleEditorDevicePath
        {
            get
            {
                if (editorApi != null)
                {
                    return editorApi.AssetBundleEditorDevicePath;
                }
                else
                {
                    throw new Exception("这个接口只有在Editor下有效果!");
                }
            }
        }
    }
}