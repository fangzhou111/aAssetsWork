/*
 * @Author: chiuan wei 
 * @Date: 2017-11-21 00:58:34 
 * @Last Modified by:   chiuan wei 
 * @Last Modified time: 2017-11-21 00:58:34 
 */
using System;
namespace SuperMobs.AssetManager.Loader
{
	public abstract class AssetLoader<T> : ILoader<T>
	{
		// 加载出来的对象
		internal object loadedObject;

		public virtual string error { get; protected set; }

		/// <summary>
		/// 停止这个加载器加载
		/// 一般对异步加载有关系
		/// </summary>
		public virtual void Stop() { throw new System.NotImplementedException(); }
		public virtual bool isDone { get; private set; }

		internal virtual void Init()
		{ }

		public virtual object Load(string fileName)
		{
			throw new NotImplementedException();
		}

		public virtual object LoadImmediate(string fileName)
		{
			throw new NotImplementedException();
		}

		public virtual object Require()
		{
			return loadedObject;
		}
	}
}