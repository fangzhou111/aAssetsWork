/*
 * @Author : chiuan wei 
 * @Date : 2017-04-10 11:34:58 
 * @Last Modified by : chiuan wei
 * @Last Modified time : 2017-06-15 11:44:54
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Loader;
using SuperMobs.AssetManager.Package;
using SuperMobs.Core;
//using TinyTeam.Debuger;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMobs.AssetManager.Update
{
    /// <summary>
    /// 游戏启动更新界面入口
    /// 1、负责更新游戏资源
    /// 2、负责启动游戏逻辑
    /// 3、负责重启更新逻辑，把现有的ab清掉、lua环境清掉
    /// /// </summary>
    [RequireComponent(typeof(UpdateUILogicBase))]
    public class UpdateController : MonoBehaviour
    {
        // 更新成功回调设置回调处理
        public static Action callLogin;

        /// <summary>
        /// ui logic
        /// </summary>
        UpdateUILogicBase uiLogicBase;

        UpdateTip tipContent = new UpdateTip();
        AppPreference appPreference;
        Updater updater;

        public static bool isSDKInit = false;

        // const string
        const string MSG_LOGIN_START = "GAME_LOGIN_START";
        const string SDK_MANAGER_CLASS_NAME = "SDKManager";

        public List<PackageAsset> GetCurrentUpdateAssets()
        {
            return updater != null ? updater.GetCurrentUpdateAssets() : null;
        }

        void Awake()
        {
            uiLogicBase = GetComponent<UpdateUILogicBase>();

            //set this device optimized
            transform.position = new Vector3(9999, 0, 0);

            // support https with http request
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => { return true; });
        }

        private void RefreshAppServer()
        {
            if (appPreference == null || string.IsNullOrEmpty(appPreference.server))
            {
                return;
            }

            PlayerPrefs.SetString("LOGIN_SERVER", appPreference.server);
            AssetLogger.Log("[UpdateController]获取到APP里面默认设置的服务器 " + appPreference.server, "Net");
        }

        /// <summary>
        /// 初始化log提示，增加后门判断，提供上线版本可以打开日志系统
        /// </summary>
        void InitDebuger()
        {
            string backDoorFilePath = AssetPath.persistentDataPath + AssetPath.DirectorySeparatorChar + "log.txt";
            if (File.Exists(backDoorFilePath))
            {
                //TTDebuger.Log("存在log后门文件，激活Log!");
                return;
            }

            if ((appPreference == null || appPreference.log.Equals("enable", StringComparison.OrdinalIgnoreCase) == false))
            {
                // 只有在非编辑器模式才会禁用logger
                if (Application.isEditor == false)
                {
                    //Debug.Log("运行时设置了屏蔽log输出");
                    //TTDebuger.EnableLog = false;
#if UNITY_2017_2_OR_NEWER
                    UnityEngine.Debug.unityLogger.logEnabled = false;
#else
                    UnityEngine.Debug.logger.logEnabled = false;
#endif
                    return;
                }
            }

            //TTDebuger.Log("激活运行时log");
        }

        // Use this for initialization
        IEnumerator Start()
        {
            // 一定要第一步执行,需要根据这个来控制是否显示打开log
            appPreference = AppPreference.LoadFromResources();
            InitDebuger();

            // 安卓服务相关的初始化
            Android.Init();

            yield return null;

            // 加载项目的更新提示文档刷新，支持多语言
            tipContent.Load();

            if (appPreference == null)
            {
                PopUpOneBtnTip(tipContent.APP_ERROR, tipContent.BTN_CONFIM, () =>
                {
                    Application.Quit();
                });
                yield break;
            }

            AssetLogger.Log("初始更新界面,优化Device显示性能,可能缩放分辨率", "Net");
            DeviceOptimizer.InitDeviceOptimized(DeviceOptimizer.Mode.Android);

            // 记录下app里面设定的
            RefreshAppServer();

            yield return null;
            yield return null;
            yield return null;

            DoUpdate();
        }

        void DoUpdate()
        {
            uiLogicBase.Reset();
            StartCoroutine(IEDoUpdate());
        }

        IEnumerator IEDoUpdate()
        {
            updater = new Updater();
            yield return null;

            if (appPreference == null)
            {
                AssetLogger.LogException("app preference load from resources null.", "Net");

                PopUpOneBtnTip(tipContent.APP_ERROR, tipContent.BTN_CONFIM, () =>
                {
                    Application.Quit();
                });
                yield break;
            }

            // 如果需要sdk初始化的消息，那么要等sdk初始化完回调哦
            if (appPreference.channel.Equals("none") == false)
            {
                AssetLogger.Log("等待SDK=" + appPreference.channel + "初始化......", "Net");
                uiLogicBase.RefreshProcessTip(tipContent.SDK_INIT);

                float timer = 0f;
                while (isSDKInit == false)
                {
                    if (InitSDK() == false)
                    {
                        PopUpOneBtnTip(tipContent.SDK_INIT_ERROR, tipContent.BTN_CONFIM, () =>
                        {
                            Application.Quit();
                        });
                    }
                    yield return null;
                    timer += Time.deltaTime;
                    if (timer >= 60.0f)
                    {
                        AssetLogger.LogError("timeout sdk init", "Net");
                        PopUpOneBtnTip(tipContent.SDK_INIT_ERROR, tipContent.BTN_CONFIM, () =>
                        {
                            Application.Quit();
                        });
                        yield break;
                    }
                }
            }
            else
            {
                AssetLogger.LogWarning("没有SDK的需要初始化.", "Net");
            }

            uiLogicBase.RefreshProcessTip(tipContent.CHECK_RESOURCES);

            // 2016.09.21
            // TODO:不再维护版本更新
            if (appPreference.noUpdate.Equals("yes"))
            {
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    // 也支持检查web版本，仅仅审核版本校验需要识别review version
                    Updater.isNoUpdate = true;
                }
                else
                {
                    AssetLogger.Log(Color.yellow, "APP 没有更新功能，直接完成!", "Net");
                    OnFinish(true);
                    yield break;
                }
            }

            updater.Check(ret =>
            {
                AssetLogger.Log("检查文件更新结果：" + ret.ToString(), "Net");

                if (ret == Updater.State.NEWAPP_TIP)
                {
                    PopUpOneBtnTip(tipContent.NEW_APP_ONLY_TIP, tipContent.BTN_CONFIM, () =>
                    {
                        Application.Quit();
                    });
                }
                else if (ret == Updater.State.NEWAPP)
                {
                    // 弹提示让玩家确认更新下载
                    float appmb = updater.GetNewAppSizeMB();
                    string content = tipContent.NEW_APP + (appmb > 0f ? appmb + "mb" : "");
                    if (Updater.netKind != Updater.NetKind.WIFI)
                    {
                        content += "\n" + tipContent.NOT_WIFI_WARNNING;
                    }

                    PopUpOneBtnTip(content, tipContent.BTN_CONFIM, () =>
                    {
                        updater.DoAppInstallNew((p, mb) =>
                        {
                            uiLogicBase.SetProcessValue(p);
                            uiLogicBase.RefreshProcessTip(tipContent.DOWNLOADING + mb);
                        }, (state) =>
                        {
                            if (state == Updater.InstallAPPResult.DownloadError)
                            {
                                PopUpOneBtnTip(tipContent.DOWNLOAD_ERROR, tipContent.BTN_RESTART, DoUpdate);
                            }
                            else if (state == Updater.InstallAPPResult.URLNotExist)
                            {
                                PopUpOneBtnTip(tipContent.NEW_APP_ERROR_NO_URL, tipContent.BTN_RESTART, DoUpdate);
                            }
                            else if (state == Updater.InstallAPPResult.Finished)
                            {
                                PopUpOneBtnTip(tipContent.NEW_APP_OK, tipContent.BTN_CONFIM, () =>
                                {

                                    Application.Quit();
                                });
                            }
                        });
                    });
                }
                else if (ret == Updater.State.NeedUpdate)
                {
                    uiLogicBase.RefreshProcessTip(tipContent.CHECK_RESOURCES);

                    Action<float, string> _onUpdateProcess = (p, mb) =>
                    {
                        //TTDebuger.LogWarning("[UPGRADE] ...." + p, "Net");
                        uiLogicBase.SetProcessValue(p);
                        uiLogicBase.RefreshProcessTip(tipContent.DOWNLOADING + mb);
                    };

                    Action<string> _onError = (error) =>
                    {
                        AssetLogger.LogError("[UPGRADE] error = " + error, "Net");
                        uiLogicBase.RefreshProcessTip(tipContent.DOWNLOAD_ERROR);
                        OnFinish(false);
                    };

                    // check size.
                    updater.CheckAppUpgradeSize(_onUpdateProcess, _onError,
                        (_size) =>
                        {
                            if (Updater.netKind == Updater.NetKind.WIFI)
                            {
                                updater.DoAppUpgrade(_onUpdateProcess, _onError, () =>
                                {
                                    AssetLogger.LogError("[UPGRADE] done", "Net");
                                    uiLogicBase.RefreshProcessTip(tipContent.DOWNLOAD_OK);
                                    OnFinish(true);
                                });
                            }
                            else
                            {
                                if (_size >= 1024 * 1024)
                                {
                                    float seed = 1024 * 1024;
                                    float totalMB = float.Parse((_size / seed).ToString("F2"));
                                    PopUpTip(tipContent.NEW_RESOURCES + totalMB + "mb\n" + tipContent.NOT_WIFI_WARNNING,
                                        tipContent.BTN_CANCLE, tipContent.BTN_CONFIM, () =>
                                        {
                                            Application.Quit();
                                        }, () =>
                                        {
                                            updater.DoAppUpgrade(_onUpdateProcess, _onError, () =>
                                            {
                                                AssetLogger.LogError("[UPGRADE] done", "Net");
                                                uiLogicBase.RefreshProcessTip(tipContent.DOWNLOAD_OK);
                                                OnFinish(true);
                                            });
                                        });
                                }
                                else
                                {
                                    updater.DoAppUpgrade(_onUpdateProcess, _onError, () =>
                                    {
                                        AssetLogger.LogError("[UPGRADE] done", "Net");
                                        uiLogicBase.RefreshProcessTip(tipContent.DOWNLOAD_OK);
                                        OnFinish(true);
                                    });
                                }
                            }
                        });
                }
                else if (ret == Updater.State.PASS)
                {
                    uiLogicBase.RefreshProcessTip(tipContent.PASS);
                    OnFinish(true);
                }
                else //if(ret == TTUpgradeState.ERROR)
                {
                    uiLogicBase.RefreshProcessTip(tipContent.DOWNLOAD_ERROR);

                    // 如果是测试模式
                    // 那么可以不需要更新
                    if (appPreference.demo.Equals("no") == false)
                    {
                        AssetLogger.LogWarning("========测试模式，更新出错依旧直接进游戏，没有这个版本的服务器资源也一样进入======", "Net");
                        OnFinish(true);
                    }
                    else
                    {
                        AssetLogger.LogError("更新出错APP Preference=" + appPreference.ToJson(false), "Net");

                        updater.CheckNetworkIsReady(netstate =>
                        {
                            if (netstate == false)
                            {
                                PopUpOneBtnTip(tipContent.NETWORK_NOT_OK, tipContent.BTN_RESTART, DoUpdate);
                            }
                            else
                            {
                                PopUpOneBtnTip(tipContent.DOWNLOAD_SERVER_VERSION_ERROR, tipContent.BTN_RESTART, DoUpdate);
                            }
                        });
                    }
                }
            });
        }

        void OnDisable() { }

        void OnFinish(bool isPass)
        {
            if (isPass)
            {

                uiLogicBase.SetProcessValue(1.0f);
                uiLogicBase.RefreshProcessTip(tipContent.ENTER_SCENE_LOADING);

                StartCoroutine(IEDelayCall(0, () =>
                {
                    //add listen to close.
                    Messenger.AddListener(MSG_LOGIN_START, CallbackLogin);

                    // DO LOGIN START
                    if (callLogin != null) callLogin();
                    else
                    {
                        AssetLogger.LogError("没有设置callLogin的回调处理!", "Net");
                    }

                }));

            }
            else
            {
                updater.CheckNetworkIsReady(netstate =>
                {
                    if (netstate == false)
                    {
                        PopUpOneBtnTip(tipContent.NETWORK_NOT_OK, tipContent.BTN_RESTART, DoUpdate);
                    }
                    else
                    {
                        PopUpOneBtnTip(tipContent.DOWNLOAD_ERROR, tipContent.BTN_RESTART, DoUpdate);
                    }
                });
            }
        }

        /// <summary>
        /// 通知关掉更新界面
        /// </summary>
        void CallbackLogin()
        {
            Messenger.RemoveListener(MSG_LOGIN_START, CallbackLogin);

            StartCoroutine(IEDelayCall(0, () =>
            {
                // dismiss effect...
                //gameObject.SendMessage("StartDeadEffect", SendMessageOptions.DontRequireReceiver);
                GameObject.Destroy(this.gameObject);
            }));
        }

        #region sdk init

        /// <summary>
        /// 检查是否需要有sdk初始化
        /// 就是项目中有存在sdk，就需要先初始化完，确认sdk不会弹它的热更的判断
        /// </summary>
        /// <returns></returns>
        public bool InitSDK()
        {
            if (Application.isEditor) return false;

            if (isSDKInit) return true;

            Type type = FindType(SDK_MANAGER_CLASS_NAME);
            if (type != null)
            {
                System.Reflection.MethodInfo init = type.GetMethod("Init", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | BindingFlags.NonPublic);
                if (init == null)
                {
                    AssetLogger.LogError("找不到SDKManager.Init", "Net");
                    return false;
                }
                init.Invoke(null, null);
                return true;
            }
            else
            {
                return false;
            }
        }

        Type FindType(string typeFullName)
        {
            Type type = Type.GetType(typeFullName);

            //try found in unity asset dlls.
            if (type == null)
            {
                Assembly assembly = Assembly.Load("Assembly-CSharp");
                if (assembly != null)
                {
                    type = assembly.GetType(typeFullName);
                }
            }

            if (type == null)
            {
                Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int n = 0; n < Assemblies.Length; n++)
                {
                    Assembly asm = Assemblies[n];
                    type = asm.GetType(typeFullName);
                    if (type != null)
                        return type;
                }
            }

            if (type == null)
            {
                AssetLogger.LogError("Cant found right type in dlls,Type = " + typeFullName, "Net");
                return null;
            }

            return type;
        }

        #endregion

        #region Refresh UI

        void PopUpOneBtnTip(string content, string confimBtn, Action confim)
        {
            uiLogicBase.PopUpOneBtnTip(content, confimBtn, confim);
        }

        void PopUpTip(string content, string cancleBtn, string confimBtn, Action cancle, Action confim)
        {
            uiLogicBase.PopUpTip(content, cancleBtn, confimBtn, cancle, confim);
        }

        IEnumerator IEDelayCall(float time, Action callback)
        {
            if (time <= 0f)
                yield return null;
            else
                yield return new WaitForSeconds(time);

            if (callback != null) callback();
        }

        #endregion

    }

}