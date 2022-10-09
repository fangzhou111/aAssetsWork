/*
 * @Author: chiuan wei 
 * @Date: 2017-06-29 14:51:06 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-05 22:29:13
 */
using UnityEngine;
using System.Collections;
using System;
using SuperMobs.AssetManager.Package;
using SuperMobs.Core;
using SuperMobs.AssetManager.Loader;
using SuperMobs.AssetManager.Core;
using System.IO;
using UnityEngine.Networking;
using UniRx;
using SuperMobs.AssetManager.Assets;
using System.Net;
using System.Collections.Generic;

namespace SuperMobs.AssetManager.Update
{
    /// <summary>
    /// 版本更新流程：
    ///     1：下载app version 文件信息校验 coreversion 是否有更新、assetversion 是否有更新
    ///     2：core更新走apk、ipa下载更新 > 步骤1重复
    ///     3：asset更新下载完整的manifest文件，加载校验哪些是：增、删、改 需要进行处理（TODO:考虑增加完整的crc检查，或者关键的资源crc检查)
    ///     4：完毕后进入游戏
    /// 
    /// TODO:游戏运行过程中，断网重新连接时候考虑是否走一趟版本更新流程
    /// 
    /// 需要的文件：
    ///     Resources里面有一个version.text记录校验的url
    ///     本地有一个版本文件version.json
    ///     服务器版本目录也有一个version.json
    /// 
    /// API:
    ///     CheckAppUpgrade(callback) --- 检查是否需要更新
    ///     DoAppUpgrade(Action<float> onUpdateProcess,Action<string> onError,Action onFinish) --- 执行更新
    /// 
    /// APP_IS_REVIEW       int 这个app是否审核版本 0 :不是, 1:是
    /// WEB_EXTENSION       string web端配置的扩展参数存起来
    /// LOGIN_SERVER        string 登陆服务器地址
    /// APP_FULL_VERSION    string 这个app的完整版本号1.0.x.x
    /// 
    /// 
    /// </summary>

