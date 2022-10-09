using System;
using System.Collections.Generic;
using System.IO;
using SuperMobs.AssetManager.Core;
using UniRx;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
    /**
     * 为了兼顾editor运行时直接加载某个路径的地图
     * 在运行前先设置一次所有的场景路径到buildsetting里面
     * 不运行就还原
     * */
    [InitializeOnLoad]
    class SuperEditorSceneManager
    {
        const string config = "EDITOR_BUILD_SETTING.config";

        static SuperEditorSceneManager()
        {
            // 当代码编译时候
            if (EditorApplication.isPlaying == false)
                Store();
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged += (mode) =>
            {
                if (mode == PlayModeStateChange.ExitingEditMode)
                {
                    // 准备play
                    // Debug.LogWarning("修改EditorBuildSetting");
                    Play();
                }
                else if (mode == PlayModeStateChange.ExitingPlayMode)
                {
                    // 如果play就还原
                    // Debug.LogWarning("还原EditorBuildSetting");
                    Store();
                }
            };

#else
            EditorApplication.playmodeStateChanged += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        // 准备play
                        //Debug.LogWarning("修改EditorBuildSetting");
                        Play();
                    }
                    else
                    {
                        // 如果play就还原
                        //Debug.LogWarning("还原EditorBuildSetting");
                        Store();
                    }
                }
            };
#endif
        }

        static void Play()
        {
            Dictionary<string, bool> scenes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            EditorBuildSettingsScene[] oldscenes = EditorBuildSettings.scenes;

            // record old scenes setting
            string path = AssetPath.ProjectRoot + config;
            using (StreamWriter writer = new StreamWriter(path, false, System.Text.UTF8Encoding.Default))
            {
                writer.WriteLine("// Record Current Editor Setting Build Scene");
                for (int i = 0; i < oldscenes.Length; i++)
                {
                    var old = oldscenes[i];
                    var str = old.path + " = " + old.enabled;
                    writer.WriteLine(str);
                }
            }

            foreach (var scene in oldscenes)
            {
                scenes[scene.path] = scene.enabled;
            }
            var assetScenes = AssetDatabase.FindAssets("t:Scene");
            foreach (string asset in assetScenes)
            {
                string assetpath = AssetDatabase.GUIDToAssetPath(asset);
                if (assetpath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
                    !scenes.ContainsKey(assetpath))
                    scenes.Add(assetpath, true);
            }
            EditorBuildSettingsScene[] newscenes = new EditorBuildSettingsScene[scenes.Count];
            int index = 0;
            foreach (var scene in scenes)
            {
                newscenes[index++] = new EditorBuildSettingsScene(scene.Key, scene.Value);
            }
            EditorBuildSettings.scenes = newscenes;
        }

        static void Store()
        {
            string path = AssetPath.ProjectRoot + config;
            if (File.Exists(path))
            {
                ByteReader br = new ByteReader(File.ReadAllBytes(path));
                var dict = br.ReadDictionary();
                EditorBuildSettingsScene[] newscenes = new EditorBuildSettingsScene[dict.Count];
                int index = 0;
                foreach (var scene in dict)
                {
                    newscenes[index++] = new EditorBuildSettingsScene(scene.Key, scene.Value.ToLower().Equals("true") ? true : false);
                }
                EditorBuildSettings.scenes = newscenes;
            }
        }
    }
}