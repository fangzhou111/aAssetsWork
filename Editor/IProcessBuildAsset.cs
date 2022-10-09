namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using System.Collections;
	using Object = UnityEngine.Object;

	public class ProcessBuildAssetAttribute : System.Attribute
	{
		public int order;
		public ProcessBuildAssetAttribute(int order)
		{
			this.order = order;
		}
	}

	public interface IProcessBuildAsset
	{
		bool IsValid(string assetPath);
		string[] DoProcess(string assetPath);
	}
}