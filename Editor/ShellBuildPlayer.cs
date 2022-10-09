using System;
using System.Collections.Generic;
using System.IO;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Package;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace SuperMobs.AssetManager.Editor
{
    /*
     * 输出客户端
     * */

    public partial class ShellBuilder
    {
        // 处理完，在buildplayer前回调
        public static Action<ShellBuilder> beforeBuildPlayer;

        public string companyName;
        public string appName;
        public string bundleid;
        public string version;
        public string sdk;
        public string channel;
        public string platform;
        public string truebuild;
        public string resVer;
        // c#代码版本号自动增加
        public int codeVer;
        // 打包号,每次打包增加,用于平台上传区分新旧,用户不可见
        public int buildNum;
        public string log;
        public string buildPath;

        // android encrypt build
        public string encrypt;

        // web check info url
        public string web;
        public string cdn;

        // 是否允许更新出错也进游戏
        // yes  | no
        public string demo;

        // 是否停止提供更新检查，例如上线没有维护了。。
        // yes  | no
        public string noUpdate;

        // 设置里面填写的服务器登
        public string server;

        // unity 宏定义
        public string symbols;

        // cleanup after builded
        static string cleanup = "false";

        string[] GetBuildScenes()
        {
            List<string> names = new List<string>();

            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (e == null)
                    continue;
                if (e.enabled)
                    names.Add(e.path);
            }
            return names.ToArray();
        }

        static string PreferenceConfigPath
        {
            get
            {
                return Application.dataPath + "/Resources/version.txt";
            }
        }

        private void InitBuildPlayerSetting()
        {
            companyName = GetArg("companyName") ?? "supermobs";
            appName = GetArg("appName") ?? "NONAME";
            bundleid = GetArg("bundleid") ?? "com.supermobs.demo";
            platform = GetArg("platform") ?? AssetPath.GetBuildTargetPlatform();
            truebuild = GetArg("truebuild") ?? "true";
            sdk = GetArg("sdk") ?? "default";
            channel = GetArg("channel") ?? "none";
            version = GetArg("version") ?? "0.0";
            //resVer = GetArg("resVer") ?? "0"; //fix 不再传进来，通过制作资源package时候产生
            var cpp = GetArg("cpp") ?? "false"; // code plus plus
            codeVer = PackageScriptVersion.GenCodeNum(!cpp.Equals("false"), platform);
            buildNum = PackageScriptVersion.GenBuildNum(platform);
            log = GetArg("log") ?? "enable";

            if (platform == AssetPreference.PLATFORM_IOS || platform == AssetPreference.PLATFORM_ANDROID)
            {
                buildPath = GetArg("buildpath") ?? AssetPath.ProjectRoot + "buildplayer/" + platform;
            }
            else if (platform == AssetPreference.PLATFORM_STANDARD)
            {
                buildPath = GetArg("buildpath") ?? AssetPath.ProjectRoot + "buildplayer/" + platform;
            }

            encrypt = GetArg("encrypt") ?? "no";

            cleanup = GetArg("cleanup") ?? "true";

            web = GetArg("web") ?? "";
            cdn = GetArg("cdn") ?? "";

            demo = GetArg("demo") ?? "yes";
            noUpdate = GetArg("noUpdate") ?? "yes";

            server = GetArg("server") ?? "";

            symbols = GetArg("appSymbols") ?? "";

            AssetBuilderLogger.Log("start build player with>" +
               "\n" + "company:" + companyName +
               "\n" + "appName:" + appName +
               "\n" + "bundleid:" + bundleid +
               "\n" + "platform:" + platform +
               "\n" + "sdk:" + sdk +
               "\n" + "channel:" + channel +
               "\n" + "version:" + version +
               //"\n" + "resVer:" + resVer +
               "\n" + "codeVer:" + codeVer +
               "\n" + "buildNum:" + buildNum +
               "\n" + "log:" + log +
               "\n" + "cdn:" + cdn +
               "\n" + "web:" + web +
               "\n" + "server:" + server +
               "\n" + "symbols:" + symbols +
               "\n" + "buildPath:" + buildPath);
        }

        private void InitSetting()
        {
            ChangePlatform();
            InitShellBuildSetting();
            InitBuildPlayerSetting();
        }

        private void InitReadyToBuild()
        {
            InitSetting();
            NewPackage(); //NOTE: gen resVer here!
            ProjectSetting();
            WriteAppPreferenceConfig();
            ShellSDK.InitSDK(setting, sdk, channel);
        }

        private void RunBuildPlayer()
        {
            InitReadyToBuild();

            if (beforeBuildPlayer != null)
            {
                beforeBuildPlayer(this);
            }

            if (platform == AssetPreference.PLATFORM_IOS)
            {
                BuildIOS();
            }
            else if (platform == AssetPreference.PLATFORM_ANDROID)
            {
                BuildAndroid();
            }
            else if (platform == AssetPreference.PLATFORM_STANDARD)
            {
                BuildPC();
            }
            else
            {
                throw new Exception("Cant Build UNKNOWN platform:" + platform);
            }

            AssetBuilderLogger.Log(Color.green, "yeh,build player for " + platform + " done.");
        }

        internal void NewPackage()
        {
            Packager pk = new Packager();
            pk.sdk = sdk;
            pk.platform = platform;
            pk.GenNewServerPackage();
            pk.GenNewClientPackage();

            resVer = pk.packageManifestCrc.ToString();
        }

        internal void ProjectSetting()
        {
            // -------------------------------------------
            // 修改 PlayerSettings 的build id和bundle号
            // Android should modify the AndroidManifest.xml when builded.
            // -------------------------------------------
            PlayerSettings.productName = appName;
            PlayerSettings.companyName = companyName;
            PlayerSettings.bundleVersion = version + "." + codeVer;

            // NOTE:每次编译都会不一样
            // Build Number 对用户不可见,用于平台上传每次编译都会++(用于上传)不一样
            PlayerSettings.Android.bundleVersionCode = buildNum;
            PlayerSettings.iOS.buildNumber = buildNum.ToString();

#if UNITY_5_6_OR_NEWER || UNITY_2017_2_OR_NEWER
            PlayerSettings.applicationIdentifier = bundleid;
            if (platform == AssetPreference.PLATFORM_IOS)
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, bundleid);
            }
            else if (platform == AssetPreference.PLATFORM_ANDROID)
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, bundleid);
            }
            else if (platform == AssetPreference.PLATFORM_STANDARD)
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, bundleid);
            }
