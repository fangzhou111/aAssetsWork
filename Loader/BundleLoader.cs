using System;
using System.Collections.Generic;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using SuperMobs.Core;
using UniRx;
using UnityEngine;
using ObservableUnity = UniRx.Observable;
using System.Collections;

namespace SuperMobs.AssetManager.Loader
{
      /// <summary>
      /// 负责AssetBundle类型的Bundle同步加载
      /// </summary>
      internal class BundleSync
      {
            public void LoadBundles(List<Bundle> bundles)
            {
                  if (bundles.Count <= 0)
                  {
                        throw new Exception("[asset] Sync LoadBundles Input empty List of bundles! please check your assets.");
                  }

                  var parent = bundles[bundles.Count - 1];
                  for (int i = 0, len = bundles.Count; i < len; i++)
                  {
                        var item = bundles[i];

                        // 给依赖的对象增加主资源的关联
                        if (item != parent)
                              item.AddParent(parent);

                        LoadBundle(item);
                  }

            }

            /// <summary>
            /// 1-根据bundle类型使用不同的加载器加载
            /// 2-根据bundle类型处理不同bundle资源准备完毕
            /// </summary>
            void LoadBundle(Bundle bundle)
            {
                  if (bundle.isLoaded == false)
                  {
                        // 如果这时候有个异步载正在处理
                        // 那么如果这个时候去判断一下IsDone如果成功的话,那么bundle的assetbundle将会被构造出来了.
                        if (bundle.wwwProcessor != null && bundle.wwwProcessor.IsDone)
                        {
                              AssetLogger.Log(bundle.bundleName + " is www done when in sync loading!");
                              if (!string.IsNullOrEmpty(bundle.wwwProcessor.error))
                              {
                                    AssetLogger.LogError("but the www contain error:" + bundle.wwwProcessor.error);
                              }
                        }
                        else if (bundle.wwwProcessor != null)
                        {
                              AssetLogger.Log(bundle.bundleName + " is www running... but not done yet. www exist when in sync load!");
                              // bundle.ReleaseWWW();
                        }

                        // 也许前面判断异步isDone就完成了
                        if (bundle.isLoaded == false)
                        {
                              var ab = bundle.assetBundle;
                              if (ab == null)
                              {
                                    try
                                    {
                                          AssetLogger.Log(Color.cyan, "同步加载 >>> " + bundle.bundleName);
                                          ab = AssetBundleSyncLoader.Instance.Load(bundle.bundleName) as AssetBundle;
                                    }
                                    catch (Exception e)
                                    {
                                          AssetLogger.LogError("同步加载ab异常:" + bundle.bundleName + " exception:" + e.Message);
                                    }

                                    // 设置一下assetbundle
                                    bundle.assetBundle = ab;
                              }

                              if (bundle.assetBundle == null)
                              {
                                    AssetLogger.LogError("同步加载ab不到:" + bundle.bundleName);
                                    return;
                              }

                              // mark loaded ok.
                              bundle.isLoaded = true;
                        }

                        // if (bundle.wwwProcessor != null)
                        // {
                        // 	bundle.ReleaseWWW();
                        // }
                  }
            }

            /// <summary>
            /// 负责从这个Bundle里面构造某个路径的AssetObject
            /// NOTE:steam ab 不需要读ab
            /// </summary>
            public AssetObject LoadAsset(Bundle bundle, string assetBuildPath)
            {
                  if (bundle.assetBundle != null)
                  {
                        UnityEngine.Object asset;
                        if (bundle.bundleType != AssetBuildType.streamab)
                              asset = bundle.assetBundle.LoadAsset(assetBuildPath, bundle.GetAssetLoadType());
                        else
                              asset = bundle.assetBundle;

                        return bundle.ReadyAssetObject(assetBuildPath, asset);
                  }
                  else
                  {
                        AssetLogger.LogError("同步载Bundle里面的AssetObject跪求了，这个Bundle的AB是空! " + bundle.bundleName);
                        return null;
                  }
            }

      }

      /// <summary>
      /// 异步处理bundle的加载,当assetbundle
      /// </summary>
      internal class BundleAsync
      {
            public readonly ReactiveProperty<int> currentLoadingCount = new ReactiveProperty<int>(0);
            delegate void LoadBundlesCallback(bool result);

            /// <summary>
            /// 不删除的gameobject用来支持observable切换场景时候确保不并干掉 
            /// </summary>
            GameObject dontDestroyObj
            {
                  get
                  {
                        return AssetManager.Assets.AssetManager.Instance.gameObject;
                  }
            }

