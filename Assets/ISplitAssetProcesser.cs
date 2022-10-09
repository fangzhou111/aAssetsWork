namespace SuperMobs.AssetManager.Assets
{
	using UnityEngine;
	using System.Collections;
	using Object = UnityEngine.Object;

	// 资源引用关系处理器接口
	// 资源类型：texture、sprite、audio、/*font*/、mesh、Avatar、animation\RuntimeAnimatorController、ScriptObject
	public interface ISplitAssetProcessor<T> where T : class
	{
		// 将资源赋给对象
		void SetAssets(T obj, Object[] assets);

		// 获取对象使用的资源列表
		Object[] GetAssets(T obj);

		// 去掉组件对当前使用资源的引用
		void CleanAssets(T obj);
	}
}