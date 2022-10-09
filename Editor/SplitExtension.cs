using System;
using System.Collections;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Assets;

namespace SuperMobs.AssetManager.Editor
{
	public static class SplitExtension
	{
		public static string[] GetAllSplitAssetPaths(this SplitController controller)
		{
			List<string> assets = new List<string>();

			if (controller.splitComponents != null)
			{
				foreach (var com in controller.splitComponents)
				{
					foreach (var item in com.linkAssets)
					{
						if (string.IsNullOrEmpty(item) == false && assets.Contains(item) == false)
							assets.Add(item);
					}
				}
			}

			if (controller.splitMaterials != null)
			{
				foreach (var com in controller.splitMaterials)
				{
					foreach (var item in com.texturePaths)
					{
						if (string.IsNullOrEmpty(item) == false && assets.Contains(item) == false)
							assets.Add(item);
					}
				}
			}

			return assets.ToArray();
		}
	}
}
