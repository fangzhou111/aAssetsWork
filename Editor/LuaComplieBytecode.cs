namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using UnityEditor;
	using System;
	using System.Collections;
	using System.IO;
	using System.Diagnostics;
	using Debug = UnityEngine.Debug;
	using System.Collections.Generic;
	using Object = UnityEngine.Object;
	using SuperMobs.AssetManager.Core;

	public class LuaComplieBytecode
	{
		/// <summary>
		/// 编译Lua文件
		/// NOTE:if osx show the luajit is not "exec" program
		/// need do: chmod 755 luajit exec file
		/// </summary>
		public static int DoJit(string sourcePath, string targetPath)
		{
			string dir = AssetPath.ProjectRoot + "Assets/Editor/Luajit/";

			//Debug.LogWarning("-----CompileLuaFile------" + "\n" + sourcePath + "\n" + targetPath);

			ProcessStartInfo info = new ProcessStartInfo();

			///if windows use .exe process
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				info.FileName = dir + "luajit.exe";
			}
			else
			{
				//FIX:ios use luajit2.1
				if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
				{
					info.FileName = dir + "luajit2.1";
				}
				else
				{
					info.FileName = dir + "luajit";
				}
			}

			///setting info
			info.Arguments = "-b -g " + sourcePath + " " + targetPath;
			info.WindowStyle = ProcessWindowStyle.Minimized;
			info.UseShellExecute = false;
			info.RedirectStandardError = true;
			info.WorkingDirectory = dir;

			Process pro = Process.Start(info);
			string error = pro.StandardError.ReadToEnd();
			pro.WaitForExit();

			if (string.IsNullOrEmpty(error) == false)
			{
				throw new Exception("CompileLuaFile:" + error + "\nsourcePath=" + sourcePath + "\ntargetPath=" + targetPath);
			}

			pro.Close();
			//pro.Kill();

			return 0;
		}

		/// <summary>
		/// 把Lua自定义编译，因为ios64并不支持通用32位的bytecode
		/// 所以这里自己把Lua代码进行加密
		/// </summary>
		public static int DoCustom(string sourcePath, string targetPath)
		{
			byte[] bytes = File.ReadAllBytes(sourcePath);

			for (int i = 0; i < bytes.Length; i++)
			{
				//bytes[i] = (byte)(bytes[i] ^ 0x94);
			}

			FileStream file = null;
			file = File.Create(targetPath);
			file.Write(bytes, 0, bytes.Length);
			file.Close();

			return 0;
		}

	}
}