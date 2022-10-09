/*
 * @Author: chiuan wei 
 * @Date: 2017-11-21 02:11:21 
 * @Last Modified by:   chiuan wei 
 * @Last Modified time: 2017-11-21 02:11:21 
 */
using System;
using System.Collections.Generic;
using System.IO;
using SuperMobs.AssetManager.Core;
using Object = UnityEngine.Object;
using SuperMobs.AssetManager.Loader;
using SuperMobs.Core;
using UniRx;
using UnityEngine;

namespace SuperMobs.AssetManager.Assets
{
    /// <summary>
    /// 这个是一个Bundle的Asset资源集合
    /// 只存需要打包的目标对象数组
    /// </summary>
    [Serializable]
    public class Bundle : SuperJsonObject, IStreamAsset
    {
        public string bundleName;
        public uint bundleNameCrc;

        // 这个Bundle的打包类型，用于加载时候处理方式
        public AssetBuildType bundleType;

        // 这个Bundle的资源的类型
        // NOTE:单个Bundle所有资源类型一致，加载时候传类型加快速度
        public AssetLoadType assetType;

        // 这个应该是目标对象需要打包的对象
        // 是Asset 里面的 buildPaths
        // 匹配AssetBundle里面的assets
        public string[] assets;

        // 这个bundle的依赖ab列表
        // 就是默认的dependence必须先加载的bundle列表
        public string[] dependencies;

        /* 
         * -------- 以下是不需要序列化的 -----------
         */

        // loaded object in runtime.
        public AssetObject[] objects = null;

        public AssetBundle assetBundle = null;

        private Type type = null;

        // begin load time record.
        internal DateTime d0;

        private HashSet<Bundle> parents = null;

        // 关联的bundles
        private List<Bundle> connectBundles = null;

        // 作为依赖正在加载中的引用，避免在加载异步过程中计数器啥
        public int loadingRefer { get; internal set; }

        // assetPath > objects's index
        private Dictionary<string, int> _assetDict = null;

        private bool isCommBundle = false;

        // 是否已经加载完毕了
        private bool _isLoaded = false;
        public bool isLoaded
        {
            get { return _isLoaded; }
            set
            {
                _isLoaded = value;
                if (value == true)
                {
                    ResetFirstTimeLoaded();
                    timeWhenLoaded = Time.realtimeSinceStartup;
                }
            }
        }

        internal void ResetFirstTimeLoaded()
        {
            firstTimeLoaded = true;
        }

        // TODO 优化这个设置
        internal void AddParent(Bundle b)
        {
            parents = parents ?? new HashSet<Bundle>();

            if (parents.Contains(b) == false)
            {
                parents.Add(b);
            }
        }

