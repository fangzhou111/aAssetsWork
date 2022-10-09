#define UPDATE_THREAD_INIT

using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using SuperMobs.AssetManager.Package;
using UniRx;

namespace SuperMobs.AssetManager.Update {
   using System.Collections.Generic;
   using System.Collections;
   using System.IO;
   using System.Threading;
   using System;
   using UnityEngine;

   class ApplicationLife : MonoBehaviour {
      public bool isAlive = true;

      void Awake () {
         isAlive = true;
      }

      void OnDisable () {
         isAlive = false;
      }
   }

   internal class YieldUpdateAssets : CustomYieldInstruction {
      Action<float, string> actUpdate;

      PackageManifest appManifest;
      PackageManifest localManifest;
      PackageManifest serverManifest;

      List<PackageAsset> updateAssets = new List<PackageAsset> ();
      List<Downloader.UrlInfo> urlInfos = new List<Downloader.UrlInfo> ();

      // 下载器
      Downloader downloader = new Downloader ();

      // 资源下载url
      public string cdn = string.Empty;

      float seed = 1024 * 1024;

      ApplicationLife appLife = null;

      private bool forceQuit = false;

      public bool isInit = false;

      public long updateSize = 0;

      public YieldUpdateAssets (Action<float, string> actUpdate, PackageManifest app, PackageManifest local, PackageManifest server, string cdn) {
         this.cdn = cdn;
         this.actUpdate = actUpdate;

         this.appManifest = app;
         this.localManifest = local == null ? app : local; // 如果local没有，则用app的覆盖
         this.serverManifest = server;

         if (local == app) {
            AssetLogger.Log ("本地packageManifest 和 app的PackageManifest一致", "Net");
         }

         GameObject life = new GameObject ("APPLife");
         life.hideFlags = HideFlags.HideAndDontSave;
         appLife = life.AddComponent<ApplicationLife> ();

      }

      public override bool keepWaiting {
         get {
            // check downloader state
            if (downloader.state == Downloader.State.E_DOWNLOAD_ERROR) {
               return false;
            } else if (downloader.state == Downloader.State.E_DOWNLOAD_FINISHED) {
               AssetLogger.Log ("Update Download finished.", "Net");
               return false;
            } else if (downloader.state == Downloader.State.E_DOWNLOADING) {
               if (downloader.totalLength == 0.0f) {
                  actUpdate (0f, "");
               } else {
                  float p = downloader.receivedLength * 1.0f / downloader.totalLength;
                  float downMB = float.Parse ((downloader.receivedLength / seed).ToString ("F2"));
                  actUpdate (p, "(" + downMB.ToString("0.0") + "/" + downloader.totalMB.ToString("0.0") + "mb)");
               }
            } else {
               AssetLogger.Log ("download state = " + downloader.state.ToString (), "Net");
            }

            return true;
         }
      }

      public List<PackageAsset> GetCurrentUpdateAssets()
      {
          return updateAssets;
      }

