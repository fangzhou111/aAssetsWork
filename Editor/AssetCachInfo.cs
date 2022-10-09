using UniRx;
using System.Linq;
namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using System.Collections.Generic;
	using SuperMobs.AssetManager.Core;
	using System;
	using SuperMobs.AssetManager.Assets;

	/// <summary>
	/// single asset build infomation meta file
	/// each builded file will cach its dependencies & build files
	/// </summary>

	[Serializable]
	public class AssetCachInfo : SuperJsonObject
	{
		public string sourcePath = string.Empty;
		public uint sourceCrc = 0;

		// asset build type
		// >>> singleasset | dependence | rawimage | rawsprite
		// if dep should build all ABB togather
		// this will check when split the buildAssets when process working...
		// only splitasset is unity's dependence should be changed with this flag.
		public AssetBuildType buildType = AssetBuildType.single;

		// NOTE: maybe the clone path as same as sourcePath
		public string[] buildPaths = null;

		// all buildPaths should into some bundles names.
		// use for check if this all buildPaths bundles exist.
		public string[] buildBundleNames = null;

		// link sources if split[] or dependencies[] should 
		// link this source asset paths
		public string[] linkSourcePaths = null;
        
        public AssetCachInfo()
        {
            linkSourcePaths = new string[0];
        }

		public void AddLinks(List<string> linkAssets)
		{
			var list = new List<string>(linkSourcePaths ?? new string[0]);

			linkAssets.AsSafeEnumerable()
				 .ToObservable(Scheduler.Immediate)
				 .Where(item => list.Contains(item) == false)
				 .Do(item => list.Add(item))
				 .Subscribe();

			// replace new list.
			linkSourcePaths = list.ToArray();
		}

		public void AddBundle(string bundle)
		{
			if (string.IsNullOrEmpty(bundle))
			{
				throw new Exception("wanna AddBundle with empty input for " + sourcePath);
			}

			var list = new List<string>(buildBundleNames ?? new string[0]);

			// NOTE:考虑需要匹配buildPaths么,也就是每个对应一个bundle名称
			//if (list.Contains(bundle) == false)
			{
				list.Add(bundle);
			}

			// replace new list.
			buildBundleNames = list.ToArray();
		}

        public string GetBuildName(string buildPath)
        {
            for (int i = 0,len = buildPaths.Length; i < len; i++)
            {
                if(buildPaths[i] == buildPath)
                {
                    return buildBundleNames[i];
                }
            }

            return null;
        }
	}
}