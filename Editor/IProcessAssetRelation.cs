namespace SuperMobs.AssetManager.Editor
{
	using SuperMobs.AssetManager.Assets;
	using UnityEngine;
	using UnityEditor;
	using System;
	using System.Collections;
	using SuperMobs.AssetManager.Core;
	using Object = UnityEngine.Object;

	/*
	 * 资源分离器
	 * 提供处理某个资源时候，判断是否需要分离资源
	 * 如果要，则针对这个打包的原始资源会处理分离后的资源然后返回数组
	 * 注意：这里的依赖并非仅仅是分离，也可以是不分离，然后返回需要依赖的对象，默认依赖
	 * */

	public class ProcessAssetRelationAttribute : Attribute
	{
		public int order;
		public ProcessAssetRelationAttribute(int order)
		{
			this.order = order;
		}
	}

	/// <summary>
	/// 默认的分离器接口
	/// 对某个类型或者某个资源分离可以设置分离Option
	/// </summary>
	public interface IProcessAssetRelation
	{
		bool IsVaild(string assetPath);
		string[] DoSplit(string assetPath);
		AssetBuildType GetAssetBuildMode(string assetPath);
	}
}