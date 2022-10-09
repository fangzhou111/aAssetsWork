using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SuperMobs.AssetManager.Assets
{
	public class SplitMeshFilter : ISplitAssetProcessor<MeshFilter>
	{
		public void CleanAssets(MeshFilter obj)
		{
			obj.sharedMesh = null;
		}

		public Object[] GetAssets(MeshFilter obj)
		{
			if (obj.sharedMesh == null || string.IsNullOrEmpty(obj.sharedMesh.name))
				return null;
			return new Object[] { obj.sharedMesh };
		}

		public void SetAssets(MeshFilter obj, Object[] assets)
		{
			obj.sharedMesh = assets[0] as Mesh;
		}
	}
}

