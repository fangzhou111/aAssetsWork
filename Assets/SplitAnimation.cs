using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SuperMobs.AssetManager.Assets
{
	public class SplitAnimation : ISplitAssetProcessor<Animation>
	{
		static List<string> GetClipNames(Animation obj)
		{
			List<string> ret = new List<string>();
			IEnumerator enumerator = obj.GetEnumerator();
			while (enumerator.MoveNext())
				ret.Add((enumerator.Current as AnimationState).name);
			return ret;
		}

		public void CleanAssets(Animation obj)
		{
			var clips = GetClipNames(obj);
			foreach (string name in clips)
				obj.RemoveClip(name);

			obj.clip = null;
		}

		public Object[] GetAssets(Animation obj)
		{
			List<Object> ret = new List<Object>();
			ret.Add(obj.clip);
			var clips = GetClipNames(obj);
			foreach (string name in clips)
				ret.Add(obj.GetClip(name));
			return ret.ToArray();
		}

		public void SetAssets(Animation obj, Object[] assets)
		{
			for (int i = 1; i < assets.Length; i++)
			{
				AnimationClip clip = assets[i] as AnimationClip;
				obj.AddClip(clip, clip.name);
			}
			obj.clip = assets[0] as AnimationClip;
		}
	}
}

