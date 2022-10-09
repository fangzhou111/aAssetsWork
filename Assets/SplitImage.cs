using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using SuperMobs.AssetManager.Core;

namespace SuperMobs.AssetManager.Assets
{
	public class SplitImage : ISplitAssetProcessor<Image>
	{
		public void CleanAssets(Image obj)
		{
			obj.sprite = null;
		}

		public Object[] GetAssets(Image obj)
		{
			return new Object[] { obj.sprite };
		}

		public void SetAssets(Image obj, Object[] assets)
		{
			obj.sprite = assets[0].AsSprite();
		}
	}
}

