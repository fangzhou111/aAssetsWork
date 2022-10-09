using System;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Assets;
using System.IO;
using UnityEngine;

namespace SuperMobs.AssetManager.Package
{
    /// <summary>
    /// app的属性设置
    /// 从编译cmd传入或者默认设置
    /// </summary>
    [Serializable]
    public class AppPreference : SuperJsonObject
    {
        // version + "." + codeVer + "." + resVer
        public string version;

        // test \ uc.test ....
        public string sdk;

        // uc \ xiaomi \ huawei \ mz
        public string channel;

        // enable \ disable
        public string log;

        // cdn url
        public string cdn;

        // web checkout info url
        public string web;

        // 是否允许更新出错也进游戏
        // yes  | no
        public string demo;

        // 是否停止提供更新检查，例如上线没有维护了。。
        // yes  | no
        public string noUpdate;

        // 默认登陆服务器
        public string server;

        public static AppPreference LoadFromResources()
        {
            var appPreference = new AppPreference();
            var t = Resources.Load("version") as TextAsset;
            if (t != null)
            {
                try
                {
                    appPreference.FromBytes(t.bytes);
                    PlayerPrefs.SetString("SDK", appPreference.sdk);
                    PlayerPrefs.SetString("CHANNEL", appPreference.channel);
                    return appPreference;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return null;
        }
    }
}
