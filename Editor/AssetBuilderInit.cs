using SuperMobs.Core;
namespace SuperMobs.AssetManager.Editor
{
    using UnityEngine;
    using System.Collections.Generic;
    using UniRx;
    using UnityEditor;
    using System;
    using System.Linq;
    using System.Reflection;
    using Object = UnityEngine.Object;

    /*
	 * 打包规则初始化操作
	 * 获取项目中自定义的打包规则脚本
	 * */

    public partial class AssetBuilder
    {
        private string assetKind = string.Empty;

        private AssetBuilder() { }
        public AssetBuilder(Object[] objs, string kind)
        {
            this.assetKind = kind;
            Service.ResetAll();

            InitBuildSources(objs);
            InitService();
            InitAPIs();
        }

        public void Disponse()
        {
            Service.ResetAll();
        }

        void InitBuildSources(Object[] objs)
        {
            if (objs != null)
            {
                foreach (var item in objs)
                {
                    string assetPath = AssetDatabase.GetAssetPath(item);
                    if (!changedSources.Contains(assetPath))
                    {
                        changedSources.Add(assetPath);
                    }
                }
            }
        }

        Type[] GetTargetTypes()
        {
            List<Type> allRuntimeTypes = new List<Type>();
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.EndsWith(".dll", StringComparison.Ordinal) || path.Contains("/Plugins/"))
                    continue;

                try
                {
                    allRuntimeTypes.AddRange(Assembly.LoadFile(path).GetExportedTypes());
                }
                catch
                {
                    Debug.LogWarning("[AssetBuild] igonre dll, path = " + path + "!");
                }
            }

			try
			{
				allRuntimeTypes.AddRange(Assembly.Load("Assembly-CSharp-Editor").GetExportedTypes());
				allRuntimeTypes.AddRange(Assembly.Load("Assembly-CSharp-Editor").GetTypes());
			}
            catch { Debug.LogWarning("[AssetBuild] igonre Assembly-CSharp-Editor !"); }

            //Debug.Log(type.ToString() + "  : " + allRuntimeTypes.Count);
            return allRuntimeTypes.ToArray();
        }

        void InitAPIs()
        {
            var types = GetTargetTypes();

            //Assembly
            //	.GetAssembly(typeof(IOptimizeBuildAsset)).GetTypes()
            types
            .Where(t => t.GetCustomAttributes(typeof(OptimizeBuildAssetBeforeProcessAttribute), false).Length > 0)
            .OrderBy(t => (t.GetCustomAttributes(typeof(OptimizeBuildAssetBeforeProcessAttribute), false)[0] as OptimizeBuildAssetBeforeProcessAttribute).order)
            .AsSafeEnumerable()
            .ToObservable(Scheduler.Immediate)
            .Subscribe(t => preOptimizeBuildSourcesAPIs.Add(Activator.CreateInstance(t) as IOptimizeBuildAsset));

            //Assembly
            //	.GetAssembly(typeof(IProcessBuildAsset)).GetTypes()
            types
                .Where(t => t.GetCustomAttributes(typeof(ProcessBuildAssetAttribute), false).Length > 0)
                .OrderBy(t => (t.GetCustomAttributes(typeof(ProcessBuildAssetAttribute), false)[0] as ProcessBuildAssetAttribute).order)
                .AsSafeEnumerable()
                .ToObservable(Scheduler.Immediate)
                .Subscribe(t => processBuildSourcesAPIs.Add(Activator.CreateInstance(t) as IProcessBuildAsset));

            //Assembly
            //	.GetAssembly(typeof(IGenBuildmap)).GetTypes()
            types
                .Where(t => t.GetCustomAttributes(typeof(GenBuildMapAttribute), false).Length > 0)
                .OrderBy(t => (t.GetCustomAttributes(typeof(GenBuildMapAttribute), false)[0] as GenBuildMapAttribute).order)
                .AsSafeEnumerable()
                .ToObservable(Scheduler.Immediate)
                .Subscribe(t => genbuildmapAPIs.Add(Activator.CreateInstance(t) as IGenBuildmap));

            //Assembly
            //	.GetAssembly(typeof(IBuildAssetBundle)).GetTypes()
            types
                .Where(t => t.GetCustomAttributes(typeof(BuildAssetBundleAttribute), false).Length > 0)
                .OrderBy(t => (t.GetCustomAttributes(typeof(BuildAssetBundleAttribute), false)[0] as BuildAssetBundleAttribute).order)
                .AsSafeEnumerable()
                .ToObservable(Scheduler.Immediate)
                .Subscribe(t => buildAssetAPIs.Add(Activator.CreateInstance(t) as IBuildAssetBundle));

            //Assembly
            //	.GetAssembly(typeof(IProcessAssetRelation)).GetTypes()
            types
                .Where(t => t.GetCustomAttributes(typeof(ProcessAssetRelationAttribute), false).Length > 0)
                .OrderBy(t => (t.GetCustomAttributes(typeof(ProcessAssetRelationAttribute), false)[0] as ProcessAssetRelationAttribute).order)
                .AsSafeEnumerable()
                .ToObservable(Scheduler.Immediate)
                .Subscribe(t => splitBuildSourcesAPIs.Add(Activator.CreateInstance(t) as IProcessAssetRelation));

            AssetBuilderLogger.Log("optimize api count = " + preOptimizeBuildSourcesAPIs.Count + "\n" +
                                 "process api count = " + processBuildSourcesAPIs.Count + "\n" +
                                 "gen buildmap api count = " + genbuildmapAPIs.Count + "\n" +
                                 "build api count = " + buildAssetAPIs.Count + "\n" +
                                 "split api count = " + splitBuildSourcesAPIs.Count);
        }

        void InitService()
        {
            Service.Set<SplitService>(new SplitService());
            Service.Set<AssetCachService>(new AssetCachService());
        }

    }
}