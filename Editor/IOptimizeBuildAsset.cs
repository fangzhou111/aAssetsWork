namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using System.Collections;

	public class OptimizeBuildAssetBeforeProcessAttribute : System.Attribute
	{
		public int order;

		public OptimizeBuildAssetBeforeProcessAttribute(int order)
		{
			this.order = order;
		}
	}

	public interface IOptimizeBuildAsset
	{
		bool IsVaild(string assetPath);
		void DoOptimize(string assetPath);
	}
}