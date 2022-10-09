using System;
using UnityEngine;
using SuperMobs.Core;

namespace SuperMobs.AssetManager.Assets
{

	[Serializable]
	public struct ComponentSplitLinker
	{
		public Component component;
		public string[] linkAssets;
	}

	[Serializable]
	public struct MaterialSplitLinker
	{
		public Material material;
		public string[] propertyNames;
		public string[] texturePaths;
	}

	/// <summary>
	/// 负责管理某个对象或者场景的分离还原和剔除操作
	/// </summary>
	public class SplitController : MonoBehaviour
	{
		// 缓存了需要分离的组件脚本
		public ComponentSplitLinker[] splitComponents;

		// 缓存需要分离的材质球信息
		public MaterialSplitLinker[] splitMaterials;

		void Awake()
		{
			Bind();
		}

		public void Bind()
		{
			foreach (var linker in splitComponents)
			{
				var split = SplitProcesserConfig.Instance.GetProcesser(linker.component.GetType());
				if (split != null)
				{
					var objs = new UnityEngine.Object[linker.linkAssets.Length];
					for (int i = 0; i < linker.linkAssets.Length; i++)
					{
                        if (string.IsNullOrEmpty(linker.linkAssets[i]) == false)
                        {
                            var a = AssetManager.Instance.Load(linker.linkAssets[i]);
                            objs[i] = a.Require<UnityEngine.Object>(this.gameObject);
                        }
					}
					split.SetAssets(linker.component, objs);
				}
			}
		}

		public void Split()
		{

		}
	}
}