        internal bool IsParentAlive()
        {
            if (parents == null) return false;

            foreach (var p in parents)
            {
                // 内部检查不需要充值这个首次
                if (p.GetReferCount(true, false) > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// 获取这个Bundle关联的所有Bundles
        /// NOTE:包含本身在最后一个
        /// </summary>
        internal List<Bundle> GetDependenceBundles()
        {
            if (connectBundles == null)
            {
                connectBundles = Service.Get<AssetManifest>().FindDependenceBundles(this);
            }

            return connectBundles;
        }

        internal Type GetAssetLoadType()
        {
            return type;
        }

        private void UnloadAssetBundleWhenLoadAllAssets()
        {
            if (bundleType == AssetBuildType.single)
            {
                bool isAllLoaed = true;
                for (int i = 0, len = objects.Length; i < len; i++)
                {
                    if (objects[i] == null || objects[i].isLoaded() == false)
                    {
                        isAllLoaed = false;
                        break;
                    }
                }
                if (isAllLoaed)
                {
                    if (assetBundle != null)
                    {
                        assetBundle.Unload(false);
                        assetBundle = null;
                    }
                }
            }
        }

        internal AssetObject ReadyAssetObject(string assetPath, Object obj)
        {
            var asset = ReadyAssetObject(assetPath);
            asset.InitAsset(obj);

            // 需要检查这个对象的Bundle所有资源都准备好了
            // 这个时候可以把AB释放掉
            UnloadAssetBundleWhenLoadAllAssets();

            return asset;
        }

        // 构造ao占坑
        internal AssetObject ReadyAssetObject(string assetPath)
        {
            int i = 0;
            if (_assetDict.TryGetValue(assetPath, out i) == false)
            {
                // new 
                i = _assetDict.Count;
                _assetDict.Add(assetPath, i);
                objects[i] = new AssetObject(this);
            }

            return objects[i];
        }

        /// <summary>
        /// 当加载完后，获取这个Bundle的资源
        /// </summary>
        internal AssetObject LoadObject(string buildPath)
        {
            int index;
            if (isLoaded && _assetDict.TryGetValue(buildPath, out index) && objects[index].isLoaded())
            {
                return objects[index];
            }
            else
            {
                return Service.Get<LoaderService>().LoadAssetObject(this, buildPath);
            }
        }

        /// <summary>
        /// 异步获取某个资源 
        /// 必须加载完这个Bundle后
        /// </summary>
        internal void LoadObject(string buildPath, Action<AssetObject> callback)
        {
            Action<AssetObject> _callback = callback ?? (_ => { AssetLogger.LogWarning("Async LoadObject no callback arg with buildPath = " + buildPath); });

            int index;
            if (isLoaded && _assetDict.TryGetValue(buildPath, out index) && objects[index].isLoaded())
            {
                _callback(objects[index]);
            }
            else
            {
                Service.Get<LoaderService>().LoadAssetObject(this, buildPath, _callback);
            }
        }

        internal void Unload(bool isAppQuit = false)
        {
            if (isLoaded == false) return;

            if (isAppQuit == false && isCommBundle)
            {
                return;
            }

            // wwwLoader = null;
            if (wwwLoadState != null)
            {
                AssetLogger.LogError("Unload Bundle : " + bundleName + " 但是这个Bundle的wwwLoadState不为空!? 还在加载中? 不释放.");
                return;
            }

            if (objects != null)
            {
                foreach (var item in objects)
                {
                    if (item != null)
                        item.Unload();
                }
            }

            if (assetBundle != null)
            {
                assetBundle.Unload(false);
                assetBundle = null;
            }

            if (isAppQuit == false)
            {
                AssetLogger.Log(Color.grey, "Unload Bundle : " + bundleName);
            }

            isLoaded = false;
        }

        #region async loading 

        // 异步资源处理器
        // 负责管理该bundle的异步加载、和资源准备
        internal IWWWAsset wwwProcessor = null;

        // 控制是否已经完成加载了
        Subject<Unit> _wwwLoadState = null;
        public Subject<Unit> wwwLoadState
        {
            get
            {
                return _wwwLoadState;
            }
            private set { }
        }

        public Subject<Unit> CreateWWWObservable()
        {
            if (_wwwLoadState == null)
            {
                _wwwLoadState = new Subject<Unit>();
                _wwwLoadState
                    .DoOnCompleted(() =>
                    {
                        wwwProcessor = null;
                        _wwwLoadState = null;
                    }).Subscribe().AddTo(AssetManager.Instance.gameObject);
            }
            return _wwwLoadState;
        }

        #endregion

        #region reference

        private bool firstTimeLoaded = false;
        private float timeWhenLoaded = 0f;

        // owner references
        private Stack<WeakReference> references = new Stack<WeakReference>();

        internal void AddOwner(Object owner)
        {
            if (owner == null) return;

            WeakReference empty = null;
            foreach (var item in references)
            {
                if ((item.Target as Object) == owner)
                {
                    return;
                }
                else if (empty == null && item.Target == null)
                {
                    empty = item;
                }
            }

            if (empty == null)
            {
                empty = new WeakReference(owner);
                references.Push(empty);
            }
            else
            {
                empty.Target = owner;
            }
        }

        internal void RemoveOwner(Object owner)
        {
            foreach (var item in references)
            {
                if ((item.Target as Object) == owner)
                {
                    item.Target = null;
                    break;
                }
            }
        }

        public int GetReferCount(bool firstTimeCheck, bool needResetFirstTime = false)
        {
            if (loadingRefer > 0)
            {
                return 666;
            }

            if (firstTimeCheck && firstTimeLoaded)
            {
                if (needResetFirstTime) firstTimeLoaded = false;
                return int.MaxValue;
            }

            // 为了低端内存iphone4s??
            //if(CheckIfDependenceABShouldUnload())
            //{
            //    return 0;
            //}

            if (bundleType == AssetBuildType.dependence && IsParentAlive()) return 888;

            int i = 0;
            foreach (var item in references)
            {
                if ((item.Target as Object) != null)
                {
                    i++;
                }
            }
            return i;
        }

        // timeout & not using...
        // TODO optimized memory warnning
        bool CheckIfDependenceABShouldUnload()
        {
            if (AssetManager.memoryLevel == 1 && bundleType == AssetBuildType.dependence)
            {
                if (Time.realtimeSinceStartup - timeWhenLoaded >= 60.0f)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region stream read & write

        public override void ToStream(BinaryWriter bw)
        {
            bw.Write(bundleName);
            bw.Write(bundleNameCrc);
            bw.Write((int) bundleType);
            bw.Write((char) assetType);
            bw.WriteArray(assets);
            bw.WriteArray(dependencies);
        }

        public override void FromStream(BinaryReader br)
        {
            bundleName = br.ReadString();
            bundleNameCrc = br.ReadUInt32();
            bundleType = (AssetBuildType) br.ReadInt32();
            assetType = (AssetLoadType) br.ReadChar();
            assets = br.ReadArrayString();
            dependencies = br.ReadArrayString();

            // init only once.
            switch (assetType)
            {
                case AssetLoadType.GameObject:
                    type = typeof(GameObject);
                    break;
                case AssetLoadType.Mesh:
                    type = typeof(Mesh);
                    break;
                case AssetLoadType.TextAsset:
                    type = typeof(TextAsset);
                    break;
                case AssetLoadType.Texture:
                    type = typeof(Texture);
                    break;
                case AssetLoadType.Sprite:
                    type = typeof(Sprite);
                    break;
                default:
                    type = typeof(Object);
                    break;
            }

            _assetDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            objects = new AssetObject[assets.Length];
            isCommBundle = bundleName.Equals("comm.ab") ? true : false;
        }

        public override string ToString()
        {
            return bundleName + "," + bundleType.ToString() + "," + assetType.ToString() + ",dep:" + dependencies.Length;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    internal static partial class Extension
    {
        public static void ResetFirstTimeLoaded(this List<Bundle> bundles)
        {
            if (bundles == null || bundles.Count <= 0) return;

            foreach (var b in bundles)
            {
                if (b != null)
                    b.ResetFirstTimeLoaded();
            }
        }
    }
}