#else
         PlayerSettings.bundleIdentifier = bundleid;
#endif
        }

        /// <summary>
        /// 写入app的preference
        /// TO:用于客户端更新校验
        /// </summary>
        void WriteAppPreferenceConfig()
        {
            FileInfo fi = new FileInfo(PreferenceConfigPath);
            if (fi.Directory.Exists == false) fi.Directory.Create();
            if (fi.Exists) fi.Delete();

            AppPreference preference = new AppPreference();
            preference.version = version + "." + codeVer + "." + resVer;
            preference.sdk = sdk;
            preference.channel = channel;
            preference.log = log;
            preference.cdn = cdn;
            preference.web = web;
            preference.demo = demo;
            preference.noUpdate = noUpdate;
            preference.server = server;

            preference.Save(PreferenceConfigPath);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// 1、准备package
        /// 2、准备sdk-channel的资源处理
        /// 3、设置好unity项目导出设置
        /// 4、写入app preference
        /// 4、buildplayer
        /// 5、build xcode项目做回调处理资源
        /// </summary>
        internal void BuildIOS()
        {
            if (truebuild == "true")
            {
                DirectoryInfo di = new DirectoryInfo(buildPath);
                if (di.Exists == false) di.Create();

                BuildPipeline.BuildPlayer(GetBuildScenes(), buildPath, BuildTarget.iOS, EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None);
            }
        }

        internal void BuildAndroid()
        {
            if (truebuild == "true")
            {
                DirectoryInfo di = new DirectoryInfo(buildPath);
                if (di.Exists == false) di.Create();

                string apkName = version + "." + codeVer +
                   "." + sdk +
                   "." + log +
                   (demo.Equals("yes") ? ".demo" : "") +
                   (noUpdate.Equals("yes") ? ".noupdate" : "") +
                   ".apk";
                if (encrypt == "yes")
                {
                    // ouput google project

                }
                else
                {
                    BuildPipeline.BuildPlayer(GetBuildScenes(), buildPath + "/" + apkName, BuildTarget.Android, EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None);
                }
            }
        }

        internal void BuildPC()
        {
            if (truebuild == "true")
            {
                DirectoryInfo di = new DirectoryInfo(buildPath);
                if (di.Exists == false) di.Create();

                string apkName = appName + ".exe";
                if (encrypt == "yes")
                {
                    // ouput google project

                }
                else
                {
                    if (Directory.Exists(buildPath))
                    {
                        Directory.Delete(buildPath, true);
                    }

                    BuildPipeline.BuildPlayer(GetBuildScenes(), buildPath + "/" + apkName, BuildTarget.StandaloneWindows64, EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None);
                }
            }
            else
            {
                Debug.LogError("truebuild:"+truebuild + " check");
            }
        }

        #region PostProcessBuild Callback

        /// <summary>
        /// if ios platform need uncomress ab copy to xcode build Data folder.
        /// ios DONT!! import unity assets to Assets/...
        /// </summary>
        [PostProcessBuild(101)]
        static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.Android)
            {
                if (pathToBuiltProject.EndsWith(".apk", StringComparison.Ordinal) == false)
                {

                    string androidDataFolder = pathToBuiltProject + "/" + PlayerSettings.productName + "/assets/" +
                    						   AssetPath.ASSETBUNDLE_DEVICE_FOLDER + AssetPath.DirectorySeparatorChar;

                    //Directory.Move(AssetPath.AssetBundleEditorDevicePath, androidDataFolder);

                    ////删掉目录下的meta文件
                    //string[] files = Directory.GetFiles(androidDataFolder);
                    //foreach (string file in files)
                    //{
                    //    if (file.EndsWith(".meta", StringComparison.Ordinal))
                    //    {
                    //        File.Delete(file);
                    //    }
                    //}

                    // android ...
                }
            }
            else if (target == BuildTarget.iOS) // ios
            {
                //得到xcode工程的路径
                //string path = Path.GetFullPath(pathToBuiltProject);
                string xcodeDataFolder = pathToBuiltProject + AssetPath.DirectorySeparatorChar + "Data/" +
                   AssetPath.ASSETBUNDLE_DEVICE_FOLDER + AssetPath.DirectorySeparatorChar;

                if (Directory.Exists(AssetPath.AssetBundleEditorDevicePath))
                {
                    // 确保xcode里面资源目录是最新的
                    if (Directory.Exists(xcodeDataFolder))
                    {
                        Directory.Delete(xcodeDataFolder, true);
                    }

                    Directory.Move(AssetPath.AssetBundleEditorDevicePath, xcodeDataFolder);

                    //删掉目录下的meta文件
                    string[] files = Directory.GetFiles(xcodeDataFolder);
                    foreach (string file in files)
                    {
                        if (file.EndsWith(".meta", StringComparison.Ordinal))
                        {
                            File.Delete(file);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("不存在ios准备的package资源：" + AssetPath.AssetBundleEditorDevicePath + ".");
                    Debug.LogWarning("此次发包不是通过自动打包生成res吧?");
                }

                // disable the bitcode
                DisableBitcode(target, pathToBuiltProject);
            }
            else if (target == BuildTarget.StandaloneWindows64)
            {
                Debug.Log("需要处理拷贝资源到res文件夹");
                string datafile = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf(".", StringComparison.Ordinal)) + "_Data/" + AssetPath.DirectorySeparatorChar +
                  AssetPath.ASSETBUNDLE_DEVICE_FOLDER + AssetPath.DirectorySeparatorChar;
                Debug.Log(datafile);
                Debug.Log(AssetPath.AssetBundleEditorDevicePath);

                if (Directory.Exists(AssetPath.AssetBundleEditorDevicePath))
                {
                    // 确保资源目录是最新的
                    if (Directory.Exists(datafile))
                    {
                        Directory.Delete(datafile, true);
                    }

                    Directory.Move(AssetPath.AssetBundleEditorDevicePath, datafile);

                    //删掉目录下的meta文件
                    string[] files = Directory.GetFiles(datafile);
                    foreach (string file in files)
                    {
                        if (file.EndsWith(".meta", StringComparison.Ordinal))
                        {
                            File.Delete(file);
                        }
                    }
                }


            }

            if (cleanup == "true") Cleanup();
        }

        /// <summary>
        /// 目前情况下不需要bitcode
        /// !因为luajit编码目前不支持bitcode
        /// </summary>
        static void DisableBitcode(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) return;

#if UNITY_IOS
         string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
         PBXProject proj = new PBXProject();
         proj.ReadFromFile(projPath);

      
         string nativeTarget = proj.GetUnityMainTargetGuid();
         string testTarget = proj.TargetGuidByName(PBXProject.GetUnityTestTargetName());
         string[] buildTargets = new string[] { nativeTarget, testTarget };

         proj.ReadFromString(File.ReadAllText(projPath));
         proj.SetBuildProperty(buildTargets, "ENABLE_BITCODE", "NO");
         File.WriteAllText(projPath, proj.WriteToString());
#endif

        }

        static void Cleanup()
        {
            AssetBuilderLogger.Log("cleanup after build player!");

            // delete preference config
            if (File.Exists(PreferenceConfigPath)) File.Delete(PreferenceConfigPath);

            // cleanup the assetbundle raw path
            DirectoryInfo di = new DirectoryInfo(AssetPath.AssetBundleEditorDevicePath);
            if (di.Exists)
            {
                di.Delete(true);
            }

            // cleanup sdk assets
            ShellSDK.Cleanup();

            AssetDatabase.Refresh();
        }

        #endregion

    }
}