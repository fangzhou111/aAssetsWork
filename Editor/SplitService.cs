using System;
using System.Collections;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Assets;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using UniRx;
using System.IO;

namespace SuperMobs.AssetManager.Editor
{
	/// <summary>
	/// 提供操作分离资源的服务
	/// </summary>
	public class SplitService
	{
		Dictionary<Object, string> cachSplitAssetPath = new Dictionary<Object, string>();

		/// <summary>
		/// 获取一系列需要分离对象的分离操作实例
		/// </summary>
		/// <returns>The splitors.</returns>
		/// <param name="targetTypes">Target need split types.</param>
		public ISplitAssetProcessor<object>[] GetSplitors(List<Type> targetTypes)
		{
			List<ISplitAssetProcessor<object>> ret = new List<ISplitAssetProcessor<object>>();

			foreach (var item in targetTypes)
			{
				var splitor = SplitProcesserConfig.Instance.GetProcesser(item);
				if (!ret.Contains(splitor))
					ret.Add(splitor);
			}

			return ret.ToArray();
		}

		public SplitController GenSplitController(string assetPath)
		{
			// 检查是否prefab 或者 场景
			if (assetPath.EndsWith(".prefab", StringComparison.Ordinal))
			{
				GameObject go = AssetDatabase.LoadAssetAtPath<Object>(assetPath) as GameObject;
				var controller = go.GetComponent<SplitController>();
				if (controller == null) controller = go.AddComponent<SplitController>();
				return controller;
			}
			else if (assetPath.EndsWith(".unity", StringComparison.Ordinal))
			{
				var scene = SceneManager.GetSceneByPath(assetPath);
				var sceneRoots = scene.GetRootGameObjects();
				foreach (var go in sceneRoots)
				{
					var ctrl = go.GetComponent<SplitController>();
					if (ctrl != null) return ctrl;
				}

				EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);

				GameObject newgo = new GameObject("_SplitController");
				var controller = newgo.AddComponent<SplitController>();

				return controller;
			}
			else
			{
				throw new Exception("不能分离奇怪的资源:" + assetPath);
			}
		}

		/// <summary>
		/// 输入要分离的对象路径 + 需要分离的对象类型
		/// asset > split > object[]
		/// </summary>
		public string[] DoSplit(string assetPath, List<Type> splitTargetType)
		{
			var splits = GetSplitors(splitTargetType);
			if (splits.Length == 0) return new string[0];

			SplitController splitController = null;
			List<Component> splitComponents = new List<Component>();
			List<ComponentSplitLinker> componentSplitLinkers = new List<ComponentSplitLinker>();
			List<MaterialSplitLinker> materialSplitLinkers = new List<MaterialSplitLinker>();

			// 获取需要分离的组件 
			if (assetPath.EndsWith(".prefab", StringComparison.Ordinal))
			{
				var go = AssetDatabase.LoadAssetAtPath<Object>(assetPath) as GameObject;
				if (go == null)
					throw new Exception("DoSplit assetPath PrefabObject is Loaded with null > " + assetPath);

				// 根据需要分离的类型，获取所有可以分离的对象组件
				splitTargetType
					.ToObservable(Scheduler.Immediate)
					.Do(type =>
					{
						var coms = go.GetComponentsInChildren(type, true);
						coms
						.ToObservable(Scheduler.Immediate)
						.Where(com => splitComponents.Contains(com) == false)
						.Do(com => splitComponents.Add(com))
						.Subscribe();
					})
					.Subscribe();

				//Debug.Log("splitComponent count = " + splitComponents.Count);

				splitController = go.GetOrAddComponent<SplitController>();
			}
			else if (assetPath.EndsWith(".unity", StringComparison.Ordinal))
			{
				// 打开场景
				EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);

				var roots = SceneManager.GetActiveScene().GetRootGameObjects();

				// 根据需要分离的类型，获取所有可以分离的对象组件
				splitTargetType
					.ToObservable(Scheduler.Immediate)
					.Do(type =>
					{
						foreach (var root in roots)
						{
							var coms = root.GetComponentsInChildren(type, true);
							coms
							.ToObservable(Scheduler.Immediate)
							.Where(com => splitComponents.Contains(com) == false)
							.Do(com => splitComponents.Add(com))
							.Subscribe();
						}
					})
					.Subscribe();

				var go = roots.First(obj => obj.name.Equals("SplitController"));
				go = go ?? new GameObject("SplitController");
				splitController = go.GetOrAddComponent<SplitController>();
			}
			else
				throw new Exception("不能分离资源陌生类型:" + assetPath);

