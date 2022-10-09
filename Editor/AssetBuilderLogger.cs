// 可以开启打包资源LOG
#define ENABLE_ASSET_LOG


using System;
using UnityEditor;
using UnityEngine;
using SuperMobs.AssetManager.Core;

namespace SuperMobs.AssetManager.Editor
{
	public static class AssetBuilderLogger
	{
#if ENABLE_ASSET_LOG
		static readonly bool enableLog = true;
#else
		static readonly bool enableLog = false;
#endif

		public static void LogError(string content)
		{
			if (enableLog)
				Debug.LogError(content);
		}

		public static void Log(string content)
		{
			if (enableLog)
				Debug.Log(content);
		}

		public static void Log(Color color, string content)
		{
			if (enableLog)
			{
				Debug.Log("<color=#" + color.ColorToHex() + ">" + content + "</color>");
			}
		}
	}
}
