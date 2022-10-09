﻿namespace SuperMobs.AssetManager.Editor
{
	using SuperMobs.AssetManager.Core;
	using SuperMobs.AssetManager.Assets;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using UnityEditor;
	using UnityEngine;
	using UnityEditor.Callbacks;

	[InitializeOnLoad]
	internal class GenSplitProcessorConfig : AssetPostprocessor
	{
		const string CONFIG_FOLDER = "Assets/Resources/";

		static void EditorApplication_DelayCall()
		{
			EditorApplication.delayCall -= EditorApplication_DelayCall;
			RefreshConfig();
		}

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;

			string config = CONFIG_FOLDER + SplitProcesserConfig.SAVE_ASSET_NAME + ".asset";
			foreach (string str in deletedAssets)
			{
				if (str.Equals(config, StringComparison.OrdinalIgnoreCase))
				{
					DelayRefreshConfig();
					return;
				}
			}
		}

		static void DelayRefreshConfig()
		{
			EditorApplication.delayCall += EditorApplication_DelayCall;
		}

		[DidReloadScripts]
		static void RefreshConfig()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			List<Type> allRuntimeTypes = new List<Type>();
			foreach (string path in AssetDatabase.GetAllAssetPaths())
			{
                if (!path.EndsWith(".dll", StringComparison.Ordinal))
					continue;

				try
				{
					allRuntimeTypes.AddRange(Assembly.LoadFile(path).GetExportedTypes());
				}
				catch
				{
					// Debug.LogWarning("[AssetRelation] igonre dll, path = " + path + "!");
				}
			}

			try { allRuntimeTypes.AddRange(Assembly.Load("Assembly-CSharp").GetExportedTypes()); }
			catch { Debug.LogWarning("[AssetRelation] igonre Assembly-CSharp !"); }

			Dictionary<Type, Type> assetRelationTypes = new Dictionary<Type, Type>();
			foreach (Type t in allRuntimeTypes)
			{
				Type interfaceType = t.GetInterface("ISplitAssetProcessor`1");
				if (interfaceType == null)
					continue;
				Type assetType = interfaceType.GetMethod("CleanAssets").GetParameters()[0].ParameterType;
                if (!assetRelationTypes.ContainsKey(assetType))
                {
                    assetRelationTypes.Add(assetType, t);
                }
			}

			List<string> rawAssemblys = new List<string>();
			List<string> rawTypes = new List<string>();
			List<string> processerAssemblys = new List<string>();
			List<string> processerTypes = new List<string>();
			Dictionary<Type, Type>.Enumerator enumerator = assetRelationTypes.GetEnumerator();
			while (enumerator.MoveNext())
			{
				rawAssemblys.Add(enumerator.Current.Key.Assembly.FullName);
				rawTypes.Add(enumerator.Current.Key.FullName);
				processerAssemblys.Add(enumerator.Current.Value.Assembly.FullName);
				processerTypes.Add(enumerator.Current.Value.FullName);
			}
            
			string assetSavePath = CONFIG_FOLDER + SplitProcesserConfig.SAVE_ASSET_NAME + ".asset";
			SplitProcesserConfig config = null;
			if (File.Exists(assetSavePath))
			{
				config = Resources.Load<SplitProcesserConfig>(SplitProcesserConfig.SAVE_ASSET_NAME);
			}

			if(config == null)
			{
				AssetDatabase.DeleteAsset(assetSavePath);

				if (!Directory.Exists(CONFIG_FOLDER)) Directory.CreateDirectory(CONFIG_FOLDER);
				config = ScriptableObject.CreateInstance<SplitProcesserConfig>();
				AssetDatabase.CreateAsset(config, assetSavePath);
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			}

			if (config == null)
			{
				Debug.LogWarning(CONFIG_FOLDER + SplitProcesserConfig.SAVE_ASSET_NAME + ".asset" + " not ready!");
				DelayRefreshConfig();
				return;
			}

			config.assetCompontAssemblys = rawAssemblys.ToArray();
			config.assetCompontTypes = rawTypes.ToArray();
			config.assetCompontProcesserAssemblys = processerAssemblys.ToArray();
			config.assetCompontProcesserTypes = processerTypes.ToArray();
			EditorUtility.SetDirty(config);
			AssetDatabase.SaveAssets();
		}
	}
}

