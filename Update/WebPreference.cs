using System;
using SuperMobs.AssetManager.Core;
using UnityEngine;

namespace SuperMobs.AssetManager.Update
{
	[Serializable]
	public class WebPreference : SuperJsonObject
	{
		/// <summary>
		/// 版本号1.0.1.resVer
		/// </summary>
		public string version = string.Empty;

		/// <summary>
		/// apk、ipa下载链接
		/// </summary>
		public string app = string.Empty;

		/// <summary>
		/// cdn资源链接
		/// </summary>
		public string cdn = string.Empty;

		/// <summary>
		/// 登陆服务器的地址
		/// </summary>
		public string server = string.Empty;

		/// <summary>
		/// 审核版本号
		/// 用来给ios校验当前版本是否用来审核
		/// 如果是，则标记该版本是个审核版本
		/// </summary>
		public string reviewVersion = string.Empty;

		/// <summary>
		/// 审核服务器的地址
		/// </summary>
		public string reviewServer = string.Empty;

		/// <summary>
		/// 扩展参数 保存在全局表里面 > WEB_EXTENSION
		/// </summary>
		public string extension = string.Empty;

		public new void FromJson(string json)
		{
			try
			{
				JsonUtility.FromJsonOverwrite(json, this);
			}
			catch (Exception e)
			{
				// 是否兼容其他格式？
				throw new Exception("这个不是json的格式:" + json + "\nexception=" + e.Message);
			}
		}
	}
}
