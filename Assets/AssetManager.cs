/*
 * @Author: chiuan wei 
 * @Date: 2017-05-08 16:55:30 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-11-21 02:36:32
 */
//#define MEMORY_LOW_SIMULATE

using System;
using System.Collections.Generic;
using System.IO;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Loader;
using SuperMobs.Core;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace SuperMobs.AssetManager.Assets
{
    public class AssetManager : MonoBehaviour
    {

        #region singleton

        private static object _lock = new object();
        private static AssetManager _instance;
        public static AssetManager Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<AssetManager>();
                        singleton.name = "(singleton) AssetManager";

                        // init the asset manager
                        _instance.Init();

                        DontDestroyOnLoad(singleton);
                    }

                    return _instance;
                }
            }
        }

        static bool applicationIsQuitting = false;

        void OnDestroy()
        {
            UnloadAllBundles();
            Service.ResetAll();
            _instance = null;
        }

        void Update()
        {
            AutoUnloadBundle();
        }

        #endregion

        #region Init

        // 自动释放定时回调
        public class AssetGCEvent : UnityEvent { };
        /// <summary>
        /// 可让外部监听gcEvent事件 
        /// </summary>
        public static AssetGCEvent gcEvent = new AssetGCEvent();

        /// <summary>
        /// 自动执行 gc collect 
        /// 在内存级别低的时候
        /// </summary>
        public static bool gcAutoCollect = false;

        private void Init()
        {
            LogAssetManager();

            if (Service.IsSet<LoaderService>() == false)
            {
                Service.Set<LoaderService>(new LoaderService());
            }

            if (Service.IsSet<AssetManifest>() == false)
            {
                Service.Set<AssetManifest>(manifest);
            }

            // 安卓服务相关的初始化
            Android.Init();

            // auto release
            // Observable
            //	.IntervalFrame(600, FrameCountType.EndOfFrame)
            //	.Repeat()
            //	//.Do(_ => AssetLogger.Log("running unload unuse bundles"))
            //	.Do(_ => UnloadUnuseBundles())
            //	.TakeUntilDestroy(this.gameObject)
            //	.Subscribe();

            Observable
                .Interval(TimeSpan.FromSeconds(60))
                .Repeat()
                .Do(_ =>
                {
                    if (memoryLevel <= 1)
                    {
                        if (gcEvent != null) gcEvent.Invoke();

                        if (gcAutoCollect)
                        {
                            Resources.UnloadUnusedAssets();
                            System.GC.Collect(0);
                        }
                    }
                })
                .TakeUntilDestroy(this.gameObject)
                .Subscribe()
                .AddTo(this);

            // 初始化加载comm
            LoadCommAb();

            // init memorylevel
            InitMemoryLevel();
        }

        void LogAssetManager()
        {
            AssetLogger.Log(Color.grey, "[AssetManager Setting] isEditorAndNotSimulate : " + AssetPreference.isEditorAndNotSimulate);
        }

        #endregion

        #region manifest

        AssetManifest _manifest = null;
        public AssetManifest manifest
        {
            get
            {
                if (_manifest == null)
                {
                    DateTime d0 = DateTime.Now;
                    _manifest = Service.Get<LoaderService>().GetAssetManifestLoader().Load(string.Empty) as AssetManifest;
                    AssetLogger.Log(Color.grey, "load manifest cost " + (DateTime.Now - d0).TotalSeconds.ToString("F3") + "s");
                }
                return _manifest;
            }
        }

        private void LoadCommAb()
        {
            if (manifest == null) return;
            var bundle = manifest.FindBundle("comm" + AssetPath.ASSETBUNDLE_SUFFIX);
            if (bundle == null)
            {
                AssetLogger.LogWarning("this app dont exist comm.ab ,it's ok if you dont need this.");
            }
            else
            {
                DateTime d0 = DateTime.Now;
                LoadBundle(bundle);
                Shader.WarmupAllShaders();
                AssetLogger.Log(Color.grey, "load comm.ab cost " + (DateTime.Now - d0).TotalSeconds.ToString("F3") + "s");
            }
        }

        #endregion

        #region load asset

        public bool Exist(string sourcePath)
        {
            return manifest != null && manifest.FindAsset(sourcePath) != null;
        }

        public AssetObject Load(string sourcePath)
        {
            if (manifest == null) return null;
            var asset = manifest.FindAsset(sourcePath);
            string buildPath;
            var bundle = manifest.FindBundle(ref asset, out buildPath);
            if (bundle != null)
            {

                if (bundle.isLoaded == false)
                {
                    // return [dependence,self]
                    var bundles = manifest.FindDependenceBundles(bundle);
                    bundles.ResetFirstTimeLoaded();
                    Service.Get<LoaderService>().LoadBundlesSync(bundles);
                }

                // 独立关联的assets
                // 注意：这里因为同一个bundle即使加载了，当时里面某个资源索引相关的独立资源并没有加载
                foreach (var linkAsset in asset.linkSingleAssets)
                {
                    Load(linkAsset);
                }

                // if isLoaded
                return bundle.LoadObject(buildPath);
            }
            else
            {
                AssetLogger.LogError("Sync Load cant found bundle with sourcePath : " + sourcePath);
                return null;
            }
        }

        /// <summary>
        /// 根据BundleName来加载
        /// </summary>
        // Bundle LoadBundle(string bundleName)
        // {
        //    if (manifest == null) return null;
        //    var bundle = manifest.FindBundle(bundleName);
        //    if (bundle == null)
        //    {
        //       AssetLogger.LogError("Sync LoadBundle cant found bundle : " + bundleName);
        //       return null;
        //    }
        //    return LoadBundle(bundle);
        // }

        /// <summary>
        /// 直接load某个Bundle
        /// </summary>
        Bundle LoadBundle(Bundle bundle)
        {
            if (bundle != null)
            {
                if (bundle.isLoaded == false)
                {
                    var bundles = manifest.FindDependenceBundles(bundle);
                    bundles.ResetFirstTimeLoaded();
                    Service.Get<LoaderService>().LoadBundlesSync(bundles);
                }

                // if isLoaded
                return bundle;
            }
            else
            {
                return null;
            }
        }

        UniRx.IObservable<Unit> CreateAsyncLoadObservable(string sourcePath)
        {
            Subject<Unit> sb = new Subject<Unit>();
            //    sb.DoOnError(ex => AssetLogger.LogError("Async Load with sub asset exception :" + ex));
            AsyncLoad(sourcePath, _ => sb.OnCompleted());

            return sb;
        }

        /// <summary>
        /// 异步加载
        /// </summary>
        void AsyncLoad(string sourcePath, Action<AssetObject> callback)
        {
            // protected the callback null
            Action<AssetObject> _callback = callback ?? ((_) => AssetLogger.LogWarning("Async no callback arg with source = " + sourcePath));

            if (manifest == null) goto END;

            var asset = manifest.FindAsset(sourcePath);
            if (asset == null)
            {
                AssetLogger.LogError("Async Load cant FindAsset " + sourcePath);
                goto END;
            }

            string buildPath;
            var bundle = manifest.FindBundle(ref asset, out buildPath);
            if (bundle != null)
            {
                List<UniRx.IObservable<Unit>> obs = new List<UniRx.IObservable<Unit>>();

                // 依赖的bundles
                if (bundle.isLoaded == false)
                {
                    var bundles = manifest.FindDependenceBundles(bundle);
                    bundles.ResetFirstTimeLoaded();

                    var obb = Service.Get<LoaderService>().LoadBundlesAsync(bundles);
                    obs.Add(obb);
                }

                // 独立的关联的bundles
                // 注意：这里因为同一个bundle即使加载了，当时里面某个资源索引相关的独立资源并没有加载
                foreach (var link in asset.linkSingleAssets)
                {
                    obs.Add(CreateAsyncLoadObservable(link));
                }

                // 给异步加载的所有bundle增加异步标签
                manifest.EachBundleForAsset(asset, bundle, b =>
                {
                    b.loadingRefer++;
                });

                // 以上所有的ob将监听
                // 完成才算完成回调
                Observable
                    .WhenAll(obs)
                    .ObserveOn(Scheduler.MainThread)
                    .DoOnTerminate(() =>
                    {
                        // 同步加载ao
                        // _callback(bundle.LoadObject(buildPath));
                        // // 完成释放时候异步标签--
                        // manifest.EachBundleForAsset(asset, bundle, b =>
                        // {
                        //     b.loadingRefer--;
                        // });

                        // 异步加载ao
                        bundle.LoadObject(buildPath, ao =>
                        {
                            _callback(ao);
                            // 完成释放时候异步标签--
                            manifest.EachBundleForAsset(asset, bundle, b =>
                            {
                                b.loadingRefer--;
                            });
                        });
                    })
                    .Subscribe()
                    .AddTo(this);

                // DONT NEED TO GO END!
                return;
            }
            else
            {
                AssetLogger.LogError("Async Load cant found any bundle for " + sourcePath);
            }

            END:
            _callback(null);
        }

        public void Load(string sourcePath, Action<AssetObject> callback)
        {
            AsyncLoad(sourcePath, callback);
        }

        #endregion

        #region unload asset

        /// <summary>
        /// 检查没有存在引用的Bundle释放掉
        /// </summary>
        public void UnloadUnuseBundles(bool firstTimeCheck = true)
        {
            var m = Service.Get<AssetManifest>();
            if (m == null) return;

            var c = m.GetBundleCollector().GetCurrentAssetCollection();
            c
                // .ToObservable(firstTimeCheck ? Scheduler.CurrentThread : Scheduler.Immediate)
                .ToObservable(Scheduler.MainThread)
                .Where(bundle => bundle.isLoaded)
                .Do(bundle =>
                {
                    // 是否需要检查loading时候的计数器引用
                    if (bundle.GetReferCount(firstTimeCheck, true) <= 0)
                    {
                        bundle.Unload();
                    }
                })
                .TakeLast(1)
                .Do(_ =>
                {
                    if (gcAutoCollect)
                    {
                        Resources.UnloadUnusedAssets();
                        System.GC.Collect();
                    }
                })
                .Subscribe();
        }

        /// <summary>
        /// Unloads all bundles without no reason
        /// use for assetmanager disable or reset when is needed.
        /// </summary>
        void UnloadAllBundles()
        {
            var m = Service.Get<AssetManifest>();
            if (m == null) return;

            var b = m.GetBundleCollector();
            var c = b.GetCurrentAssetCollection();
            c
                .ToObservable(Scheduler.Immediate)
                .Where(bundle => bundle.isLoaded)
                .Do(bundle =>
                {
                    bundle.Unload(true);
                })
                .Subscribe();
        }

        #endregion

        #region 检查是否Bundle不存在引用了

        public static int memoryLevel = 1;

        const float MEMORY_DIVIDER = 1.0f / 1048576; // 1024^2

        public long getCurrntAvailableMemorySize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Service.Get<Android>().GetMemory();
