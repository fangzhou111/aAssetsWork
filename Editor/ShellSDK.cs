/*
 * @Author: chiuan wei 
 * @Date: 2017-06-15 21:40:59 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-04 19:01:57
 */
using System;
using System.Collections.Generic;
using System.IO;
using SuperMobs.AssetManager.Core;
using UniRx;
using UnityEditor;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
    class SDKCleanupSaver
    {
        const string fileName = "EDITOR_BUILD_SDK_STATIC.config";

        [MenuItem("Assets/AssetManager/SDK/Add Plugins File To Static", false, int.MaxValue - 1000)]
        static void MenuItem()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var folder in ShellSDK.SDKFolder)
                {
                    if (path.ToLower().StartsWith(folder.ToLower()))
                    {
                        Add(path);
                        Debug.Log("添加了一个SDK静态不删除文件 " + path);
                        return;
                    }
                }

                Debug.Log("想添加某个SDK静态文件不成功 " + path);
            }
        }

        [MenuItem("Assets/AssetManager/SDK/Add Plugins File To Static", true)]
        static bool VailedMenuItem()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var folder in ShellSDK.SDKFolder)
                {
                    if (path.ToLower().StartsWith(folder.ToLower()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static void Save(string[] files)
        {
            string path = AssetPath.ProjectRoot + fileName;
            StreamWriter sw = new StreamWriter(File.Create(path));
            sw.WriteLine("// 设置在ShellSDK处理打包切换SDK资源时候,执行清理Plugins/Android | iOS目录时候不删掉哪些文件?(建议打开Unity右键添加)");
            sw.WriteLine();
            foreach (var str in files)
            {
                sw.WriteLine(str);
            }
            sw.Flush();
            sw.BaseStream.Close();
            sw.Close();
        }

        /// <summary>
        /// 增加一个新的文件到列表里面
        /// </summary>
        public static void Add(string file)
        {
            List<string> list = new List<string>(Load());
            if (list.Contains(file) == false)
            {
                list.Add(file);
            }

            Save(list.ToArray());
        }

        /// <summary>
        /// 从配置文件中读取不需要删除的文件设置
        /// </summary>
        public static string[] Load()
        {
            string path = AssetPath.ProjectRoot + fileName;
            if (File.Exists(path))
            {
                SuperMobs.AssetManager.Core.ByteReader br = new SuperMobs.AssetManager.Core.ByteReader(File.ReadAllBytes(path));
                return br.ReadLines().ToArray();
            }
            else
            {
                Save(ShellSDK.StaticFiles);
            }
            return ShellSDK.StaticFiles;
        }
    }

    /**
     * 处理SDK平台资源,打包过程中会自动处理
     * SDK资源目录存放规则:
     * 	SDK/
     * 		dyb/
     * 		baidu/
     * 		xiaomi/
     * 
     *  SDK/
     * 		channel/
     * 
     *  icon可以在下面创建不同app名字的
     * 		dyb/icon/setting.appName/
     * */
    public class ShellSDK
    {
        private ShellSDK() { }

        enum Platform
        {
            Android,
            iOS
        }

        ShellBuildSetting setting = null;

        // 缓存老的symbols发完包清理后还原
        static string oldSymbols = "";

        internal static string[] SDKFolder = new string[] {
            "Assets/Plugins/iOS",
            "Assets/Plugins/Android",
        };

        internal static string[] StaticFiles = new[] {
            "Assets/Plugins/iOS/AntiCheatToolkit",
            "Assets/Plugins/iOS/liblz4",
            "Assets/Plugins/iOS/liblzma",
            "Assets/Plugins/iOS/libslua",
            "Assets/Plugins/iOS/libYvImSdk",
            "Assets/Plugins/iOS/LuaInterface",
            "Assets/Plugins/iOS/Device",
            "Assets/Plugins/Android/assets/yayavoice_for_assets_2015101201",
            "Assets/Plugins/Android/helper",
            "Assets/Plugins/Android/assets/res",
            "Assets/Plugins/Android/libs/yayavoice",
            "Assets/Plugins/Android/libs/armeabi-v7a.meta",
            "Assets/Plugins/Android/libs/armeabi-v7a/liblz4",
            "Assets/Plugins/Android/libs/armeabi-v7a/liblzma",
            "Assets/Plugins/Android/libs/armeabi-v7a/libslua",
            "Assets/Plugins/Android/libs/armeabi-v7a/libYvImSdk",
            "Assets/Plugins/Android/libs/armeabi-v7a/libun7z",
            "Assets/Plugins/Android/libs/x86.meta",
            "Assets/Plugins/Android/libs/x86/liblz4",
            "Assets/Plugins/Android/libs/x86/liblzma",
            "Assets/Plugins/Android/libs/x86/libslua",
            "Assets/Plugins/Android/libs/x86/libYvImSdk",
            "Assets/Plugins/Android/libs/x86/libun7z",
            "Assets/Plugins/Android/AndroidManifest.xml",
        };

        /// <summary>
        /// 初始化静态的文件,打包完不清空目录处理等
        /// </summary>
        void InitStaticFiles()
        {
            StaticFiles = SDKCleanupSaver.Load();
            AssetBuilderLogger.Log(Color.yellow, "[SDK][SHELL][BUILD] 以下的文件打包切换SDK不会被删掉文件:\n" + StaticFiles.ToArrayString());
        }

        void Run(ShellBuildSetting set, string sdk, string channel)
        {
            if (channel.Equals("none"))
            {
                sdk = "default";
                AssetBuilderLogger.Log(Color.yellow, "没有指定channel，默认使用SDK/none资源");
            }

            // 从sdk配置里面获取改sdk的一些参数配置
            setting = set;

            // reset symbols
            oldSymbols = "";

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            if (target == BuildTarget.Android)
            {
                DoCleanup();
                CopyIcon(sdk, channel);
                CopySdkFiles(Platform.Android, channel);
                ChangeBuildSetting(channel);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            else if (target == BuildTarget.iOS)
            {
                DoCleanup();
                CopyIcon(sdk, channel);
                CopySdkFiles(Platform.iOS, channel);
                ChangeBuildSetting(channel);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        /// <summary>
        /// channel 控制真正编译给的渠道名称是啥(但是例如any的不会是具体渠道>,<!)
        /// </summary>
        void CopySdkFiles(Platform platform, string channel)
        {
            string target = platform.ToString();

            // fix any.90\uc.xx use the any or uc only
            //if (sdk.Contains("."))
            //{
            //	sdk = sdk.Substring(0, sdk.LastIndexOf('.'));
            //}

            // **/SDK/uc/Android/...
            string path = Directory.GetCurrentDirectory();
            string sdkFolder = string.Format("{0}/SDK/{1}/{2}", path, channel, target);
            sdkFolder = sdkFolder.Replace("\\", "/");
            if (!Directory.Exists(sdkFolder))
            {
                AssetBuilderLogger.Log(Color.magenta, "sdk folder is not exist:" + sdkFolder);
                return;
            }

            string[] files = Directory.GetFiles(sdkFolder, "*.*", SearchOption.AllDirectories);
            if (files == null || files.Length == 0)
            {
                AssetBuilderLogger.Log(Color.magenta, "sdk folder has no files to copy:" + sdkFolder);
                return;
            }

            for (int i = 0; i < files.Length; i++)
            {
                // **/SDK/uc/Android/wewx/wew.text
                string file = files[i].Replace("\\", "/");
                file = file.Replace(sdkFolder, "");
                //file = Path.Combine(AssetPath.ProjectRoot + "Assets/Plugins/" + target, file);
                //Debug.Log("拷贝SDK从" + file[i] + "\n到" + file);
                file = AssetPath.ProjectRoot + "Assets/Plugins/" + target + file;
                FileInfo fi0 = new FileInfo(files[i]);
                if (fi0.Name.StartsWith(".", StringComparison.Ordinal)) continue;
                Debug.Log("拷贝" + files[i] + "\n到" + file);
                FileInfo fi = new FileInfo(file);
                if (fi.Directory.Exists == false) fi.Directory.Create();
                File.Copy(files[i], file, true);
            }
        }

        /// <summary>
        /// 如果SDK目录有特殊icon
        /// 需要替换一下
        /// </summary>
        void CopyIcon(string sdk, string channel)
        {
            string path = Directory.GetCurrentDirectory();
            string iconFolder = string.Format("{0}/SDK/{1}/{2}", path, channel, "icon");
            iconFolder = iconFolder.Replace("\\", "/");

            // note: 相同游戏不同名称icon
            if (setting != null)
            {
                AssetBuilderLogger.Log(Color.green, "setting存在.拷特殊目录的:" + iconFolder + "/" + setting.sdk);
                // 例如同一个游戏渠道的不同名字:icon/dyb.01/
                if (Directory.Exists(iconFolder + "/" + setting.sdk))
                {
                    iconFolder = iconFolder + "/" + setting.sdk;
                }
                else
                {
                    AssetBuilderLogger.Log(Color.magenta, "got setting data > 没有找到特殊icon目录需要替换的，将用默认icon:" + iconFolder);
                    return;
                }
            }
            else if (string.IsNullOrEmpty(sdk) == false) // 增加判断传进来的sdk字段获取特殊目录
            {
                AssetBuilderLogger.Log(Color.green, "sdk查找目标特殊目录:" + iconFolder + "/" + sdk);
                // 例如同一个游戏渠道的不同名字:icon/dyb.01/
                if (Directory.Exists(iconFolder + "/" + sdk))
                {
                    iconFolder = iconFolder + "/" + sdk;
                }
                else
                {
                    AssetBuilderLogger.Log(Color.magenta, "no setting data > 没有找到特殊icon目录需要替换的，将用默认icon:" + iconFolder);
                    return;
                }
            }

            if (!Directory.Exists(iconFolder))
            {
                AssetBuilderLogger.Log(Color.magenta, "没有找到icon目录需要替换的，将用默认icon:" + iconFolder);
                return;
            }
            else
            {
                AssetBuilderLogger.Log(Color.magenta, "icon准备拷贝目标目录下的所有文件:" + iconFolder);
            }

            if (!Directory.Exists(string.Format("{0}/Assets/icon", path)))
            {
                AssetBuilderLogger.Log(Color.magenta, "项目没有icon目录，不需要替换" + iconFolder);
                return;
            }

            string[] files = Directory.GetFiles(iconFolder, "*.*", SearchOption.AllDirectories);
            if (files == null || files.Length == 0) return;
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i].Replace("\\", "/");
                file = file.Replace(iconFolder, "");
                FileInfo fi0 = new FileInfo(files[i]);
                if (fi0.Name.StartsWith(".", StringComparison.Ordinal)) continue;
                //file = Path.Combine(AssetPath.ProjectRoot + "Assets/icon/", file);
                //file = "Assets/icon" + (file.StartsWith("/", StringComparison.Ordinal) ? file : "/" + file);
                file = AssetPath.ProjectRoot + "Assets/icon/" + file;
                Debug.Log("拷贝icon:" + files[i] + "\n到" + file);
                FileInfo fi = new FileInfo(file);
                if (fi.Directory.Exists == false) fi.Directory.Create();
                File.Copy(files[i], file, true);
            }

        }

        /// <summary>
        /// 从配置文件里读取特定的sdk配置
        /// 如果有自定义的名称则在这里覆盖
        /// </summary>
        void ChangeBuildSetting(string channels)
        {
            if (setting != null)
            {
                PlayerSettings.companyName = setting.companyName;
                PlayerSettings.productName = setting.appName;

                if (string.IsNullOrEmpty(setting.appSymbols) == false)
                {
                    // cached old symbols
                    oldSymbols = AssetEditorHelper.GetDefineSymbols();
                    AssetBuilderLogger.Log("老的symbols=" + oldSymbols);

                    AssetBuilderLogger.Log("打包前设置symbols=" + setting.appSymbols);
                    AssetEditorHelper.SetDefineSymbols(setting.appSymbols);
                }

#if UNITY_5_6_OR_NEWER || UNITY_2017_2_OR_NEWER
				PlayerSettings.applicationIdentifier = setting.bundleid;

                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                if (target == BuildTarget.Android) {
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, setting.bundleid);
                } else if (target == BuildTarget.iOS) {
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, setting.bundleid);
                }
#else
                PlayerSettings.bundleIdentifier = setting.bundleid;
#endif
            }
        }

        /// <summary>
        /// 如果deep的话会把sdk的目录真的全删掉
        /// </summary>
        void DoCleanup(bool deep = false)
        {
            // 先初始化不清空列表
            InitStaticFiles();

            // cleanup the sdk folder reset.
            foreach (var item in SDKFolder)
            {
                if (deep == false)
                    CleanupSDKFolder(item);
                else
                {
                    // 直接删除所有这些sdk目录
                    if (Directory.Exists(item) == false) continue;
                    Directory.Delete(item, true);
                }
            }

            // reset the symbols
            if (string.IsNullOrEmpty(oldSymbols) == false)
            {
                AssetBuilderLogger.Log("打包后设置symbols=" + oldSymbols);
                AssetEditorHelper.SetDefineSymbols(oldSymbols);

                oldSymbols = "";
            }
        }

        // 先查找所有文件，比对是否需要删除，再查找一遍目录，为空则删掉目录
        void CleanupSDKFolder(string folder)
        {
            if (Directory.Exists(folder) == false) return;

            // 找出所有文件
            string[] allFiles = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            List<string> fileList = new List<string>();
            for (int i = 0; i < allFiles.Length; i++)
            {
                fileList.Add(allFiles[i].Replace("\\", "/"));
            }

            // 判断是否要删掉这个文件
            for (int i = 0; i < fileList.Count; i++)
            {
                string path = fileList[i];
                bool del = true;
                for (int j = 0; j < StaticFiles.Length; j++)
                {
                    if (path.ToLower().Contains(StaticFiles[j].ToLower()))
                    {
                        del = false;
                        break;
                    }
                }
                if (del && File.Exists(path)) File.Delete(path);
            }

            // 所有的目录
            // 删掉空的目录撒
            allFiles = Directory.GetDirectories(folder, "*", SearchOption.AllDirectories);
            List<string> dicList = new List<string>();
            for (int i = 0; i < allFiles.Length; i++)
            {
                allFiles[i] = allFiles[i].Replace("\\", "/");
                dicList.Add(allFiles[i]);
            }

            dicList.Sort(delegate (string a, string b)
            {
                if (a.Length > b.Length)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });

            // 删掉空的目录
            for (int i = 0; i < dicList.Count; i++)
            {
                string path = dicList[i].Replace("\\", "/");
                string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    DirectoryInfo dir = new DirectoryInfo(path);
                    dir.Delete(true);
                    if (File.Exists(path + ".meta"))
                        File.Delete(path + ".meta");
                }
            }
        }

        #region static api

        public static void InitSDK(ShellBuildSetting s, string sdk, string channel)
        {
            ShellSDK shell = new ShellSDK();
            shell.Run(s, sdk, channel);
        }

        public static void Cleanup()
        {
            ShellSDK shell = new ShellSDK();
            shell.DoCleanup();
        }

        #endregion

    }
}