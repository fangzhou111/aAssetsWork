using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SuperMobs.AssetManager.Assets
{
	[Serializable]
	public class SplitProcesserConfig : ScriptableObject
	{
		public const string SAVE_ASSET_NAME = "asset_split_type_config";

		class MiddleProcesser<T1, T2> : ISplitAssetProcessor<T1> where T1 : class, T2
			where T2 : class
		{
			private ISplitAssetProcessor<T2> realProcesser;
			public MiddleProcesser(ISplitAssetProcessor<T2> processer) { realProcesser = processer; }
			public void CleanAssets(T1 obj) { realProcesser.CleanAssets(obj); }
			public UnityEngine.Object[] GetAssets(T1 obj) { return realProcesser.GetAssets(obj); }
			public void SetAssets(T1 obj, UnityEngine.Object[] assets) { realProcesser.SetAssets(obj, assets); }
		}

		class MiddleProcesser<T> : ISplitAssetProcessor<object> where T : class
		{
			private ISplitAssetProcessor<T> realProcesser;
			public MiddleProcesser(ISplitAssetProcessor<T> processer) { realProcesser = processer; }
			public void CleanAssets(object obj) { realProcesser.CleanAssets(obj as T); }
			public UnityEngine.Object[] GetAssets(object obj) { return realProcesser.GetAssets(obj as T); }
			public void SetAssets(object obj, UnityEngine.Object[] assets) { realProcesser.SetAssets(obj as T, assets); }
		}

		private static SplitProcesserConfig m_instance = null;
		public static SplitProcesserConfig Instance
		{
			get
			{

				if (m_instance == null)
				{
					m_instance = Resources.Load<SplitProcesserConfig>(SAVE_ASSET_NAME);
					m_instance.Init();
				}
				return m_instance;
			}
		}

		public string[] assetCompontAssemblys;
		public string[] assetCompontTypes;
		public string[] assetCompontProcesserAssemblys;
		public string[] assetCompontProcesserTypes;

		private Dictionary<Type, object> m_processers = null;
		private Dictionary<Type, ISplitAssetProcessor<object>> m_commonProcessers = null;

		private void Init()
		{
			m_processers = new Dictionary<Type, object>();
			m_commonProcessers = new Dictionary<Type, ISplitAssetProcessor<object>>();

			SplitProcesserConfig types = Resources.Load<SplitProcesserConfig>(SAVE_ASSET_NAME);
			for (int i = 0; i < types.assetCompontTypes.Length; i++)
				m_processers.Add(
					Assembly.Load(types.assetCompontAssemblys[i]).GetType(types.assetCompontTypes[i]),
					Assembly.Load(types.assetCompontProcesserAssemblys[i]).CreateInstance(types.assetCompontProcesserTypes[i])
					);
		}

		public ISplitAssetProcessor<object> GetProcesser(Type t)
		{
			if (!m_commonProcessers.ContainsKey(t))
			{
				object rawProcesser = GetProcesserInternal(t);
				if (rawProcesser == null)
					m_commonProcessers.Add(t, null);
				else
					m_commonProcessers.Add(t, Activator.CreateInstance(typeof(MiddleProcesser<>).MakeGenericType(t),
																	rawProcesser) as ISplitAssetProcessor<object>);
			}
			return m_commonProcessers[t];
		}

		public ISplitAssetProcessor<T> GetProcesser<T>() where T : class
		{
			object processer = GetProcesserInternal(typeof(T));
			if (processer == null)
				return null;
			return processer as ISplitAssetProcessor<T>;
		}

		private object GetProcesserInternal(Type t)
		{
			if (m_processers.ContainsKey(t))
				return m_processers[t];

			Type baseType = t;
			while ((baseType = baseType.BaseType) != null)
			{
				if (!m_processers.ContainsKey(baseType))
					continue;
				m_processers.Add(t, Activator.CreateInstance(typeof(MiddleProcesser<,>)
					.MakeGenericType(t, baseType), m_processers[baseType]));
				return m_processers[t];
			}

			m_processers.Add(t, null);
			return null;
		}

		public IEnumerator<Type> GetAssetTypeEnumerator()
		{
			foreach (Type type in m_processers.Keys)
				yield return type;
		}
	}
}

