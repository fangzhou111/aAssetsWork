namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using UnityEditor;
	using System;
	using System.Collections.Generic;
	using UniRx;

	public class AssetBundleBuildMap
	{
		Dictionary<string, AssetBundleBuild> dic_abb = new Dictionary<string, AssetBundleBuild>();

		public int Count
		{
			get { return dic_abb.Count; }
		}

		public string Log()
		{
			string log = "BuldMap Count=" + Count + "\n";
			foreach (var item in dic_abb)
			{
				var str = item.Key + ":\n";
				str += "[ \n";
				foreach (var asset in item.Value.assetNames)
				{
					str += "    > " + asset;
				}

				str += "\n]\n\n";
				log += str;
			}
			return log;
		}

		public void Add(AssetBundleBuild abb)
		{
			Add(abb.assetBundleName, abb.assetNames, abb.assetBundleVariant);
		}

		public void Add(string assetBundleName, string[] assetNames, string assetBundleVariant)
		{
			AssetBundleBuild abb;
			if (!dic_abb.TryGetValue(assetBundleName, out abb))
			{
				abb = new AssetBundleBuild();
				dic_abb.Add(assetBundleName, abb);
			}

			Func<string, bool> checkIfContains = (assetName) =>
			{
				for (int i = 0; abb.assetNames != null && i < abb.assetNames.Length; i++)
				{
					if (abb.assetNames[i] == assetName) return true;
				}
				return false;
			};

			Action<string> addAsset = (assetName) =>
			{
				if (abb.assetNames == null)
				{
					abb.assetNames = new string[0];
				}
				string[] assets = new string[abb.assetNames.Length + 1];
				for (int i = 0; i < abb.assetNames.Length; i++)
				{
					assets[i] = abb.assetNames[i];
				}
				assets[assets.Length - 1] = assetName;
				abb.assetNames = assets;
			};

			for (int i = 0; i < assetNames.Length; i++)
			{
				if (!checkIfContains(assetNames[i]))
				{
					addAsset(assetNames[i]);
				}
			}

			abb.assetBundleName = assetBundleName;
			abb.assetBundleVariant = assetBundleVariant;
			dic_abb[assetBundleName] = abb;
		}

		public AssetBundleBuild[] ToArray()
		{
			List<AssetBundleBuild> list = new List<AssetBundleBuild>();

			dic_abb
				.AsSafeEnumerable()
				.ToObservable(Scheduler.Immediate)
				.Subscribe(x => list.Add(x.Value));

			return list.ToArray();
		}

		public AssetBundleBuild GetByAssetBundleName(string bundleName)
		{
			AssetBundleBuild abb;
			if (dic_abb.TryGetValue(bundleName, out abb) == false)
			{
				throw new Exception("GetByAssetBundleName cant found > " + bundleName);
			}
			return abb;
		}
	}
}