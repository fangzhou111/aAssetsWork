using UniRx;
using System.Linq;
namespace SuperMobs.AssetManager.Editor
{
    using UnityEngine;
    using System.Collections.Generic;
    using SuperMobs.AssetManager.Core;
    using System;
    using Assets;
    /**
	 * 记录所有的打包缓存信息
	 * */

    [Serializable]
	public class AssetCachInfoCollection : SuperJsonObject
	{
		public AssetCachInfo[] infos;
	}

    [Serializable]
    public class AssetBundleCollection : SuperJsonObject
    {
        public Bundle[] bundles;
    }
}
