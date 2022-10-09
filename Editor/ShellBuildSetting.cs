using System;
using SuperMobs.AssetManager.Core;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace SuperMobs.AssetManager.Editor
{
	/*
	 * 每个平台编译的信息配置
	 * 当ShellBuilder打包时候会从cmd输入信息里面获取某些字段信息
	 * 1、先加载项目配置的shell信息
	 * 2、如果没有配置就获取输入的信息
	 * TODO fix 是否考虑优先输入的，然后没有再选择配置里面的
	 * */

	[System.Serializable]
	public class ShellBuildSetting : SuperJsonObject
	{
		// 用来识别该配置
		public string settingName;

		public string companyName;
		public string appName;
		public string bundleid;
		public string sdk;
		public string channel;
		public string web;
		public string cdn;
		public string server;
		public string demo;
		public string noUpdate;
		public string iosprofile;

		// unity可以预设一些宏定义(某些sdk渠道对接代码需要)
		public string appSymbols;

		public string Get(string key)
		{
			if (key.Equals("companyName")) return companyName;
			else if (key.Equals("appName")) return appName;
			else if (key.Equals("bundleid")) return bundleid;
			else if (key.Equals("sdk")) return sdk;
			else if (key.Equals("channel")) return channel;
			else if (key.Equals("web")) return web;
			else if (key.Equals("cdn")) return cdn;
			else if (key.Equals("server")) return server;
			else if (key.Equals("demo")) return demo;
			else if (key.Equals("noUpdate")) return noUpdate;
			else if (key.Equals("iosprofile")) return iosprofile;
			else if (key.Equals("appSymbols")) return appSymbols;
			else return null;
		}
	}

	[System.Serializable]
	internal class ShellBuildSettingPlatform : SuperJsonObject
	{
		public ShellBuildSetting[] setting;
		public ShellBuildSettingPlatform()
		{
			setting = new ShellBuildSetting[0];
		}

		public ShellBuildSetting Get(string settingName)
		{
			foreach (var item in setting)
			{
				if (item.settingName.Equals(settingName))
				{
					return item;
				}
			}
			return null;
		}

		public ShellBuildSetting GetBySdkAndChannel(string sdk, string channel)
		{
			foreach (var item in setting)
			{
				if (item.sdk.Equals(sdk) && item.channel.Equals(channel))
				{
					return item;
				}
			}
			return null;
		}
	}


	// Editor打开的时候初始化
	[InitializeOnLoad]
	class ShellBuildSettingEditor
	{
		static ShellBuildSettingPlatform DefaultSetting()
		{
			ShellBuildSettingPlatform setting = new ShellBuildSettingPlatform();

			ShellBuildSetting set = new ShellBuildSetting();
			set.settingName = "default.none";
			set.companyName = "supermobs";
			set.appName = "NONAME";
			set.bundleid = "com.supermobs.demo";
			set.sdk = "default";
			set.channel = "none";
			set.web = "";
			set.cdn = "";
			set.server = "";
			set.demo = "yes";
			set.noUpdate = "yes";
			set.iosprofile = "";
			set.appSymbols = "";
			setting.setting = new ShellBuildSetting[] { set };

			return setting;
		}

		static ShellBuildSettingEditor()
		{
			string ios = AssetPath.ProjectRoot + ShellBuilder.SETTING_IOS;
			string android = AssetPath.ProjectRoot + ShellBuilder.SETTING_ANDROID;

			if (File.Exists(ios) == false)
			{
				File.WriteAllText(ios, DefaultSetting().ToJson());
			}

			if (File.Exists(android) == false)
			{
				File.WriteAllText(android, DefaultSetting().ToJson());
			}

		}
	}
}
