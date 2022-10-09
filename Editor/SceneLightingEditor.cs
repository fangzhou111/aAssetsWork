#pragma warning disable 0162

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SuperMobs.AssetManager.Assets;
using UnityEngine.SceneManagement;
using SuperMobs.AssetManager.Core;
using System.IO;
using UniRx;
using UnityEditor.SceneManagement;

namespace SuperMobs.AssetManager.Editor
{
	/// <summary>
	/// 提供自动生成有光照渲染的场景LightingData信息保存
	/// </summary>
	public class SceneLightingEditor : AssetPostprocessor
	{
		const string SCENE_LIGHTING_MAP_GO_NAME = "SceneLightingMap";

		public static bool IsLightingData(string assetPath)
		{
			var light = AssetDatabase.LoadAssetAtPath<SceneLightingData>(assetPath);
			return light != null;
		}

		static SceneLightingMap CreateLightingMapForScene()
		{
			var roots = SceneManager.GetActiveScene().GetRootGameObjects();
			GameObject go = null;
			foreach (var root in roots)
			{
				if (root.name.Equals(SCENE_LIGHTING_MAP_GO_NAME, StringComparison.Ordinal))
				{
					go = root;
					break;
				}
			}
			go = go ?? new GameObject(SCENE_LIGHTING_MAP_GO_NAME);
			return go.GetOrAddComponent<SceneLightingMap>();
		}


		/// <summary>
		/// 生成这个场景的lightingdata数据缓存 
		/// </summary>
		public static void GenerateCurrentSceneLightingData()
		{
			if (Lightmapping.lightingDataAsset == null)
			{
				// check if scene under Map folder
				var scene = EditorSceneManager.GetActiveScene();
				if (scene.path.StartsWith("Assets/Map", StringComparison.Ordinal) == false)
				{
					//Debug.LogWarning("save with scene without lightingmap data : " + scene.path);
					return;
				}

				AssetBuilderLogger.Log(Color.blue, "[SceneLightings] 当前场景没有烘焙数据!不自动保存Lighting数据:" + scene.path);
				return;
			}

			var setting = CreateLightingMapForScene();
			if (setting == null)
			{
				return;
			}

			List<Renderer> renders = new List<Renderer>();
			List<GameObject> renderGameObjects = new List<GameObject>();
			List<GameObject> rootGameObjects = new List<GameObject>();
			SceneManager.GetActiveScene().GetRootGameObjects(rootGameObjects);

			foreach (GameObject root in rootGameObjects)
			{
				foreach (Renderer ren in root.GetComponentsInChildren<Renderer>())
				{
					if (!ren.gameObject.isStatic)
						continue;
					renderGameObjects.Add(ren.gameObject);
					renders.Add(ren);
				}
			}

			setting.staticRenderGameObjects = renderGameObjects.ToArray();
			setting.staticRenders = renders.ToArray();
			//EditorUtility.SetDirty(setting);

			string assetpath = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset).Replace(".asset", "_runtime.asset");
			SceneLightingData data;
			if (File.Exists(assetpath))
			{
				data = AssetDatabase.LoadAssetAtPath<SceneLightingData>(assetpath);
			}
			else
			{
				data = ScriptableObject.CreateInstance<SceneLightingData>();
				AssetDatabase.CreateAsset(data, assetpath);
				AssetDatabase.ImportAsset(assetpath);
			}

			// record the default lighting data path
			setting.lightingDataPath = assetpath;

			// record the data			
			data.Record(setting.staticRenders);

			EditorUtility.SetDirty(data);
			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			AssetDatabase.SaveAssets();
		}


		#region Auto generate the scene ligthing data 

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;

			if (AssetPreference.LIGHTING_DATA_AUTO_SAVE == false) return;

			if (importedAssets != null &&
			   importedAssets.Length == 1 &&
			   importedAssets[0].EndsWith(".unity", StringComparison.Ordinal))
			{
				bool createNew;
				using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, "我正在搞事情", out createNew))
				{
					if (createNew)
					{
						GenerateCurrentSceneLightingData();
						mutex.ReleaseMutex();
					}
				}
			}
		}

		#endregion
	}
}
