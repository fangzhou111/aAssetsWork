using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SuperMobs.AssetManager.Assets
{
	public class SplitSkinnedMeshRenderer : ISplitAssetProcessor<SkinnedMeshRenderer>
	{
		public void CleanAssets(SkinnedMeshRenderer obj)
		{
			obj.sharedMesh = null;
		}

		public Object[] GetAssets(SkinnedMeshRenderer obj)
		{
			if (obj.sharedMesh == null || string.IsNullOrEmpty(obj.sharedMesh.name))
				return null;
			return new Object[] { obj.sharedMesh };
		}

		public void SetAssets(SkinnedMeshRenderer obj, Object[] assets)
		{
			obj.sharedMesh = assets[0] as Mesh;
		}
	}
}

