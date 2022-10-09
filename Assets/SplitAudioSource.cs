using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SuperMobs.AssetManager.Assets
{
	public class SplitAudioSource : ISplitAssetProcessor<AudioSource>
	{
		public void CleanAssets(AudioSource obj)
		{
			obj.clip = null;
		}

		public Object[] GetAssets(AudioSource obj)
		{
			return new Object[] { obj.clip };
		}

		public void SetAssets(AudioSource obj, Object[] assets)
		{
			obj.clip = assets[0] as AudioClip;
			if (obj.enabled && obj.playOnAwake)
			{
				obj.Play();
			}
		}
	}
}