			// 分离资源哦！
			// 分离component记录分离后的组件
			splitComponents
				.ToObservable(Scheduler.Immediate)
				.Do(com =>
				{
					var splitor = SplitProcesserConfig.Instance.GetProcesser(com.GetType());
					var assets = splitor.GetAssets(com);
                    splitor.CleanAssets(com);
                    List<string> assetPaths = new List<string>();
                    //bool isError = false;

					// 是否需要对分离的资源进行特殊处理
					// 例如:Mesh
					assets
						.ToObservable(Scheduler.Immediate)
						.Do((obj) =>
						{
							var path = AssetDatabase.GetAssetPath(obj);
							if (obj is Mesh)
							{
								string cpath = "";
								if (cachSplitAssetPath.TryGetValue(obj, out cpath))
								{
									path = cpath;
								}
								else
								{
									var meshName = AssetEditorHelper.ConvertToRequireName("Assets/", path) + obj.name + ".asset";
									path = AssetCachService.CLONE_PATH_PREFIX + "CloneMesh/" + meshName;

									FileInfo fi = new FileInfo(path);
									if (fi.Directory.Exists == false) fi.Directory.Create();
									if (fi.Exists) fi.Delete();

									AssetDatabase.CreateAsset(Object.Instantiate(obj), path);
									AssetDatabase.ImportAsset(path);

									// cach process obj
									cachSplitAssetPath[obj] = path;
								}
							}

                            if (string.IsNullOrEmpty(path) == false)
                            {
                                assetPaths.Add(path);
                            }
                            else
                            {
                                //isError = true;

                                // NOTE:也先加占坑？例如animator的avatar就可能是空的
                                assetPaths.Add(path);

                                //AssetBuilderLogger.LogError("分离" + assetPath + "的" + com.GetType().ToString()
                                //    + "的某个asset是空的path:" + (obj != null ? obj.name : "null"));
                            }
						})
						.Subscribe();

                    //if(isError == false)
                    {
                        componentSplitLinkers.Add(new ComponentSplitLinker() { component = com, linkAssets = assetPaths.ToArray() });
                    }
				})
				.Subscribe();

			// 分离materials记录分离后的材质球信息
			materialSplitLinkers = DoSplitMaterials(assetPath);

			// 生成管理器SplitController
			if (componentSplitLinkers.Count > 0 || materialSplitLinkers.Count > 0)
			{
				splitController.splitComponents = componentSplitLinkers.ToArray();
				splitController.splitMaterials = materialSplitLinkers.ToArray();
			}

			// 保存场景
			if (assetPath.EndsWith(".unity", StringComparison.Ordinal))
			{
				var scene = SceneManager.GetActiveScene();
				EditorSceneManager.SaveScene(scene);
				EditorSceneManager.MarkSceneDirty(scene);
				AssetDatabase.ImportAsset(scene.path);
			}

			return splitController != null ? splitController.GetAllSplitAssetPaths() : new string[0];
		}


		/// <summary>
		/// 负责分离某个对象材质球以及其身上的贴图
		/// 返回这波材质球分离的信息结构索引
		/// </summary>
		internal List<MaterialSplitLinker> DoSplitMaterials(string assetPath)
		{
			//var go = AssetDatabase.LoadAssetAtPath<Object>(assetPath) as GameObject;

			// 返回的材质分离对象缓存结构
			List<MaterialSplitLinker> splitLinkers = new List<MaterialSplitLinker>();

			// get all linker materials
			string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

			List<Material> linkMats = new List<Material>();
			foreach (string dep in dependencies)
			{
				Object obj = AssetDatabase.LoadAssetAtPath<Object>(dep);
				if (obj != null && obj is Material && linkMats.Contains(obj as Material) == false)
				{
					linkMats.Add(obj as Material);
				}
			}

			if (linkMats.Count == 0) return splitLinkers;

			foreach (Material mat in linkMats)
			{
				List<string> properties = new List<string>();
				List<string> textures = new List<string>();

				// 检查这个材质球是否存在贴图
				Shader shader = mat.shader;
				int shaderPropertyCount = ShaderUtil.GetPropertyCount(shader);
				for (int si = 0; si < shaderPropertyCount; si++)
				{
					if (ShaderUtil.GetPropertyType(shader, si) == ShaderUtil.ShaderPropertyType.TexEnv)
					{
						string propertyName = ShaderUtil.GetPropertyName(shader, si);
						Texture tex = mat.GetTexture(propertyName);
						if (tex != null)
						{
							//记录这张贴图
							var path = AssetDatabase.GetAssetPath(tex);

							//TODO 看下是否需要判断这个贴图是否引擎内置的，如果是的话，不需要还原？

							properties.Add(propertyName);
							textures.Add(path);
						}
					}
				}

				if (properties.Count > 0)
				{
					splitLinkers.Add(new MaterialSplitLinker()
					{
						material = mat,
						propertyNames = properties.ToArray(),
						texturePaths = textures.ToArray()
					});
				}
			}

			return splitLinkers;
		}

	}
}

