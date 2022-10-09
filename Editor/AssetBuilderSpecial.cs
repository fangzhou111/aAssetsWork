namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using UniRx;
	using UnityEditor;
	using System;
	using Object = UnityEngine.Object;
	using SuperMobs.AssetManager.Assets;

	/**
	 * 	提供打包过程中特殊的处理
	 * 
	 * */


	public partial class AssetBuilder
	{

		#region 公用的fbx材质球shader

		Material LoadDefaultFBXMat()
		{
			var materialPath = "Assets/SuperMobs/Resources/default-fbx.mat";
			var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
			return mat;
		}

		void RemoveDefaultFBXMat()
		{
			var mat = LoadDefaultFBXMat();
			if (mat != null)
			{
				mat.shader = null;
			}
		}

		void RestoreDefaultFBXMat()
		{
			var mat = LoadDefaultFBXMat();
			if (mat != null)
			{
				mat.shader = Shader.Find("Standard");
			}
		}

		#endregion
	}
}
