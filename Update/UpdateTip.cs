/*
 * @Author: chiuan wei 
 * @Date: 2017-04-10 11:17:22 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-04-10 14:32:06
 */
namespace SuperMobs.AssetManager.Update {
   using System.IO;
   using SuperMobs.AssetManager.Core;
   using SuperMobs.AssetManager;
   using SuperMobs.Core;
   using UnityEngine;

   public class UpdateTip : SuperJsonObject {
      public string BTN_ENTER_IMMEDIATE = "直接进入";
      public string BTN_RESTART = "重试";
      public string BTN_CANCLE = "取消";
      public string BTN_CONFIM = "确定";

      public string APP_ERROR = "程式版本异常，请重新安装!";

      public string SDK_INIT = "SDK初始化...";
      public string SDK_INIT_ERROR = "SDK初始化不成功,请重启尝试(*^__^*)";

      public string CHECK_RESOURCES = "检查资源更新...";
      public string DOWNLOADING = "正在下载...";
      public string DOWNLOAD_ERROR = "服务器维护中，请稍后尝试(*^__^*)";
      public string DOWNLOAD_OK = "下载完成了!";
      public string ENTER_SCENE_LOADING = "进入场景，加载中不费流量哦(*^__^*)";
      public string PASS = "您已经是最新版本了！";

      public string NEW_APP = "有新的程序版本需要更新";
      public string NEW_RESOURCES = "新的资源更新:";
      public string NOT_WIFI_WARNNING = "您目前不是WIFI网络哦(*^__^*)";
      public string SIMULATE_MODE = "没有检查到更新服务，检测到是测试模式，允许直接进入游戏";
      public string NETWORK_NOT_OK = "您的网络存在异常，请确保网络联网，再重试!";
      public string DOWNLOAD_SERVER_VERSION_ERROR = "更新出错啦，下载不到版本文件";

      public string NEW_APP_ERROR_NO_URL = "更新找不到程序下载地址，请重试，或者到相应平台下载\n谢谢，么么哒";
      public string NEW_APP_OK = "更新程序完毕，请安装完后重启。";
      public string NEW_APP_ONLY_TIP = "游戏程序需要更新，请到您下载的渠道获取！\n不见不散哦(づ￣ 3￣)づ么么哒";

      /// <summary>
      /// 当解压路径存在，则读解压路径的，可以提供给多语言选择（运行时解压某个语言的到解压路径，重新启动将是读取那个语言的文件）
      /// </summary>
      public void Load () {
         string content = "";
         var localPath = AssetPath.DownloadAssetBundlesPath + "update_tip.json";
         if (File.Exists (localPath)) {
            content = File.ReadAllText (localPath);
         } else {
            var res = Resources.Load ("update_tip") as TextAsset;
            content = res != null ? res.text : "";
         }

         if (string.IsNullOrEmpty (content) == false) {
            var test = new UpdateTip ();
            try {
               test.FromJson (content);
            } catch (System.Exception e) {
               AssetLogger.LogError ("读取更新tip时候，解析出错:" + e.Message);
               return;
            }

            this.FromJson (content);

         } else {
            AssetLogger.LogWarning ("读取不到解压路径 | Resources的update_tip.json文件，将用默认提示。");
         }
      }

      /// <summary>
      /// 把当前配置储存输出，其实也只是为了editor可能开始要一份这个文件罢了
      /// </summary>
      public void Save () {
         var json = ToJson ();
         File.WriteAllText (AssetPath.ProjectRoot + "update_tip.json", json);
         AssetLogger.Log ("save 保存输出一份更新tip的文件:" + AssetPath.ProjectRoot + "update_tip.json");
      }
   }

}