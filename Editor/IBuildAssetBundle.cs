namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using UnityEditor;
	using System.Collections;
	using System.Collections.Generic;

	public class BuildAssetBundleAttribute : System.Attribute
	{
		public int order;
		public BuildAssetBundleAttribute(int order)
		{
			this.order = order;
		}
	}

	public enum BuildAssetBundleMode
	{
		Single,
		// 如果这次编译时候可以多个一起,例如配置表,贴图等
		// 不需要一个个编译,减少unity新版本会每次都compling代码
		Multi, 
	}

	public interface IBuildAssetBundle
	{
		BuildAssetBundleMode GetBuildAssetBundleMode();
		bool IsVaild(AssetBundleBuild abb);
		void Build(List<AssetBundleBuild> abb);
	}
}