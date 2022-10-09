using System;
namespace SuperMobs.AssetManager.Loader
{
	public interface ILoader
	{
		object Load(string fileName);
		object LoadImmediate(string fileName);
		object Require();
	}

	public interface ILoader<T> : ILoader
	{
		new object Load(string fileName);
		new object LoadImmediate(string fileName);
		new object Require();
	}
}
