using System;
namespace SuperMobs.AssetManager.Assets
{
	/// <summary>
	/// 某个资源打包时候的模式
	/// 独立、依赖用
	/// </summary>
	public enum AssetBuildType
	{
		single = 1 << 1,
		dependence = 1 << 2,
		//rawimage = 2,   // 放弃
		//rawsprite = 3,  // 放弃
		streamab = 1 << 3,
	}

	/// <summary>
	/// Bundle里面资源的类型
	/// 加速加载,也就是可以判断类型，然后接口AssetBundle.Load(typeof(xxx))加速
	/// </summary>
	public enum AssetLoadType : int
	{
		GameObject = 0,
		TextAsset = 1,
		Texture = 2,
		Mesh = 3,
        Sprite = 4,
		Unknown = 9,
	}
}