#elif UNITY_IOS
            return (long) (SystemInfo.systemMemorySize * 0.55f);
#else
            return (long)(SystemInfo.systemMemorySize * 0.5f);
#endif
        }

        //const float MEMORY_DIVIDER = 1.0f / 1048576; // 1024^2
        float _last_time_get_availableMem = -100.0f;
        long memAva = 0;
        public float GetAvailableMemorySize(bool refresh = false)
        {
            if (refresh || Time.realtimeSinceStartup - _last_time_get_availableMem > 10.0f)
            {
                _last_time_get_availableMem = Time.realtimeSinceStartup;
                memAva = getCurrntAvailableMemorySize();
            }

            //return mem * MEMORY_DIVIDER;
            return memAva;
        }

        private void CheckMemoryWarning(bool refreshMEM = false)
        {
            float mem = GetAvailableMemorySize();
            if (mem <= 150.0f)
            {
                //AssetLogger.LogWarning("内存在150以下啦...");
            }
        }

        private void InitMemoryLevel()
        {
            float mb = GetAvailableMemorySize();
            int level = 1;

            if (mb >= 300.0f && mb <= 500.0f)
            {
                level = 2;
            }
            else if (mb > 500f)
            {
                level = 3;
            }

            memoryLevel = level;

#if MEMORY_LOW_SIMULATE
            memoryLevel = 1;
#endif

            PlayerPrefs.SetInt("MEM_LEVEL", level);

            // 根据不同mem级别初始化一些释放的参数
            if (memoryLevel == 3)
            {
                // 内存多的话，检查慢一点，释放数量多一丢丢
                auto_check_interval = 10.0f;
                MAX_UNLOAD_BUNDLE = 20;
            }
            else if (memoryLevel == 1)
            {
                auto_check_interval = 1.0f;
                MAX_UNLOAD_BUNDLE = 10;
            }
        }

        int MAX_UNLOAD_BUNDLE = 20;
        int auto_unload_bundle_count = 0;

        float auto_check_interval = 5f;
        float auto_timer = 0f;

        int auto_bundle_check_index = 0;
        const int MAX_BUNDLE_CHECK_COUNT = 300;

        /// <summary>
        /// 自动检查是否有bundle已经不用了
        /// 需要释放掉
        /// </summary>
        void AutoUnloadBundle()
        {
            // time to call interval control
            auto_timer += Time.deltaTime;
            if (auto_timer < auto_check_interval) return;
            else
            {
                auto_timer = 0f;
            }

            var m = Service.Get<AssetManifest>();
            if (m == null) return;

            var c = m.GetBundleCollector().GetCurrentAssetCollection();

            // protected memory
            CheckMemoryWarning();

            //int temp_check_count = 0;
            if (auto_bundle_check_index >= c.Count)
            {
                auto_bundle_check_index = 0;
            }

            // bool _memEnough = GetAvailableMemorySize() > 500.0f ? true : false;

            //int temp_index = 0;
            auto_unload_bundle_count = 0;

            foreach (var b in c)
            {
                // for 过掉前面的...
                //if (temp_index < auto_bundle_check_index)
                //{
                //	temp_index++;
                //	continue;
                //}
                //auto_bundle_check_index++;
                //temp_check_count++;

                // check if const time too long not used.
                //if (!_memEnough)
                //{
                //	_assetCollector.bundles[i].CheckConstUsedTimeoutAndRemoveOwner();
                //}

                if (b.isLoaded && b.GetReferCount(true, true) <= 0)
                {
                    b.Unload();

                    auto_unload_bundle_count++;
                    if (auto_unload_bundle_count >= MAX_UNLOAD_BUNDLE)
                    {
                        auto_unload_bundle_count = 0;
                        return;
                    }
                }

                //if (temp_check_count >= MAX_BUNDLE_CHECK_COUNT)
                //{
                //    break;
                //}

            }

            // 一次最多处理20个
            auto_unload_bundle_count = 0;
        }

        #endregion

    }
}