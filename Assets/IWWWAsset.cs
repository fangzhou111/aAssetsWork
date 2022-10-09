using System;
using UnityEngine;
namespace SuperMobs.AssetManager.Assets
{
	public interface IWWWAsset : IDisposable
	{
		void Start();
		bool IsDone { get; set; }
		string error { get; set; }
        bool IsDispose { get; set; }
	}
}
