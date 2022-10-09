using System;
using System.IO;
using SuperMobs.AssetManager.Core;
using SuperMobs.Core;
using UnityEngine;

namespace SuperMobs.AssetManager.Loader
{
	internal class AppIO : SingletonLoader<AppIO, byte[]>
	{
		public override object Load(string fileName)
		{
			string path = AssetPath.GetPathInAPP(fileName);
			if (File.Exists(path))
			{
				return File.ReadAllBytes(path);
			}
			else
			{
				return null;
			}
		}
	}

	internal class DownloadIO : SingletonLoader<DownloadIO, byte[]>
	{
		public override object Load(string fileName)
		{
			string path = AssetPath.GetPathInDownLoaded(fileName);
			if (File.Exists(path))
			{
				return File.ReadAllBytes(path);
			}
			else
			{
				return null;
			}
		}
	}

	internal class AndroidIO : SingletonLoader<AndroidIO, byte[]>
	{
		public override object Load(string fileName)
		{
#if UNITY_ANDROID
			return Service.Get<Android>().LoadInAndroid(fileName);
			// return null;
#else
			return null;
#endif
		}
	}

}