            public void LoadAsset(Bundle bundle, string assetBuildPath, Action<AssetObject> callback)
            {
                  if (bundle.assetBundle != null)
                  {
                        // 先往bundle构造这个object的占坑
                        var asset = bundle.ReadyAssetObject(assetBuildPath);

                        // add async callback
                        asset.async += callback;

                        if (bundle.bundleType != AssetBuildType.streamab)
                        {
                              if (asset.dispose == null)
                              {
                                    // 如果之前已经在异步加载了
                                    bundle.loadingRefer++;
                                    var request = bundle.assetBundle.LoadAssetAsync(assetBuildPath, bundle.GetAssetLoadType());
                                    asset.dispose = request.AsObservable()
                                          .DoOnCompleted(() =>
                                          {
                                                bundle.ReadyAssetObject(assetBuildPath, request.asset);
                                                asset.dispose = null;
                                          })
                                          .DoOnTerminate(() => bundle.loadingRefer--)
                                          .ObserveOnMainThread()
                                          .Subscribe()
                                          .AddTo(dontDestroyObj);
                              }
                        }
                        else
                        {
                              bundle.ReadyAssetObject(assetBuildPath, bundle.assetBundle);
                        }
                  }
                  else
                  {
                        if (callback != null) callback(null);
                  }
            }

            public UniRx.IObservable<Unit> LoadBundles(List<Bundle> bundles)
            {
                  // AssetLogger.Log(Color.yellow, ".......start load " + bundles[bundles.Count - 1].bundleName + " async bundles count = " + bundles.Count);
                  Subject<Unit> sb = new Subject<Unit>();
                  LoadBundles(bundles, _ => sb.OnCompleted());
                  return sb;
            }

            /// <summary>
            /// 传入一系列的bundle进行加载
            /// FIX：确保依赖的先加载后才加载目标主资源，不然可能会丢失依赖
            /// </summary>
            void LoadBundles(List<Bundle> bundles, LoadBundlesCallback callback)
            {
                  if (bundles.Count <= 0)
                  {
                        callback(true);
                        return;
                  }

                  // parent
                  var parent = bundles[bundles.Count - 1];

                  // dependencies observable
                  List<UniRx.IObservable<Unit>> obs = new List<UniRx.IObservable<Unit>>();

                  // 依赖的先加载			
                  for (int i = 0, len = bundles.Count - 1; i < len; i++)
                  {
                        bundles[i].AddParent(parent);

                        if (bundles[i].isLoaded == false)
                              obs.Add(LoadBundle(bundles[i]));
                  }

                  // 如果没有任何依赖
                  if (obs.Count == 0)
                  {
                        obs.Add(ObservableUnity.Empty<Unit>());
                  }

                  ObservableUnity
                        .WhenAll(obs)
                        // .Do(_ => Debug.Log("依赖加载完了，到主资源加载:" + parent.bundleName))
                        .Concat(LoadBundle(parent))
                        .SubscribeOnMainThread()
                        .Timeout(TimeSpan.FromSeconds(60), Scheduler.MainThread)
                        .Subscribe(_ => { },
                              ex =>
                              {
                                    AssetLogger.LogError("Async load All Observable " + parent.bundleName + " exception: " + ex.Message);
                                    callback(false);
                              },
                              () =>
                              {
                                    //AssetLogger.Log("finnal load callback");
                                    callback(true);
                              })
                        .AddTo(dontDestroyObj);
            }

            // 构造异步等待器
            UniRx.IObservable<Unit> CreateAsyncWaitObservable()
            {
                  return Observable
                        .EveryUpdate()
                        .Where(_ => currentLoadingCount.Value <= AssetPreference.MAX_ASYNC_LOADING_COUNT)
                        .Take(1)
                        .AsUnitObservable();
            }

            // 返回的是这个异步加载bundle完成的observable
            private UniRx.IObservable<Unit> LoadBundle(Bundle bundle)
            {
                  //AssetLogger.Log("Async load bundle begin > " + bundle.bundleName);
                  if (bundle.isLoaded) return ObservableUnity.Empty<Unit>();

                  // 这个是目前正在异步加载这个bundle的ob
                  // 当加载完毕一定要回调一下OnComplete不然异步逻辑可能完成不了撒!
                  // 如果这个loadState已经存在了,证明正在加载
                  // 则不需要重复构造同一个bundle的了.
                  var loadState = bundle.wwwLoadState;
                  if (loadState != null)
                  {
                        return loadState;
                  }
                  else
                  {
                        loadState = bundle.CreateWWWObservable();
                  }

                  Action loadFunc = () =>
                  {
                        // 当真的异步加载进行了,先增加一个异步加载计数
                        ++currentLoadingCount.Value;

                        bundle.d0 = DateTime.Now;

                        // 开启一个异步的ob
                        CreateLoadBundleObservableWithUpdate(bundle)
                              .ObserveOnMainThread()
                              .Timeout(TimeSpan.FromSeconds(AssetPreference.GetLoaderTimeout()))
                              .DoOnTerminate(() =>
                              {
                                    AssetLogger.Log(Color.green, "异步加载完毕 >>>> " +
                                          bundle.bundleName + "  AB:" + (bundle.assetBundle != null) +
                                          " cost : " + (DateTime.Now - bundle.d0).TotalSeconds + "s");

                                    --currentLoadingCount.Value;
                                    loadState.OnCompleted();
                              })
                              .DoOnCancel(() => AssetLogger.LogWarning("async www cancle bundle " + bundle.bundleName))
                              .Subscribe(
                                    __ => { },
                                    (ex) =>
                                    {
                                          AssetLogger.LogError("async www load bundle exception > " +
                                                bundle.bundleName + "  exception=" + ex.Message +
                                                " 所有正在异步数量=" + currentLoadingCount.Value + " timescale=" + Time.timeScale);
                                    },
                                    () => { }
                              ).AddTo(dontDestroyObj);
                  };

                  if (currentLoadingCount.Value <= AssetPreference.MAX_ASYNC_LOADING_COUNT)
                  {
                        loadFunc();
                  }
                  else
                  {
                        CreateAsyncWaitObservable()
                              .Do(_ =>
                              {
                                    // 如果没加载完则触发异步加载
                                    if (bundle.isLoaded == false)
                                    {
                                          loadFunc();
                                    }
                                    else
                                    {
                                          loadState.OnCompleted();
                                    }
                              })
                              .Subscribe()
                              .AddTo(dontDestroyObj);
                  }

                  return loadState;
            }

