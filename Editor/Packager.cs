using System;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Assets;
using SuperMobs.Core;
using SuperMobs.AssetManager.Loader;
using UniRx;
using SuperMobs.AssetManager.Package;

namespace SuperMobs.AssetManager.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    /*
	 * 生成当前发布版本文件列表
	 * 1、压缩+加密 当前的bundles
	 * 2、生成Bundles的包列表
	 * 3、生成一份当前程序的版本号信息文件
	 * 4、大文件系统
	 * 
	 * */

    public class Packager
    {
        const bool IS_PACK_BIG_FILE = true;

        // 特殊开发标记符:special dev key
        public string sdk;

        // 当前发布的平台
        // 默认通过AssetPath获取
        public string platform;

        public uint packageManifestCrc = 0;

        public Packager()
        {
            sdk = "default";
        }

        /// <summary>
        /// output server upload bundles version folder
        /// upload thoes output files to cdn
        /// </summary>
        public string AssetBundleServerPath
        {
            get
            {
                if (!string.IsNullOrEmpty(sdk))
                {
                    return AssetPath.ProjectRoot + sdk + "." + platform + AssetPath.DirectorySeparatorChar;
                }
                else
                    return AssetPath.ProjectRoot + platform + ".server" + AssetPath.DirectorySeparatorChar;
            }
        }

        void ReadyOutputFolder()
        {
            if (Directory.Exists(AssetBundleServerPath)) Directory.Delete(AssetBundleServerPath, true);
            if (!Directory.Exists(AssetBundleServerPath)) Directory.CreateDirectory(AssetBundleServerPath);

            if (Directory.Exists(AssetPath.AssetBundleEditorDevicePath)) Directory.Delete(AssetPath.AssetBundleEditorDevicePath, true);
            if (!Directory.Exists(AssetPath.AssetBundleEditorDevicePath)) Directory.CreateDirectory(AssetPath.AssetBundleEditorDevicePath);

            AssetBuilderLogger.Log(Color.green, "AssetBundleAppPath =" + AssetPath.AssetBundleEditorDevicePath);
            AssetBuilderLogger.Log(Color.green, "AssetBundleServerPath = " + AssetBundleServerPath);
        }

        AssetManifest LoadAssetManifest()
        {
            if (Service.IsSet<LoaderService>() == false)
            {
                Service.Set<LoaderService>(new LoaderService());
            }

            var m = Service.Get<LoaderService>().GetAssetManifestLoader().Load(string.Empty) as AssetManifest;
            return m;
        }

        PackageManifest LoadPackageManifest(string packageManifestPath)
        {
            var ab = AssetBundle.LoadFromFile(packageManifestPath,0,AssetPreference.ENCRYPT_AB ? (ulong)10 : 0);
            var text = ab.LoadAllAssets()[0] as TextAsset;
            var bytes = text != null ? text.bytes : null;
            if (bytes == null)
            {
                ab.Unload(false);
                AssetLogger.LogException("cant load package manifest,ab is ok,but cant load asset.");
                return null;
            }

            var _manifest = new PackageManifest();
            _manifest.FromStreamBytes(bytes);
            ab.Unload(false);
            return _manifest;
        }

        static long TimeToLong()
        {
            return long.Parse(System.DateTime.Now.ToString("yyyyMMddHHmm"));
        }

        static bool IsClientBigFile()
        {
            // FIX:android contain some bug with one big file.
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS && IS_PACK_BIG_FILE;

            //return IS_PACK_BIG_FILE;
        }

        string FindPackageManifestInServer()
        {
            string filePrefix = Crc32.GetStringCRC32(AssetPath.PACKAGE_MANIFEST_FILE).ToString();
            var infoPaths = AssetEditorHelper.CollectAllPath(AssetBundleServerPath, filePrefix + "_*" + AssetPath.ASSETBUNDLE_SUFFIX);
            if (infoPaths.Length > 1)
                throw new Exception("居然找到2个packageManifest文件？ : " + infoPaths.ToArrayString());

            return infoPaths.Length > 0 ? infoPaths[0] : string.Empty;
        }

        string SavePackageManifestInServer(PackageManifest pm)
        {
            string assetPath = Application.dataPath + "/" + AssetPath.PACKAGE_MANIFEST_FILE + ".bytes";
            if (File.Exists(assetPath)) File.Delete(assetPath);
            pm.SaveStreamAssetToFile(assetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // NOTE:只有这里确保二进制出来的是一样的crc
            uint fileCrc = Crc32.GetFileCRC32(assetPath);
            packageManifestCrc = fileCrc;

            var teamAbName = Crc32.GetStringCRC32(AssetPath.PACKAGE_MANIFEST_FILE) + "_XXX" + AssetPath.ASSETBUNDLE_SUFFIX;

            AssetBundleBuild abb = new AssetBundleBuild();
            abb.assetBundleName = teamAbName;
            abb.assetNames = new string[] { "Assets/" + AssetPath.PACKAGE_MANIFEST_FILE + ".bytes" };
            BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.DisableWriteTypeTree;
            BuildPipeline.BuildAssetBundles(AssetBundleServerPath,
                                            new AssetBundleBuild[] { abb },
                                            assetBundleOptions,
                                            EditorUserBuildSettings.activeBuildTarget);

            // cleanup
            AssetEditorHelper.CleanUnityBuildAssetBundleManifest(AssetBundleServerPath);
            if (File.Exists(assetPath)) File.Delete(assetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (File.Exists(AssetBundleServerPath + teamAbName) == false) throw new Exception("打包package.manifest的ab不成功!");
            //uint fileCrc = Crc32.GetFileCRC32(AssetBundleServerPath + teamAbName);
            string newPath = AssetBundleServerPath + teamAbName.Replace("XXX", fileCrc.ToString());
            File.Move(AssetBundleServerPath + teamAbName, newPath);

            AssetBuilderLogger.Log("save package.manifest.ab done at " + newPath);

            return newPath;
        }

        /// <summary>
        /// 保存packageversion文件
        /// 当前发布版本的信息
        /// </summary>
        /// <param name="pm">当前需要获取的packagemanifest文件位置</param>
        /// <param name="abPath">保存version文件位置</param>
        void SaveServerPackageVersion(uint packageManifestCrc, string abPath)
        {
            var version = new PackageVersion();
            version.time = TimeToLong();
            version.version = packageManifestCrc;
            version.svnVer = packageManifestCrc.ToString();

            AssetBuilderLogger.Log("save package version at " + abPath + "\n" + version.ToJson(false));

            // save the text asset
            version.SaveStreamAssetToFile(abPath);

            // save assetbundle cached.
            AssetEditorHelper.BuildTextAssetToAssetBundle(abPath, abPath);
        }

        public void GenNewServerPackage()
        {
            ReadyOutputFolder();
            var manifest = LoadAssetManifest();
            if (manifest == null) throw new Exception("AssetManifest cant load when gen new package.");

            // ！构造这个包的文件信息
            var package = new PackageManifest();

            var bundleCollector = manifest.GetBundleCollector();
            bundleCollector
                .names
                .ToObservable(Scheduler.Immediate)
                .Select(name => manifest.FindBundle(name))
                .Do(bundle =>
                {
                    package.AddBundle(bundle);
                })
                .Do(bundle =>
                {
                    string file = AssetPath.AssetbundlePath + bundle.bundleName;
                    string server = AssetBundleServerPath + bundle.bundleNameCrc + "_" + Crc32.GetFileCRC32(file) + AssetPath.ASSETBUNDLE_SUFFIX;
                    File.Copy(file, server, true);
                })
                .TakeLast(1)
                .Do(_ =>
                {
                    // copy the asset manifest to server output.
                    string file = AssetPath.AssetbundlePath + AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX;
                    uint amName = Crc32.GetStringCRC32(AssetPath.MANIFEST_FILE + AssetPath.ASSETBUNDLE_SUFFIX);
                    uint amCrc = Crc32.GetFileCRC32(file);
                    string server = AssetBundleServerPath + amName + "_" + amCrc + AssetPath.ASSETBUNDLE_SUFFIX;
                    File.Copy(file, server, true);

                    // add asset manifest file to package manifest.
                    package.AddAssetManifest();

                    // create the pm
                    SavePackageManifestInServer(package);

                    // create the package version
                    string pvPath = AssetBundleServerPath
                                         + Crc32.GetStringCRC32(AssetPath.PACKAGE_VERSION_FILE)
                                         + "_" + packageManifestCrc
                                         + AssetPath.ASSETBUNDLE_SUFFIX;

                    SaveServerPackageVersion(packageManifestCrc, pvPath);

                    WriteServerResVersionText();
                    WriteServerResInfo(package);

                    if (AssetPreference.ENCRYPT_AB)
                    {
                        EncryptAssetBundles(AssetBundleServerPath);
                    }
                })
                .Subscribe();
        }

        /// <summary>
        /// 把当前asset version写入一个text文件
        /// </summary>
        void WriteServerResVersionText()
        {
            var file = AssetBundleServerPath + "version.txt";
            if (File.Exists(file)) File.Delete(file);
            FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write);
            var content = Encoding.Default.GetBytes(packageManifestCrc.ToString());
            fs.Write(content, 0, content.Length);
            fs.Close();

            // 复制一份到根目录，给jenkins读取
            File.Copy(file, AssetPath.ProjectRoot + platform + "_server_resource_version.config", true);
        }

        class ServerResInfo : SuperJsonObject
        {
            [Serializable]
            public class Item
            {
                public string name;
                public int size;
                public Item(string name, int size)
                {
                    this.name = name;
                    this.size = size;
                }
            }

            public uint version;
            public List<Item> list;
        }
        /// <summary>
        /// 资源文件信息写成json
        /// </summary>
        /// <param name="package"></param>
        void WriteServerResInfo(PackageManifest package)
        {
            ServerResInfo info = new ServerResInfo();
            info.version = packageManifestCrc;

            var list = new List<ServerResInfo.Item>();
            var enumerator = package.assets.GetEnumerator();
            while (enumerator.MoveNext())
                list.Add(new ServerResInfo.Item(
                    enumerator.Current.nameCrc + "_" + enumerator.Current.fileCrc + AssetPath.ASSETBUNDLE_SUFFIX,
                    enumerator.Current.fileLength));
            info.list = list;

            var file = AssetBundleServerPath + "version.json";
            if (File.Exists(file)) File.Delete(file);
            FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write);
            var content = Encoding.Default.GetBytes(info.ToJson());
            fs.Write(content, 0, content.Length);
            fs.Close();
        }

        /// <summary>
        /// 根据当前库里面，必须有Server版本生成再处理
        /// 一般先server、后client
        /// </summary>
        public void GenNewClientPackage()
        {
            var packageManifestPath = FindPackageManifestInServer();
            if (File.Exists(packageManifestPath) == false) throw new Exception("cant gen new client ,because server asset not ready at " + packageManifestPath);

            var pm = LoadPackageManifest(packageManifestPath);
            pm
                .assets
                .ToObservable(Scheduler.Immediate)
                .Select(pa => AssetEditorHelper.CollectAllPath(AssetBundleServerPath, pa.nameCrc + "_*" + AssetPath.ASSETBUNDLE_SUFFIX))
                .Select(arr => arr[0])
                .Do(file =>
                {
                    // copy to client app
                    FileInfo fi = new FileInfo(file);
                    string client = AssetPath.AssetBundleEditorDevicePath + fi.Name.Substring(0, fi.Name.LastIndexOf("_", StringComparison.Ordinal)) + AssetPath.ASSETBUNDLE_SUFFIX;
                    File.Copy(file, client, true);
                })
                .TakeLast(1)
                .Do(_ =>
                {
                    // copy the current server package manifest to client.
                    FileInfo fi = new FileInfo(packageManifestPath);
                    string client = AssetPath.AssetBundleEditorDevicePath + fi.Name.Substring(0, fi.Name.LastIndexOf("_", StringComparison.Ordinal)) + AssetPath.ASSETBUNDLE_SUFFIX;
                    File.Copy(packageManifestPath, client, true);

                    // copy the current package version to client.
                    var serverVersionFile = AssetEditorHelper.CollectAllPath(AssetBundleServerPath, Crc32.GetStringCRC32(AssetPath.PACKAGE_VERSION_FILE) + "_*" + AssetPath.ASSETBUNDLE_SUFFIX);
                    if (serverVersionFile.Length != 1)
                    {
                        throw new Exception("制作客户端app内部资源出错，找不到匹配的服务器 packageversion文件!");
                    }

                    string pvServer = serverVersionFile[0];

                    string pvClient = AssetPath.AssetBundleEditorDevicePath
                                                  + Crc32.GetStringCRC32(AssetPath.PACKAGE_VERSION_FILE)
                                                  + AssetPath.ASSETBUNDLE_SUFFIX;

                    File.Copy(pvServer, pvClient, true);
                })
                .Where(_ => IsClientBigFile())
                .Do(_ => AssetBuilderLogger.Log("start gen big file in client!"))
                .Do(_ => GenClientBigFile())
                .DoOnCompleted(() => AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport))
                .Subscribe();

            AssetBuilderLogger.Log("gen client default app bundle done!");
        }

        #region big file pack

        void GenClientBigFile()
        {
            var files = AssetEditorHelper.CollectAllPath(AssetPath.AssetBundleEditorDevicePath,
                                                         "*" + AssetPath.ASSETBUNDLE_SUFFIX);

            PackBigFile bigFileEditor = new PackBigFile(AssetPath.AssetBundleEditorDevicePath +
                                                        Crc32.GetStringCRC32(AssetPath.BIG_FILE)
                                                        + AssetPath.ASSETBUNDLE_SUFFIX);

            // 获取client目录里面的文件
            files
                .ToObservable(Scheduler.Immediate)
                .Do(file =>
                {
                    FileInfo fi = new FileInfo(file);
                    uint n = uint.Parse(fi.Name.Substring(0, fi.Name.LastIndexOf(".", StringComparison.Ordinal)));
                    bigFileEditor.WriteFile(file, n);
                })
                .Do(file => File.Delete(file))
                .TakeLast(1)
                .Do(_ =>
                {
                    var path = AssetPath.AssetBundleEditorDevicePath +
                                                       Crc32.GetStringCRC32(AssetPath.BIG_FILE_MANIFEST) +
                                                       AssetPath.ASSETBUNDLE_SUFFIX;
                    bigFileEditor.WriteBigFileManifest(path);

                    if(AssetPreference.ENCRYPT_AB)
                    {
                        EncryptAssetBundle(path);
                    }
                })
                .Do(_ => AssetBuilderLogger.Log("pack client package to big file done!"))
                .Subscribe();
        }

        #endregion

        #region 加密ab,用于发布资源

        static void EncryptAssetBundles(string folder)
        {
            AssetBuilderLogger.Log(Color.cyan,"*********加密ab*********"); 
            var files = Directory.GetFiles(folder);
            foreach (var file  in files)
            {
                if(file.EndsWith(AssetPath.ASSETBUNDLE_SUFFIX) == false) continue;
                EncryptAssetBundle(file);
            }
        }

        static void EncryptAssetBundle(string file)
        {
            var bs1 = System.Text.Encoding.Default.GetBytes("chiuan");
            var bytes = File.ReadAllBytes(file);
            var bs2 = BitConverter.GetBytes((int)UnityEngine.Random.Range(0,uint.MaxValue));
            List<byte> ret = new List<byte>();
            ret.AddRange(bs1);
            ret.AddRange(bs2);
            ret.AddRange(bytes);
            File.WriteAllBytes(file,ret.ToArray());
        }

        static bool CheckHasEncryptAssetBundle(byte[] content)
        {
            if(content.Length <= 10) return false;

            if(System.Text.Encoding.Default.GetString(content,0,6).Equals("chiuan"))
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
