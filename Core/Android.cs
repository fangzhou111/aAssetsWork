/*
 * @Author: chiuan wei 
 * @Date: 2017-07-04 17:15:01 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-06 16:29:47
 */
using System;
using SuperMobs.Core;
using UnityEngine;

namespace SuperMobs.AssetManager.Core
{
    /// <summary>
    /// 安卓特殊的东西
    /// 特殊jar接口
    /// </summary>
    public class Android
    {
        //[RuntimeInitialize(RuntimeInitializeType.BeforeSceneLoad, 0)]
        public static void Init()
        {
            if (Service.IsSet<Android>() == false)
            {
                var android = new Android();
                android._init();
                Service.Set<Android>(android);
            }
        }

        public static string jarName = "com.lj.AndroidHelper.Helper";

#if UNITY_ANDROID
        private static AndroidJavaClass _helper;
        //private const string ANDROID_GET_BYTES_FUNCTION = "getAssetBytes";
        //private const string ANDROID_CHECK_FUNCTION = "isFileExists";
        //private const string ANDROID_GET_STRING_FUNCTION = "getString";

        //调用方案2
        private static IntPtr clazzPtr;
        private static IntPtr methodPtr1;
        private static IntPtr methodPtr2;
#endif

        void _init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
         _helper = new AndroidJavaClass(jarName);
         using(AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
            object jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
            _helper.CallStatic("init", jo);
         }

         clazzPtr = AndroidJNI.FindClass("com/lj/AndroidHelper/Helper");
         methodPtr1 = AndroidJNI.GetStaticMethodID(clazzPtr, "getAssetSize", "(Ljava/lang/String;)I");
         methodPtr2 = AndroidJNI.GetStaticMethodID(clazzPtr, "getAssetBytes", "(Ljava/lang/String;)[B");
#endif
        }

        //  #if UNITY_ANDROID && !UNITY_EDITOR 
#if UNITY_ANDROID
        public static byte[] GetBytesInAndroid(string path)
        {
            byte[] data = null;

            object[] objs = new object[] { path };
            jvalue[] jvs = AndroidJNIHelper.CreateJNIArgArray(objs);

            int exsit = AndroidJNI.CallStaticIntMethod(clazzPtr, methodPtr1, jvs);
            if (exsit > 0)
            {
                IntPtr dataPtr = AndroidJNI.CallStaticObjectMethod(clazzPtr, methodPtr2, jvs);
                data = AndroidJNI.FromByteArray(dataPtr);
                AndroidJNI.DeleteLocalRef(dataPtr);
            }
            else
            {
                AssetLogger.LogError("cant found android io file at : " + path);
            }
            AndroidJNIHelper.DeleteJNIArgArray(objs, jvs);

            return data;
        }

#endif

        public bool InitAPK(string apk)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass jni = new AndroidJavaClass(jarName);
            return jni.CallStatic<bool>("installApk", apk);
#else
        return true;
#endif
        }

        public long GetMemory()
        {
#if UNITY_ANDROID
            AndroidJavaClass jni = new AndroidJavaClass(jarName);
            return jni.CallStatic<long>("getCurrentAvailableMemorySize");
#else
         return 0;
#endif
        }

        public string GetSDFolder()
        {
#if UNITY_ANDROID
            AndroidJavaClass jni = new AndroidJavaClass(jarName);
            return jni.CallStatic<string>("getExternalStoragePath") + AssetPath.DirectorySeparatorChar;
#else
         return AssetPath.persistentDataPath + AssetPath.DirectorySeparatorChar;
#endif
        }

        public byte[] LoadInAndroid(string fileName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
         string path = AssetPath.GetPathInAndroidAssetManager(fileName);
         return GetBytesInAndroid(path);
#else
            return null;
#endif
        }

    }
}