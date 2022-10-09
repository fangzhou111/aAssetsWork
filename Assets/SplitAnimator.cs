using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SuperMobs.AssetManager.Assets
{
	public class SplitAnimator : ISplitAssetProcessor<Animator>
	{
		public void CleanAssets(Animator obj)
		{
			obj.runtimeAnimatorController = null;
			obj.avatar = null;
		}

		public Object[] GetAssets(Animator obj)
		{
			return new Object[] { obj.runtimeAnimatorController, obj.avatar };
		}

		public void SetAssets(Animator obj, Object[] assets)
		{
			obj.runtimeAnimatorController = assets[0] as RuntimeAnimatorController;

			Avatar avatar = assets[1] as Avatar;
			obj.avatar = avatar;
		}
	}
}

