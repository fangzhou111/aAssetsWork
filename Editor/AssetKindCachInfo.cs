using UniRx;
using System.Linq;
namespace SuperMobs.AssetManager.Editor
{
	using UnityEngine;
	using System.Collections.Generic;
	using SuperMobs.AssetManager.Core;
	using System;

	/// <summary>
	/// cach all sources path of same kind of assets.
	/// like: ui
	/// </summary>

	[Serializable]
	public class AssetKindCachInfo : SuperJsonObject
	{
		// assets kind: ui,lua,config...
		public string kind = string.Empty;

		// all sources path
		public string[] sources = new string[0];
	}
}