            // 构造update去执行判断
            UniRx.IObservable<Unit> CreateLoadBundleObservableWithUpdate(Bundle bundle)
            {
                  if (bundle.wwwProcessor == null || bundle.wwwProcessor.IsDispose)
                  {
                        bundle.wwwProcessor = new WWWAB(bundle);
                  }

                  // start
                  bundle.wwwProcessor.Start();

                  var ob =
                        Observable
                        .EveryUpdate()
                        .Where(_ => bundle.wwwProcessor.IsDone)
                        .Take(1)
                        .Do(_ =>
                        {
                              if (string.IsNullOrEmpty(bundle.wwwProcessor.error) == false)
                                    AssetLogger.LogError("wwwProcessor error = " + bundle.wwwProcessor.error);
                        })
                        .Do(_ => { bundle.wwwProcessor.Dispose(); })
                        .AsUnitObservable();

                  return ob;
            }

      }

      /// <summary>
      /// 异步加载assetbundle的类
      /// </summary>
      class WWWAB : IWWWAsset, IDisposable
      {
            AssetBundleAsyncLoader loader;
            Bundle bundle;
            string _error;

            public WWWAB(Bundle b)
            {
                  this.bundle = b;
            }

            public void Start()
            {
                  if (bundle.assetBundle == null)
                  {
                        loader = new AssetBundleAsyncLoader();
                        AssetLogger.Log(Color.yellow, "异步加载 >>>> " + bundle.bundleName);
                        loader.Load(bundle.bundleName);
                  }
                  else
                  {
                        bundle.isLoaded = true;
                  }
            }

            public string error
            {
                  get
                  {
                        return _error;
                  }

                  set
                  {
                        throw new NotImplementedException();
                  }
            }

            public bool IsDone
            {
                  get
                  {
                        // 是否存在可能是同步加载assetbundle完成了
                        if (bundle.assetBundle != null)
                        {
                              AssetLogger.LogWarning("WWWAB when checking IsDone got bundle.assetbundle != null. maybe sync loaded.");
                              Dispose();
                              return true;
                        }

                        // 如果loader还没完
                        if (loader != null && loader.isDone == false)
                        {
                              return false;
                        }
                        else if (loader != null && loader.isDone)
                        {
                              _error = loader.error;
                              if (string.IsNullOrEmpty(_error) == false)
                              {
                                    AssetLogger.LogError("WWWAB load assetbundle but with error = " + _error);
                                    return true;
                              }

                              // 完成先缓存下ab
                              try
                              {
                                    if (bundle.assetBundle == null)
                                    {
                                          var ab = loader.Require() as AssetBundle;
                                          bundle.assetBundle = ab;
                                    }
                              }
                              catch (Exception e)
                              {
                                    AssetLogger.LogError("WWWAB load assetbundle with loader.Require() " + bundle.bundleName +
                                          " exception=" + e.Message);
                              }

                              if (bundle.assetBundle == null)
                              {
                                    _error = "WWWAB " + bundle.bundleName + " Async Processor got null assetbundle when done!";
                                    return true;
                              }

                              // mark loaded ok.
                              bundle.isLoaded = true;

                              // dont need this anymore.
                              loader = null;
                        }

                        return true;
                  }

                  set
                  {
                        throw new NotImplementedException();
                  }
            }

            public void Dispose()
            {
                  if (loader != null)
                  {
                        loader.Stop();
                        loader = null;
                  }
            }

            public bool IsDispose
            {
                  get
                  {
                        return loader == null;
                  }

                  set
                  {
                        throw new NotImplementedException();
                  }
            }
      }

}