      public void StartInit () {
#if !UPDATE_THREAD_INIT
         appLife.StartCoroutine (ThreadInit ());
#else
         Thread thread = new Thread (new ThreadStart (ThreadInit));
         thread.Start ();
#endif
      }

#if !UPDATE_THREAD_INIT
      IEnumerator ThreadInit ()
#else
      void ThreadInit ()
#endif
      {
#if !UPDATE_THREAD_INIT
         yield return null;
#endif
         for (int i = 0, len = localManifest.assets.Count; i < len; i++) {
            var old = localManifest.assets[i];
            var server = serverManifest.Check (old.nameCrc);

            // delete
            if (server == null) {
               var path = AssetPath.GetPathInDownLoaded (old.nameCrc);
               if (File.Exists (path)) {
                  File.Delete (path);
               }
               continue;
            }

            // modify
            if (old.fileCrc != server.fileCrc) {
               // 这里需要检查本地crc，可能已经之前下载过最新，只是manifest还没到最后，没更新.
               var path = AssetPath.GetPathInDownLoaded (old.nameCrc);
               if (File.Exists (path)) {
                  var bytes = File.ReadAllBytes (path);
                  if (Crc32.GetBytesCRC32 (bytes) == server.fileCrc) {
                     continue;
                  }
               }

               if (appManifest != localManifest) {
                  var app = appManifest.Check (old.nameCrc);
                  if (app != null && app.fileCrc == server.fileCrc) {
                     // 确保本地没下载的这个文件
                     // 用APP里面的
                     AssetLogger.LogWarning ("文件mod > 但是app跟server的文件一致，不需要下载并且删掉local更新的" + old.nameCrc, "Net");
                     if (File.Exists (path)) { File.Delete (path); }

                     continue;
                  }
               }

               AssetLogger.LogWarning (">>>>>>>但是本地文件crc和server不一样:" + server.nameCrc, "Net");

               updateAssets.AddSafe (server);
            }

         }

         // add
         for (int i = 0, len = serverManifest.assets.Count; i < len; i++) {
            var server = serverManifest.assets[i];
            var local = localManifest.Check (server.nameCrc);
            if (local == null) {
               // 检测本地是否已经下载好了
               var path = AssetPath.GetPathInDownLoaded (server.nameCrc);
               if (File.Exists (path)) {
                  var bytes = File.ReadAllBytes (path);
                  if (Crc32.GetBytesCRC32 (bytes) == server.fileCrc) {
                     continue;
                  }
               }

               if (localManifest != appManifest) {
                  var app = appManifest.Check (server.nameCrc);
                  if (app != null && app.fileCrc == server.fileCrc) {
                     // 确保本地没下载的这个文件
                     // 用APP里面的
                     AssetLogger.LogWarning ("文件add > 但是app跟server的文件一致，不需要下载，并且删掉local更新的" + server.nameCrc, "Net");
                     if (File.Exists (path)) { File.Delete (path); }

                     continue;
                  }
               }

               AssetLogger.LogWarning (">>>>>>>新增的bundle:" + server.nameCrc, "Net");

               updateAssets.AddSafe (server);
            }
         }

         // 把需要更新的bundle转化为下载信息
         foreach (var asset in updateAssets) {
            string url = cdn + asset.nameCrc + "_" + asset.fileCrc + AssetPath.ASSETBUNDLE_SUFFIX;
            string localPath = AssetPath.GetPathInDownLoaded (asset.nameCrc);
            Downloader.UrlInfo info = Downloader.UrlInfo.create (url, localPath, asset.fileLength);

            urlInfos.Add (info);
         }

         updateSize = downloader.CalculateTotalSize (urlInfos);

         AssetLogger.LogWarning ("[UPGRADE] 需要更新的文件数量=" + updateAssets.Count + ",updateSize=" + updateSize, "Net");

         isInit = true;
      }

      public bool CheckNeedUpdate () {
         return urlInfos.Count > 0;
      }

      /// <summary>
      /// 开始下载需要更新下载的文件
      /// </summary>
      public void StartUpdate () {
         Thread thread = new Thread (new ThreadStart (ThreadWorker));
         thread.Start ();
      }

      void ThreadWorker () {
         downloader.state = Downloader.State.E_DOWNLOADING;

         while (urlInfos.Count > 0) {
            // 如果已经游戏退出，或者Editor下停止了
            if (appLife.isAlive == false || forceQuit) {
               return;
            }

            Downloader.UrlInfo info = urlInfos[0];
            urlInfos.RemoveAt (0);

            // maybe is failed should reset 
            // and try ?
            long readLen = 0;
            bool success = false;

            for (int i = 0; i < 5; i++) {
               if (!downloader.DownLoadHTTPFile (info, true, ref readLen)) {
                  // 减回去
                  downloader.receivedLength -= readLen;
                  continue;
               } else {
                  // if success
                  success = true;
                  break;
               }
            }

            if (success == false) {
               downloader.state = Downloader.State.E_DOWNLOAD_ERROR;
               forceQuit = true;
               break;
            }
         }

         if (downloader.state != Downloader.State.E_DOWNLOAD_ERROR) {
            downloader.state = Downloader.State.E_DOWNLOAD_FINISHED;
         }
      }

      public bool CheckIfSuccess () {
         return downloader.state == Downloader.State.E_DOWNLOAD_FINISHED;
      }

   }

}