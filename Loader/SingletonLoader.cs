using System;
using SuperMobs.Core;

namespace SuperMobs.AssetManager.Loader
{
	internal class SingletonLoader<T, R> : AssetLoader<R> where T : AssetLoader<R>
		where R : class
	{
		static T _instance;
		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = Activator.CreateInstance<T>();
					_instance.Init();
				}
				return _instance;
			}
		}

		static readonly object locker = new object();
		int bigFileFlag = -1;
		protected bool isBigFileMode
		{
			get
			{
				lock (locker)
				{
					if (bigFileFlag == -1)
					{
						var bigManifest = Service.Get<LoaderService>().GetBigFileManifest();
						if (bigManifest == null)
						{
							bigFileFlag = 0;
						}
						else
						{
							bigFileFlag = 1;
						}
					}

					return bigFileFlag == 1;
				}
			}
		}

	}
}
