namespace SuperMobs.AssetManager.Editor
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using SuperMobs.AssetManager.Assets;
	using SuperMobs.AssetManager.Core;
	using UnityEditor;
	using UnityEngine;

	public class PackBigFile
	{
		public List<BigFileInfo> fileInfos = new List<BigFileInfo>();
		BinaryWriter bw = null;
		long writeIndex = 0;

		public PackBigFile(string bigFilePath)
		{
			bw = new BinaryWriter(File.OpenWrite(bigFilePath));
		}

		~PackBigFile()
		{
			CloseStream(bw);
		}

		public void CloseStream(BinaryWriter writer)
		{
			if (writer != null)
			{
				try
				{
					writer.Flush();
					writer.BaseStream.Close();
					writer.Close();
				}
				catch (Exception) { }
			}
		}

		/// <summary>
		/// 把某个文件写入大文件
		/// </summary>
		public void WriteFile(string sourceFile, uint fileID)
		{
			if (!File.Exists(sourceFile))
				throw new Exception("写入大文件不存在：" + sourceFile);

			FileInfo fi = new FileInfo(sourceFile);

			writeIndex = bw.BaseStream.Position;

			bw.Write(File.ReadAllBytes(sourceFile));

			BigFileInfo bigInfo = new BigFileInfo();
			bigInfo.id = fileID;
			bigInfo.beginIndex = (ulong)writeIndex;
			bigInfo.length = (int)fi.Length;
			fileInfos.Add(bigInfo);
		}

		/// <summary>
		/// 保存大文件信息文件
		/// </summary>
		public void WriteBigFileManifest(string filePath)
		{
			CloseStream(bw);

			BigFileManifest pbf = new BigFileManifest();
			pbf.fileInfos = fileInfos.ToArray();

			FileInfo fi = new FileInfo(filePath);
			if (fi.Exists)
			{
				fi.Delete();
			}

			FileStream fs = new FileStream(filePath, FileMode.Create);
			BinaryWriter w = new BinaryWriter(fs);
			pbf.ToStream(w);
			CloseStream(w);

			// build assetbundle压缩
			string assetPath = Application.dataPath + "/" + AssetPath.BIG_FILE_MANIFEST + ".bytes";
			if (File.Exists(assetPath)) File.Delete(assetPath);
			File.Move(filePath, assetPath);
			AssetDatabase.ImportAsset("Assets/" + AssetPath.BIG_FILE_MANIFEST + ".bytes", ImportAssetOptions.ForceSynchronousImport);
			AssetBundleBuild abb = new AssetBundleBuild();
			abb.assetBundleName = fi.Name;
			abb.assetNames = new string[] { "Assets/" + AssetPath.BIG_FILE_MANIFEST + ".bytes" };
			BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.DisableWriteTypeTree;
			BuildPipeline.BuildAssetBundles(fi.DirectoryName, new AssetBundleBuild[] { abb }, assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

			// cleanup
			AssetEditorHelper.CleanUnityBuildAssetBundleManifest(fi.DirectoryName);
			if (File.Exists(assetPath)) { File.Delete(assetPath); File.Delete(assetPath + ".meta"); }
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}
	}
}
