/*
 * @Author: chiuan wei 
 * @Date: 2017-07-06 15:23:26 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-06 15:25:24
 */
using System;
using System.Collections.Generic;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Package;
using UniRx;
using UnityEngine;

namespace SuperMobs.AssetManager.Loader
{
   /// <summary>
   /// 提供加载器服务
   /// 配置加载器等工作
   /// </summary>
   public class LoaderService
   {
      // 同步bundle加载器
      BundleSync bundleSync;

      // 异步bundle加载器
      BundleAsync bundleAsync;

      #region Init
      public LoaderService()
      {
         bundleSync = new BundleSync();
         bundleAsync = new BundleAsync();
      }

      #endregion

      #region big file system

      bool _bigFileLoaded = false;
      BigFileManifest _bigFileManifest = null;

      public BigFileManifest GetBigFileManifest()
      {
         if (_bigFileLoaded == false)
         {
            _bigFileLoaded = true;
            _bigFileManifest = BigFileManifestLoader.Instance.Load(null) as BigFileManifest;
            if (_bigFileManifest != null)
            {
               AssetLogger.Log(Color.green, "单个大文件系统.");
            }
         }
         return _bigFileManifest;
      }

      public ILoader GetBigFileLoader()
      {
         return BigFileAssetBundleLoader.Instance;
      }

      #endregion

      #region package

      public PackageVersion GetPackageVersionInApp()
      {
         return PackageVersionInAppLoader.Instance.Load(null) as PackageVersion;
      }

      public PackageVersion GetPackageVersionInDownload()
      {
         return PackageVersionInDownloadLoader.Instance.Load(null) as PackageVersion;
      }

      public PackageManifest GetPackageManifestInApp()
      {
         return PackageManifestInAppLoader.Instance.Load(null) as PackageManifest;
      }

      public PackageManifest GetPackageManifestInDownload()
      {
         return PackageManifestInDownloadLoader.Instance.Load(null) as PackageManifest;
      }

      #endregion

      #region Load

      public ILoader GetAssetManifestLoader()
      {
         return AssetManifestLoader.Instance;
      }

      public void LoadBundlesSync(List<Bundle> bundles)
      {
         bundleSync.LoadBundles(bundles);
      }

      public AssetObject LoadAssetObject(Bundle bundle, string assetBuildPath)
      {
         return bundleSync.LoadAsset(bundle, assetBuildPath);
      }

      // 异步加载某个bundles的列表observables
      // 增加缓存,取最后一个parent作为记录ob作用
      public UniRx.IObservable<Unit> LoadBundlesAsync(List<Bundle> bundles)
      {
         var ob = GetObservableFor(bundles[bundles.Count - 1]);
         if (ob == null)
         {
            ob = bundleAsync.LoadBundles(bundles);
            AddObservableFor(bundles[bundles.Count - 1], ob);
         }

         return ob;
      }

      public void LoadAssetObject(Bundle bundle, string assetBuildPath, Action<AssetObject> callback)
      {
         bundleAsync.LoadAsset(bundle, assetBuildPath, callback);
      }

      public int GetBundleAsyncCount()
      {
         return bundleAsync.currentLoadingCount.Value;
      }

      #endregion

      #region async cached optimized

      Dictionary<Bundle, UniRx.IObservable<Unit>> obDictCached = new Dictionary<Bundle, UniRx.IObservable<Unit>>();

      UniRx.IObservable<Unit> GetObservableFor(Bundle b)
      {
         UniRx.IObservable<Unit> ob = null;
         if (obDictCached.TryGetValue(b, out ob) && ob != null)
         {
            return ob;
         }

         return null;
      }

      void AddObservableFor(Bundle b, UniRx.IObservable<Unit> ob)
      {
         obDictCached[b] = ob;

         ob
            .DoOnCompleted(() =>
            {
               obDictCached.Remove(b);
            })
            .Subscribe().AddTo(AssetManager.Assets.AssetManager.Instance.gameObject);
      }

      #endregion

   }
}