/*
 * @Author: chiuan wei 
 * @Date: 2017-05-23 15:04:03 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-04 18:57:28
 */
using System;
using System.IO;
using SuperMobs.AssetManager.Core;
using UnityEditor;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
   /// <summary>
   /// SHELL BUILDER
   /// 远程编译操作
   /// 
   /// platform: ios\android
   /// log: enable\disable
   /// sdk: "dyb.test"
   /// channel: "dyb"
   /// version: version
   /// 
   /// </summary>
   public partial class ShellBuilder
   {
      public const string SETTING_IOS = "BUILD_SETTING_IOS.config";
      public const string SETTING_ANDROID = "BUILD_SETTING_ANDROID.config";

      string[] currentArgs = null;

      // 当前发布的版本的app参数设置
      string priority = "set"; //优先选择：cmd | set
      ShellBuildSetting setting;

      public ShellBuilder()
      {
         currentArgs = null;
      }

      /// <summary>
      /// 获取参数
      /// </summary>
      public string GetArg(string prefix)
      {
         string set = null;
         if (setting != null)
         {
            set = setting.Get(prefix);
            if (string.IsNullOrEmpty(set) == false && priority.Equals("set"))
            {
               return set;
            }
         }

         if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("SIM_BUILD_PLAYER")) == false)
         {
            return System.Environment.GetEnvironmentVariable(prefix);
         }

         // 缓存一下这次编译，可能重复执行一个unity会变化??
         if (currentArgs == null)
         {
            currentArgs = System.Environment.GetCommandLineArgs();
         }

         // 在这里分析shell传入的参数
         // 这里遍历所有参数，找到 project开头的参数， 然后把-符号 后面的字符串返回
         // version-$VERSION
         foreach (string arg in currentArgs)
         {
            string[] vals = arg.Split('-');
            if (vals[0].ToLower().Equals(prefix.ToLower()))
            {
               return arg.Substring(arg.IndexOf('-') + 1);
            }
         }

         // 如果cmd也没有，还是返回set的值(也许也是null)
         return set;
      }

      void ChangePlatform()
      {
         platform = platform ?? GetArg("platform");
         platform = platform ?? AssetPath.GetBuildTargetPlatform();

         Debug.Log("切换平台为：" + platform);
         Debug.Log("当前平台为：" + EditorUserBuildSettings.activeBuildTarget.ToString());

         if (platform == AssetPreference.PLATFORM_IOS)
         {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
#if UNITY_5_6_OR_NEWER
               EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
#else
               EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.iOS);
#endif
            }
         }
         else if (platform == AssetPreference.PLATFORM_ANDROID)
         {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
#if UNITY_5_6_OR_NEWER
               EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
#else
               EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);
#endif
            }
         }
         else if (platform == AssetPreference.PLATFORM_STANDARD)
         {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                {
#if UNITY_5_6_OR_NEWER
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
#else
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneWindows64);
#endif
                }
            }
         else
         {
            throw new Exception("Cant change to UNKNOWN platform = " + platform);
         }
      }

      void InitShellBuildSetting()
      {
         string sdk = GetArg("sdk") ?? "default";
         string channel = GetArg("channel") ?? "none";
         priority = GetArg("priority") ?? "set";
         if (string.IsNullOrEmpty(sdk) || string.IsNullOrEmpty(channel))
         {
            return;
         }

         // 默认的例如：default.none
         // release.uc
         string settingName = sdk + "." + channel;

         string path;
         if (AssetPath.GetBuildTargetPlatform() == AssetPreference.PLATFORM_IOS)
         {
            path = AssetPath.ProjectRoot + SETTING_IOS;
         }
         else if (AssetPath.GetBuildTargetPlatform() == AssetPreference.PLATFORM_ANDROID)
         {
            path = AssetPath.ProjectRoot + SETTING_ANDROID;
         }
         else if (AssetPath.GetBuildTargetPlatform() == AssetPreference.PLATFORM_STANDARD)
         {
            setting = null;
            path = null;

         }
         else
         {
            AssetBuilderLogger.Log(Color.yellow, "该平台没有配置任何的自定义打包设置!");
            setting = null;
            path = null;
         }

         if (string.IsNullOrEmpty(path) == false)
         {
            if (File.Exists(path) == false)
            {

               AssetBuilderLogger.LogError("找不到发布配置文件:" + path);
               return;
            }
            ShellBuildSettingPlatform sp = new ShellBuildSettingPlatform();
            sp.FromBytes(File.ReadAllBytes(path));
            setting = sp.GetBySdkAndChannel(sdk, channel);
            if (setting != null)
            {
               AssetBuilderLogger.Log("找到可用的发布配置config:\n" + setting.ToJson());
            }
         }
      }

   }
}