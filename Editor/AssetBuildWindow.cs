using UnityEngine;
using UnityEditor;
using System.IO;
using SuperMobs.AssetManager.Core;
using UniRx;
using SuperMobs.AssetManager.Editor;
using System.Collections.Generic;
using System;

public class AssetBuildWindow : EditorWindow
{
	static AssetBuildWindow window;

	string[] platforms = new string[0];
	int currentIndex = -1;

	long currentTotalSize = 0;
	string[] currentKinds = new string[0];
	Dictionary<string, long> kindSizes = new Dictionary<string, long>();

	float mbseed = 1.0f / (1024 * 1024);
	List<IDisposable> disposables = new List<IDisposable>();

	[MenuItem("SuperMobs/Utils/AssetBuildWindow")]
	static void ShowWindow()
	{
		window = (AssetBuildWindow)EditorWindow.GetWindow(typeof(AssetBuildWindow), true, "Asset Builded Window", true);
		window.minSize = new Vector2(600, 400);
		window.maxSize = new Vector2(600, 400);
		window.Init();
		window.Show();
	}

	void OnDestroy()
	{
		for (int i = 0; i < disposables.Count; i++)
		{
			disposables[i].Dispose();
		}
		disposables.Clear();

		//EditorApplication.update -= window.Repaint;
	}

	void Init()
	{
		InitExistPlatformAssets();

		//EditorApplication.update += window.Repaint;
	}

	/// <summary>
	/// 从assetbundles目录检查存在的好的资源
	/// </summary>
	void InitExistPlatformAssets()
	{
		var dir = AssetPath.ProjectRoot + "AssetBundles/";
		DirectoryInfo di = new DirectoryInfo(dir);
		var dirs = di.GetDirectories();
		foreach (var item in dirs)
		{
			if (item.Name.StartsWith(".", System.StringComparison.OrdinalIgnoreCase)) continue;

			//check somtthing is builded.
			var cached = dir + item.Name + "/CachedAssets/";
			var files = AssetEditorHelper.CollectAllPath(cached, "*.info");
			if (files.Length > 0)
			{
				platforms = platforms.AddSafe(item.Name);
			}
		}
		Debug.Log("exist platform assets builded at :" + dir + "\n" + platforms.ToArrayString());
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal();

		GUILayout.Label("选择查看的平台资源:", GUILayout.Width(200), GUILayout.Height(50));
		int current = GUILayout.SelectionGrid(currentIndex, platforms, platforms.Length, GUILayout.Width(100 * platforms.Length), GUILayout.Height(50));
		if (currentIndex != current)
		{
			currentIndex = current;
			Refresh();
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginVertical();

		GUILayout.Space(20);

		GUILayout.BeginHorizontal();
		{
			GUILayout.Label("全部ab大小=", GUILayout.Width(80));
			GUILayout.Label((currentTotalSize * mbseed).ToString("F2") + "mb");
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		foreach (var kind in currentKinds)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label(kind + "=", GUILayout.Width(80));
				if (kindSizes.ContainsKey(kind))
					GUILayout.Label((kindSizes[kind] * mbseed).ToString("F2") + "mb");
			}
			GUILayout.EndHorizontal();
		}

		GUILayout.EndVertical();
	}

	void Refresh()
	{
		var path = AssetPath.ProjectRoot + "AssetBundles/" + platforms[currentIndex];

		currentKinds = new string[0];
		kindSizes.Clear();
		for (int i = 0; i < disposables.Count; i++)
		{
			disposables[i].Dispose();
		}
		disposables.Clear();

		long total = 0;
		var abs = AssetEditorHelper.CollectAllPath(path, "*.ab");
		abs
			.ToObservable(Scheduler.Immediate)
			.Do(ab =>
			{
				FileInfo fi = new FileInfo(ab);
				total += fi.Length;
			})
			.TakeLast(1)
			.Do(_ =>
			{
				currentTotalSize = total;
			})
			.Subscribe();

		//List<AssetCachInfo> infoList = new List<AssetCachInfo>();
		//var files = AssetEditorHelper.CollectAllPath(path + "/CachedAssets", "*.info");
		//files
		//	.ToObservable(Scheduler.ThreadPool)
		//	.Do(file =>
		//	{
		//		AssetCachInfo info = new AssetCachInfo();
		//		info.FromBytes(File.ReadAllBytes(file));
		//		infoList.Add(info);
		//	})
		//	.Subscribe();

		var kindInfos = AssetEditorHelper.CollectAllPath(path + "/CachedAssets", "*.kind");
		foreach (var k in kindInfos)
		{
			FileInfo fi = new FileInfo(k);
			var kind = fi.Name.Substring(0, fi.Name.LastIndexOf('.'));
			currentKinds = currentKinds.AddSafe(kind);

			var kindInfo = new AssetKindCachInfo();
			kindInfo.FromBytes(File.ReadAllBytes(k));

			List<string> usedAB = new List<string>();
			List<string> used = new List<string>();

			var d = kindInfo.sources
					.ToObservable(Scheduler.ThreadPool)
					.Do(source =>
					{
						var infoPath = FindCachInfoBySourcePath(path + "/CachedAssets", source);
						var cachInfo = new AssetCachInfo();
						cachInfo.FromBytes(File.ReadAllBytes(infoPath));

						var ab = path + "/" + cachInfo.buildBundleNames[0];
						if (kindSizes.ContainsKey(kindInfo.kind) == false) kindSizes[kindInfo.kind] = 0;
						if (usedAB.Contains(ab) == false)
						{
							usedAB.Add(ab);
							kindSizes[kindInfo.kind] += new FileInfo(ab).Length;
						}

						foreach (var link in cachInfo.linkSourcePaths)
						{
							if (used.Contains(link)) continue;
							used.Add(link);
							var infoPath2 = FindCachInfoBySourcePath(path + "/CachedAssets", link);
							var cachInfo2 = new AssetCachInfo();
							cachInfo2.FromBytes(File.ReadAllBytes(infoPath2));
							var ab2 = path + "/" + cachInfo2.buildBundleNames[0];
							if (usedAB.Contains(ab2) == false)
							{
								usedAB.Add(ab2);
								kindSizes[kindInfo.kind] += new FileInfo(ab2).Length;
							}
						}
					})
					.TakeLast(1)
					.Do(_ => Debug.Log("计算" + kindInfo.kind + "完毕!"))
					//.DoOnError(ex => Debug.LogError("error:" + ex.Message))
					.DoOnTerminate(() => Debug.Log("计算" + kindInfo.kind + "Terminate!"))
					.Subscribe();

			disposables.Add(d);
		}


	}

	string FindCachInfoBySourcePath(string folder, string source)
	{
		var infoPaths = AssetEditorHelper.CollectAllPath(folder, Crc32.GetStringCRC32(source) + ".*.info");
		if (infoPaths.Length <= 0)
		{
			Debug.LogError("wtf " + source + " found nothing info : " + Crc32.GetStringCRC32(source));
		}
		return infoPaths[0];
	}

}