    class UpdaterMono : MonoBehaviour
    {
        static UpdaterMono _instance;
        public static UpdaterMono Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("UpdateMono");
                    go.hideFlags = HideFlags.HideInHierarchy;
                    _instance = go.AddComponent<UpdaterMono>();
                }
                return _instance;
            }
        }

        void Awake()
        {
            this.gameObject.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public class Updater
    {

        /// <summary>
        /// 更新状态
        /// </summary>
        public enum State : int
        {
            ERROR,
            NEWAPP,
            NeedUpdate,
            UPDATING,
            PASS,
            NEWAPP_TIP,
        }

        public enum InstallAPPResult
        {
            URLNotExist,
            DownloadError,
            CRCNotRight,
            Finished,
        }

        // 用于获取web信息校验
        AppPreference appPreference;
        WebPreference webPreference;

        // 用于校验资源是否需要更新
        PackageVersion appVer;
        PackageVersion localVer;
        PackageVersion serverVer;
        byte[] serverVersionContent;

        // 用于资源更新文件列表校验
        PackageManifest appPackageManifest;
        PackageManifest localPackageManifest;
        PackageManifest serverPackageManifest;
        byte[] serverPackageManifestContent;

        YieldUpdateAssets yieldUpdater;

        // 记录当次需要热更的文件
        // 可能上层需要用到判断
        List<PackageAsset> updateAssets = new List<PackageAsset> ();

        // 资源下载地址
        string cdn = string.Empty;

        // 程序更新地址
        string app = string.Empty;
        long appSize = 0;

        // web preference 获取的resVer
        // 从version里面最后一位来
        string webResVer = "0";

        /// <summary>
        /// 外部控制是否不检查cdn（可能压根不存在cdn支持）
        /// </summary>
        public static bool isNoUpdate = false;

        /// <summary>
        /// 如果下载失败，重试次数
        /// </summary>
        int repeatDownloadTime = 3;

        /// <summary>
        /// 类似易接sdk统一渠道，不区分个别小渠道app更新维护，只是提示个tip
        /// </summary>
        public static string[] NO_UPDATE_APP_CHANNEL = new string[] { "yijie", "tci" };
        static bool CheckNoAppUpdate(string channel)
        {
            foreach (string cc in NO_UPDATE_APP_CHANNEL)
            {
                if (cc.Equals(channel, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public void SetRepeatDownloadTime(int v)
        {
            repeatDownloadTime = v;

            if (repeatDownloadTime < 1)
            {
                repeatDownloadTime = 1;
            }
        }

        public List<PackageAsset> GetCurrentUpdateAssets()
        {
            return updateAssets;
        }

        /// <summary>
        /// 当前版本是否用来审核用的
        /// </summary>
        static bool _isReviewAPP = false;
        static bool isReviewAPP
        {
            get { return _isReviewAPP; }
            set
            {
                _isReviewAPP = value;

                if (value == false)
                {
                    PlayerPrefs.SetInt("APP_IS_REVIEW", 0);
                }
                else
                {
                    PlayerPrefs.SetInt("APP_IS_REVIEW", 1);
                }
            }
        }

        /// <summary>
        /// 重置当前更新、app状态
        /// NOTE:app启动就执行一次
        /// </summary>
        void Reset()
        {
            appPreference = null;
            webPreference = null;
            appPackageManifest = null;
            localPackageManifest = null;
            serverPackageManifest = null;
            appVer = null;
            localVer = null;
            serverVer = null;
            cdn = string.Empty;
            app = string.Empty;
            appSize = 0;

            isReviewAPP = false;

            PlayerPrefs.SetString("LOGIN_SERVER", "");

            // ready the loader service
            if (Service.IsSet<LoaderService>() == false)
                Service.Set<LoaderService>(new LoaderService());
        }

        private void RefreshAppServerIP()
        {
            if (appPreference == null || string.IsNullOrEmpty(appPreference.server))
            {
                return;
            }

            PlayerPrefs.SetString("LOGIN_SERVER", appPreference.server);
            AssetLogger.Log("获取到APP里面默认设置的服务器 " + appPreference.server, "Net");
        }

        private void RefreshAppFullVersion()
        {
            if (appPreference == null)
            {
                return;
            }

            string res = "0";
            if (localVer != null)
            {
                res = localVer.svnVer;
            }
            else if (appVer != null)
            {
                res = appVer.svnVer;
            }

            var app = appPreference.version.Substring(0, appPreference.version.LastIndexOf(".", StringComparison.Ordinal));

            PlayerPrefs.SetString("APP_FULL_VERSION", app + "." + res);
        }

        private bool GetAppPreference()
        {
            appPreference = AppPreference.LoadFromResources();
            return appPreference != null;
        }

        /// <summary>
        /// app包里面的version
        /// </summary>
        private bool GetAppVersion()
        {
            appVer = Service.Get<LoaderService>().GetPackageVersionInApp();
            return appVer != null;
        }

        /// <summary>
        /// 加载下载目录的version
        /// NOTE:需要考虑如果persistent目录的比app里面的新
        /// </summary>
        private bool GetLocalVersion()
        {
            localVer = Service.Get<LoaderService>().GetPackageVersionInDownload();
            return localVer != null;
        }

        /// <summary>
        /// 加载app里面的packagemanifest用于判断可更新资源文件时候，有可能包里面的就是想要的版本（例如倒退）就不需要更新了
        /// </summary>
        private bool GetAppPackageManifest()
        {
            appPackageManifest = Service.Get<LoaderService>().GetPackageManifestInApp();
            return appPackageManifest != null;
        }

        private bool GetLocalPackageManifest()
        {
            localPackageManifest = Service.Get<LoaderService>().GetPackageManifestInDownload();
            return localPackageManifest != null;
        }

        public float GetNewAppSizeMB()
        {
            if (appSize > 0.0f)
            {
                float ret = 0;
                float.TryParse(((appSize / (1024 * 1024.0f)).ToString("F2")), out ret);
                return ret;
            }
            else
            {
                return 0.0f;
            }
        }

        string GetWebUrlWithSlash(string url)
        {
            if (url.EndsWith("/", StringComparison.Ordinal) == false)
            {
                //return WWW.EscapeURL(url) + "/";
                return url + "/";
            }
            else
            {
                //return WWW.EscapeURL(url);
                return url;
            }
        }

        string GetWebRequestPlatform()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return "android";
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return "ios";
            }
            else
            {
                throw new Exception("GetWebRequestPlatform with dont support runtime platform");
            }
        }

        string GetAppBundleId()
        {
#if UNITY_5_6_OR_NEWER || UNITY_2017_2_OR_NEWER
            var bundleId = Application.identifier;
#else
			var bundleId = Application.bundleIdentifier;
#endif
            return bundleId;
        }

        /// <summary>
        /// 检查是否需要更新
        /// </summary>
        public void Check(Action<State> callback)
        {
            // 每次调用检查必须先重置一下
            Reset();

            // app里面一定要确保存在
            var _av = GetAppVersion();
            var _ap = GetAppPreference();
            var _apm = GetAppPackageManifest();
            if (_av == false || _ap == false || _apm == false)
            {
                AssetLogger.LogError("app default setting not ok,please check the app.", "Net");
                callback(State.ERROR);
                return;
            }
            else
            {
                // 先做一些默认处理
                RefreshAppServerIP();
            }

            AssetLogger.Log("get app version =" + appVer.ToJson(false), "Net");

            if (GetLocalVersion())
            {
                AssetLogger.Log("YES man you get download version =" + localVer.ToJson(false), "Net");

                // 不删掉？也许因为网络下载的就是这么旧的资源呢
                // 判断更新过后的和app里面的是否版本一致
                // 如果app里面的时间比更新后的version文件新，则删掉更新目录的文件
                // if (localVer != null && localVer.time < appVer.time)
                // {
                // 	try
                // 	{
                // 		AssetLogger.Log("app里面的version.config比较新，删掉下载目录的文件", "Net");

                // 		if (Directory.Exists(AssetPath.DownloadAssetBundlesPath))
                // 		{
                // 			Directory.Delete(AssetPath.DownloadAssetBundlesPath, true);
                // 		}
                // 	}
                // 	catch (Exception e)
                // 	{
                // 		Debug.LogException(e);
                // 	}
                // }
            }
            else
            {
                AssetLogger.LogWarning("NO local package version founded.本地没有下载过任何资源包信息", "Net");
            }

            // check if exist download updated assets
            // local download path exist packagemanifest file.
            if (GetLocalPackageManifest())
            {
                AssetLogger.Log("YES man your still have something in download package manifest.", "Net");
            }

            // record current version
            RefreshAppFullVersion();

            // 接下来从网路下载校验
            UpdaterMono.Instance.StartCoroutine(IECheck(callback));
        }

        IEnumerator IECheck(Action<State> callback)
        {
            //yield return null;
            // ----------------------------------------
            // 先向运营平台获取某个渠道的版本信息和下载地址
            // 只有当渠道版本号才是
            // ----------------------------------------
            // 如果没有web，就往cdn获取配置
            if (string.IsNullOrEmpty(appPreference.web) == false || string.IsNullOrEmpty(appPreference.cdn) == false)
            {
                string fullWebUrl = "";

                if (string.IsNullOrEmpty(appPreference.web) == false)
                {
                    // xxxx?platform=android&channel=uc
                    if (appPreference.web.Contains("?"))
                    {
                        fullWebUrl = appPreference.web
                                    + "&platform=" + GetWebRequestPlatform()
                                    + "&channel=" + appPreference.channel
                                    + "&bundleid=" + GetAppBundleId()
                                    + "&" + UnityEngine.Random.Range(0, int.MaxValue)
                        ;
                    }
                    else
                    {
                        fullWebUrl = appPreference.web
                                    + "?platform=" + GetWebRequestPlatform()
                                    + "&channel=" + appPreference.channel
                                    + "&bundleid=" + GetAppBundleId()
                                    + "&" + UnityEngine.Random.Range(0, int.MaxValue)
                        ;
                    }
                }
                else
                {
                    // xxxx/web.android.uc.config
                    if (GetWebUrlWithSlash(appPreference.cdn).Contains("?"))
                    {
                        fullWebUrl = GetWebUrlWithSlash(appPreference.cdn)
                                    + GetWebRequestPlatform()
                                    + "." + appPreference.channel
                                    + ".config"
                                    + "&" + UnityEngine.Random.Range(0, int.MaxValue)
                        ;
                    }
                    else
                    {
                        fullWebUrl = GetWebUrlWithSlash(appPreference.cdn)
                                    + GetWebRequestPlatform()
                                    + "." + appPreference.channel
                                    + ".config"
                                    + "?" + UnityEngine.Random.Range(0, int.MaxValue)
                        ;
                    }

                }

                AssetLogger.Log("web获取信息地址=" + fullWebUrl, "Net");

                bool _done = false;
                Exception _ex = null;

                var webOB = ObservableWWW
                    .Get(fullWebUrl)
                    .Do(text =>
                    {
                        if (string.IsNullOrEmpty(text)) return;

                        AssetLogger.Log("downloaded the web preference=\n" + text, "Net");

                        // 把获取的web配置信息转成结构
                        webPreference = new WebPreference();
                        webPreference.FromJson(text);

                        // 保存一下扩展字段
                        PlayerPrefs.SetString("WEB_EXTENSION", webPreference.extension);
                    })
                    .Timeout(TimeSpan.FromSeconds(30))
                    .DoOnError(ex => _ex = ex)
                    .DoOnTerminate(() => _done = true)
                    ;

                // if failed repeat 3 times.
                for (int i = 0; i < repeatDownloadTime; i++)
                {
                    _done = false;
                    _ex = null;
                    webPreference = null;
                    webOB.Subscribe();

                    // waiting...
                    while (_done == false) yield return null;

                    // no error.
                    if (_ex == null) break;
                }

                if (_ex != null)
                {
                    AssetLogger.LogException("获取web配置的信息出错:" + _ex.Message, "Net");
                    callback(State.ERROR);
                    yield break;
                }

                if (webPreference != null)
                {
                    // 对比code version
                    // 1.1.xx.resVer其中xx就是代码版本号
                    // NOTE:只有c#代码版本号变化时候才需要热更app

                    // web网页端配置的代码版本号,取前3位
                    int codeWebVer = -1;
                    var webNums = webPreference.version.Split('.');
                    if (webNums.Length != 4)
                    {
                        AssetLogger.LogError("web preference 的版本号version配置不齐全 n.n.n.n", "Net");
                        callback(State.ERROR);
                        yield break;
                    }
                    else
                    {
                        // 获取web资源版本号
                        webResVer = webNums[3];
                    }

                    if (int.TryParse(webPreference.version.Split('.')[2], out codeWebVer) == false)
                    {
                        AssetLogger.LogError("版本号 codeWebVer 获取校验出错,有非法字符 = " + webPreference.version, "NET");
                        callback(State.ERROR);
                        yield break;
                    }

                    // App里面保存的代码版本号,取前3位
                    int codeAppVer = -1;
                    string appCodeVersion = Application.version;
                    // FIX: fuck unity bug!!!
                    if (Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        //1.3.3.123
                        appCodeVersion = appPreference.version.Substring(0, appPreference.version.LastIndexOf(".", StringComparison.Ordinal));
                    }

                    if (int.TryParse(appCodeVersion.Split('.')[2], out codeAppVer) == false)
                    {
                        AssetLogger.LogError("版本号 codeAppVer 获取校验出错,有非法字符 = " + appCodeVersion, "NET");
                        callback(State.ERROR);
                        yield break;
                    }

                    AssetLogger.Log("web app ver=" + codeWebVer + "  app ver=" + codeAppVer, "Net");
                    PlayerPrefs.SetString("WEB_FULL_VERSION", webPreference.version);

                    if (string.IsNullOrEmpty(webPreference.server) == false)
                    {
                        PlayerPrefs.SetString("LOGIN_SERVER", webPreference.server);
                        AssetLogger.Log("获取到运营配置的服务器 " + webPreference.server, "Net");
                    }

                    // 是否配置了审核版本号
                    if (string.IsNullOrEmpty(webPreference.reviewVersion) == false)
                    {
                        // 完整的版本号校验:1.3.3.53029
                        AssetLogger.LogWarning("web.reviewVersion=" + webPreference.reviewVersion + "   app.version=" + appPreference.version, "Net");
                        if (webPreference.reviewVersion.Equals(appPreference.version))
                        {
                            AssetLogger.LogWarning("检测到这个是审核版本！", "NET");
                            isReviewAPP = true;

                            // 如果是审核版本
                            // 需要用审核的服务器
                            if (string.IsNullOrEmpty(webPreference.reviewServer) == false)
                            {
                                PlayerPrefs.SetString("LOGIN_SERVER", webPreference.reviewServer);
                                AssetLogger.Log("获取到运营配置的reviewServer服务器 " + webPreference.reviewServer, "Net");
                            }
                            else
                            {
                                AssetLogger.LogError("这个是审核版本，但是没有审核服务器设置", "Net");
                            }

                            // 审核版本干掉所有下载过的更新
                            try
                            {
                                if (Directory.Exists(AssetPath.DownloadAssetBundlesPath))
                                {
                                    Directory.Delete(AssetPath.DownloadAssetBundlesPath, true);
                                }
                            }
                            catch (Exception) { }
                        }
                        else
                        {
                            isReviewAPP = false;
                        }
                    }

                    // 不是审核版本才需要校验app核心版本号 
                    // 提示更新apk | ipa 
                    if (isReviewAPP == false && codeAppVer < codeWebVer)
                    {
                        if (CheckNoAppUpdate(appPreference.channel) || string.IsNullOrEmpty(webPreference.app))
                        {
                            AssetLogger.LogWarning(appPreference.ToJson(false) + "需要更新,但是不自动更新:" + webPreference.ToJson(false), "NET");
                            callback(State.NEWAPP_TIP);
                            yield break;
                        }

                        // get size form webPreference.app
                        YieldGetDownloadSize fileSize = new YieldGetDownloadSize(webPreference.app);
                        yield return fileSize;
                        appSize = fileSize.size;
                        app = webPreference.app;

                        AssetLogger.LogWarning("需要更新APP:" + webPreference.ToJson(false), "NET");

                        callback(State.NEWAPP);
                        yield break;
                    }
                    else
                    {
                        AssetLogger.LogWarning("AppVersion PASS，检查运营配置的codeVer一样\n" + appPreference.ToJson(false)
                            + "\n--------------------------------------------\n"
                            + webPreference.ToJson(false), "NET");
                    }
                } // webPreference 判断
            } // webPreference download

            // ---------------------------------------
            // 如果是审核版本，不需要进行校验更新
            // 如果是没有CDN，不需要进行校验更新
            // ---------------------------------------
            if (isReviewAPP || isNoUpdate)
            {
                callback(State.PASS);
                yield break;
            }
            else if (webPreference == null)
            {
                AssetLogger.LogError("web preference下载出错，更新异常!\n" + appPreference.ToJson(false), "Net");

                callback(State.ERROR);
                yield break;
            }

            // ----------------------------------------
            // 如果前面没有获取到，那么再接着和下载自己的平台的cdn地址里面的信息判断
            // 如果前面已经检查过app代码号，这里就不需要校验版本号，直接校验资源
            // ----------------------------------------

            cdn = string.IsNullOrEmpty(webPreference.cdn) ? appPreference.cdn : webPreference.cdn;
            if (string.IsNullOrEmpty(cdn) == false)
            {
                cdn = GetWebUrlWithSlash(cdn);

                var serverVerPath = cdn + Crc32.GetStringCRC32(AssetPath.PACKAGE_VERSION_FILE)
                                     + "_" + webResVer
                                     + AssetPath.ASSETBUNDLE_SUFFIX
                                     + "?" + UnityEngine.Random.Range(0, int.MaxValue);

                bool _done = false;
                Exception _ex = null;

                AssetLogger.Log("下载cdn package version:" + serverVerPath, "Net");

                var serverOB = ObservableWWW.GetAndGetBytes(serverVerPath)
                             .Do(result =>
                             {
                                 try
                                 {
                                     AssetBundle ab = AssetBundle.LoadFromMemory(AssetPreference.ConvertAssetBundleContent(result));
                                     var text = ab.LoadAllAssets()[0] as TextAsset;
                                     var bytes = text != null ? text.bytes : null;
                                     if (bytes == null)
                                     {
                                         ab.Unload(false);
                                     }

                                     serverVer = new PackageVersion();
                                     serverVer.FromStreamBytes(bytes);
                                     serverVersionContent = result;

                                     ab.Unload(false);
                                 }
                                 catch (Exception e)
                                 {
                                     AssetLogger.LogException("构造serverVer出错:" + e.Message, "Net");
                                     _ex = e;
                                 }
                             })
                            .Timeout(TimeSpan.FromSeconds(30))
                            .DoOnTerminate(() => _done = true)
                            .DoOnError(ex => _ex = ex)
                            ;

                // if failed repeat 3 times.
                for (int i = 0; i < repeatDownloadTime; i++)
                {
                    _done = false;
                    _ex = null;
                    serverVer = null;
                    serverOB.Subscribe();

                    // waiting...
                    while (_done == false) yield return null;

                    // no error.
                    if (_ex == null) break;
                }

                if (_ex != null || serverVer == null)
                {
                    callback(State.ERROR);
                    yield break;
                }
            }
            else
            {
                AssetLogger.LogException("想要获取serverVer但是cdn路径空了\nwebPreference="
                    + webPreference.ToJson(false)
                    + "\nappPreference=" + appPreference.ToJson(false), "Net");

                callback(State.ERROR);
                yield break;
            }


            // 判断校验local还是app里面的
            var currentVersion = localVer != null ? localVer.version : appVer.version;

            AssetLogger.Log("get serverVer = " + serverVer.ToJson(false), "Net");

            if (currentVersion != serverVer.version)
            {
                AssetLogger.Log(Color.yellow, "资源需要更新\n" + currentVersion + ":" + serverVer.version, "Net");
                callback(State.NeedUpdate);
            }
            else
            {
                AssetLogger.Log(Color.green, "资源校验PASS!游戏快乐!", "Net");
                callback(State.PASS);
            }

        }

        /// <summary>
        /// 检查需要更新的文件大小,以及更新文件列表
        /// </summary>
        public void CheckAppUpgradeSize(Action<float, string> onUpdateProcess, Action<string> onError, Action<long> onFinish)
        {
            UpdaterMono.Instance.StartCoroutine(IECheckUpdatingSizeAndInit(onUpdateProcess, onError, onFinish));
        }

        IEnumerator IECheckUpdatingSizeAndInit(Action<float, string> onUpdateProcess, Action<string> onError, Action<long> onFinish)
        {
            // download the packagemanifest on server side.
            var serverPackageManifestPath = cdn + Crc32.GetStringCRC32(AssetPath.PACKAGE_MANIFEST_FILE)
                                                + "_" + serverVer.version
                                                + AssetPath.ASSETBUNDLE_SUFFIX;
            //+ "?" + UnityEngine.Random.Range(0, int.MaxValue);

            bool _done = false;
            Exception _ex = null;

            //serverPackageManifestPath = WWW.EscapeURL(serverPackageManifestPath);
            var OB = ObservableWWW.GetAndGetBytes(serverPackageManifestPath)
                         .Do(result =>
                         {
                             try
                             {
                                 AssetBundle ab = AssetBundle.LoadFromMemory(AssetPreference.ConvertAssetBundleContent(result));
                                 var text = ab.LoadAllAssets()[0] as TextAsset;
                                 var bytes = text != null ? text.bytes : null;
                                 if (bytes == null)
                                 {
                                     ab.Unload(false);
                                 }

                                 serverPackageManifest = new PackageManifest();
                                 serverPackageManifest.FromStreamBytes(bytes);
                                 serverPackageManifestContent = result;

                                 ab.Unload(false);
                             }
                             catch (Exception e)
                             {
                                 AssetLogger.LogException("构造 serverPackageManifest 出错:" + e.Message, "Net");
                                 _ex = e;
                             }
                         })
                         .Timeout(TimeSpan.FromSeconds(30))
                         .DoOnTerminate(() => _done = true)
                         .DoOnError(ex => _ex = ex)
                         ;

            // if failed repeat 3 times.
            for (int i = 0; i < repeatDownloadTime; i++)
            {
                _done = false;
                _ex = null;
                serverPackageManifest = null;
                OB.Subscribe();

                // waiting...
                while (_done == false) yield return null;

                // no error.
                if (_ex == null) break;
            }

            if (_ex != null || serverPackageManifest == null)
            {
                onError("download server Package Manifest error!");
                yield break;
            }

            /// 构造更新逻辑块
            yieldUpdater = new YieldUpdateAssets(onUpdateProcess, appPackageManifest, localPackageManifest, serverPackageManifest, cdn);
            yieldUpdater.StartInit();
            while (yieldUpdater.isInit == false)
            {
                yield return null;
            }

            // 回调这个更新长度
            updateAssets = yieldUpdater.GetCurrentUpdateAssets();
            onFinish(yieldUpdater.updateSize);
        }


        /// <summary>
        /// 下载新的app安装,并且安装
        /// </summary>
        public void DoAppInstallNew(Action<float, string> onUpdateProcess, Action<InstallAPPResult> onComplete)
        {
            UpdaterMono.Instance.StartCoroutine(IEDoUpdateAPP(onUpdateProcess, onComplete));
        }

        IEnumerator IEDoUpdateAPP(Action<float, string> onUpdateProcess, Action<InstallAPPResult> onComplete)
        {
            if (string.IsNullOrEmpty(app))
            {
                onComplete(InstallAPPResult.URLNotExist);
                yield break;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (Application.platform == RuntimePlatform.Android)
            {
                string apkVersion = "";
                if (webPreference != null)
                {
                    apkVersion = webPreference.version.Replace(".", "_");
                }

                YieldDownloadApp download = new YieldDownloadApp(app, Application.productName + "_" + apkVersion + ".apk", appSize);
                download.actUpdate = onUpdateProcess;
                download.StartDownload();
                yield return download;

                if (!string.IsNullOrEmpty(download.error))
                {
                    onComplete(InstallAPPResult.DownloadError);
                    yield break;
                }
                else
                {
                    AssetLogger.Log(Color.green, "下载完apk，需要安装", "Net");

                    while (!InstallAPK(download.localPath))
                        yield return new WaitForSeconds(0.5f);
                    onComplete(InstallAPPResult.Finished);
                    yield return new WaitForSeconds(0.25f);
                    Application.Quit();
                }
            }
#elif UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                // 是否判断需要打开的是appstore还是直接下载，越狱可以直接安装。
                Application.OpenURL(app);

                onComplete(InstallAPPResult.Finished);
                yield return new WaitForSeconds(0.25f);
                Application.Quit();
            }
#else
            Debug.LogException(new Exception("其他平台不需要热更app"));
            onComplete(InstallAPPResult.URLNotExist);
#endif
        }


        /// <summary>
        /// 如果asset有更新，从asseturl下载manifest文件，然后匹配不同的文件需要重新下载撒
        /// </summary>
        public void DoAppUpgrade(Action<float, string> onUpdateProcess, Action<string> onError, Action onFinish)
        {
            UpdaterMono.Instance.StartCoroutine(IEDoUpdating(onUpdateProcess, onError, onFinish));
        }

        IEnumerator IEDoUpdating(Action<float, string> onUpdateProcess, Action<string> onError, Action onFinish)
        {
            if (yieldUpdater.CheckNeedUpdate())
            {
                yieldUpdater.StartUpdate();
                yield return yieldUpdater;

                // 如果更新失败，直接跳过
                if (yieldUpdater.CheckIfSuccess() == false)
                {
                    onError("download failed.");
                    yield break;
                }
            }

            // 保存新的manifest文件
            string localManifestPath = AssetPath.GetPathInDownLoaded(Crc32.GetStringCRC32(AssetPath.PACKAGE_MANIFEST_FILE));
            FileInfo fi = new FileInfo(localManifestPath);
            if (fi.Directory.Exists == false)
            {
                AssetLogger.LogWarning("更新完毕但是居然没有这个目录,没下过东西: " + fi.Directory.FullName);
                fi.Directory.Create();
            }
            File.WriteAllBytes(localManifestPath, serverPackageManifestContent);
            AssetLogger.Log("[UPGRADE]" + "Save server package manifest file = " + localManifestPath + "  length=" + serverPackageManifestContent.Length, "Net");

            // 更新完毕需要写入最新version文件
            string localVerPath = AssetPath.GetPathInDownLoaded(Crc32.GetStringCRC32(AssetPath.PACKAGE_VERSION_FILE));
            File.WriteAllBytes(localVerPath, serverVersionContent);
            AssetLogger.Log("[UPGRADE]" + "save server version file = " + localVerPath + "  length=" + serverVersionContent.Length, "Net");

            // record the new app full version.
            localVer = serverVer;
            RefreshAppFullVersion();

            if (onFinish != null)
                onFinish();
        }

        /// <summary>
        /// 调用安卓的安装接口安装apk
        /// </summary>
        bool InstallAPK(string apk)
        {
#if UNITY_ANDROID
            return Service.Get<Android>().InitAPK(apk);
#else
            return true;
#endif
        }

        #region network state

        public enum NetKind : int
        {
            NotReachable = 1,
            Mobile4G = 2,
            WIFI = 3,
            UNKNOWN = 4,
        }

        public static NetKind netKind
        {
            get
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                    return NetKind.NotReachable;
                else if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
                    return NetKind.Mobile4G;
                else if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
                    return NetKind.WIFI;
                else
                    return NetKind.UNKNOWN;
            }
        }

        public void CheckNetworkIsReady(Action<bool> callback)
        {
            UpdaterMono.Instance.StopCoroutine(PingConnect(callback));
            UpdaterMono.Instance.StartCoroutine(PingConnect(callback));
        }

        IEnumerator PingConnect(Action<bool> callback)
        {
            string[] ipadress = new string[] { "www.baidu.com", "www.1688.com" };
            bool isSucc = false;
            foreach (string ip in ipadress)
            {
                Ping ping = new Ping(ip);
                int nTime = 0;
                while (!ping.isDone)
                {
                    yield return new WaitForSeconds(0.1f);

                    if (nTime > 20)
                    {
                        nTime = 0;
                        AssetLogger.LogError(ip + " >>> ping失败: " + ping.time, "NET");
                        break;
                    }
                    nTime++;
                }

                if (ping.isDone)
                {
                    isSucc = true;
                    break;
                }
            }

            if (isSucc == true)
                callback(isSucc);
            else
            {
                WebRequest request = HttpWebRequest.Create("https://www.baidu.com/");
                request.Method = "HEAD";
                WebResponse response = null;
                try
                {
                    response = request.GetResponse();
                }
                catch
                {
                    response = null;
                }

                callback(response != null);
            }
        }

        #endregion

    } // class
}// namespace