using System;
using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SuperMobs.AssetManager.Assets
{
	public class SplitRawImage : ISplitAssetProcessor<RawImage>
	{
		public void CleanAssets(RawImage obj)
		{
			obj.texture = null;
		}

		public Object[] GetAssets(RawImage obj)
		{
			return new Object[] { obj.texture };
		}

		public void SetAssets(RawImage obj, Object[] assets)
		{
			if (assets[0] is Sprite)
			{
				Sprite spr = assets[0] as Sprite;
				obj.texture = spr.texture;

				Vector4 vec = DataUtility.GetInnerUV(spr);
				obj.uvRect = new Rect(vec[0], vec[1], vec[2] - vec[0], vec[3] - vec[1]);
			}
			else
			{
				obj.texture = assets[0] as Texture;
			}
		}
	}
}

