using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
	/*
	 * 输出静态方法操作给CMD外部远程调用
	 * */

	public partial class ShellBuilder
	{
		public static void BuildPlayer()
		{
			ShellBuilder sb = new ShellBuilder();
			sb.RunBuildPlayer();
		}

		public static ShellBuilder CreateWithSetting()
		{
			ShellBuilder sb = new ShellBuilder();
			sb.InitSetting();
			return sb;
		}

		public static void ReadyToBuild()
		{
			ShellBuilder sb = new ShellBuilder();
			sb.InitReadyToBuild();

			// to mark this flag because wont cleanup with custom builded.
			ShellBuilder.cleanup = "false";
		}
	}
}
