using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using SuperMobs.Core;
using SuperMobs.AssetManager.Core;

namespace SuperMobs.AssetManager.Assets
{
	public class AssetObject
	{
		private string assetName = string.Empty;
		private Bundle bundle;
		private Object obj;

		internal IDisposable dispose;
		internal Action<AssetObject> async;

		private AssetObject() { }
		public AssetObject(Bundle b)
		{
			this.bundle = b;
		}

		internal bool isLoaded()
		{
			return obj != null;
		}

		internal void Unload()
		{
			if (obj != null)
			{
				if (obj is AssetBundle)
				{
					((AssetBundle)obj).Unload(false);
				}

				Object.DestroyImmediate(obj, true);

				obj = null;
			}

			if (dispose != null)
			{
				dispose.Dispose();
				dispose = null;
			}

			if (async != null)
			{
				AssetLogger.LogError("为何释放AssetObject:" + assetName + "  但是存在async回调监听，之前的异步结果没发送.");
				async(this);
				async = null;
			}
		}

		internal void InitAsset(Object o)
		{
			//record the loaded obj.
			this.obj = o;

			// 如果异步还没结束则结束掉
			if (dispose != null)
			{
				dispose.Dispose();
				dispose = null;
			}

			if (async != null)
			{
				async(this);
				async = null;
			}
		}

		public void AddOwner(Object owner)
		{
			if (bundle != null && owner != null)
				bundle.GetDependenceBundles().ForEach(b => b.AddOwner(owner));
		}

		public T Require<T>(Object owner) where T : Object
		{
			AddOwner(owner);

			return obj as T;
		}

	}
}
