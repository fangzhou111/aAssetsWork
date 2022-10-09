using SuperMobs.AssetManager.Core;
namespace SuperMobs.AssetManager.Update
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using System.IO;
	using System.Threading;

	internal class YieldDownloadApp : CustomYieldInstruction
	{
		public override bool keepWaiting
		{
			get
			{
				if (downloader.state == Downloader.State.E_DOWNLOADING)
				{
					float downMB = float.Parse((downloader.receivedLength / seed).ToString("F2"));

					// maybe get total size when downloading
					if (totalMB == 0f && downloader.currentTargetFileLength > 0)
					{
						totalMB = float.Parse((downloader.currentTargetFileLength / seed).ToString("F2"));
					}

					if(actUpdate != null) actUpdate(totalMB > 0 ? Mathf.Clamp01(downMB / totalMB) : 0f, "(" + downMB + "/" + totalMB + "mb)");
					return true;
				}
				else
				{
					if (downloader.state == Downloader.State.E_DOWNLOAD_FINISHED)
					{
					}
					else
					{
						error = downloader.state.ToString() + "\nthis.serverManifestUrl = " + this.serverDownloadFileURL;
					}
				}

				return false;
			}
		}


		private YieldDownloadApp() { }

		//bool isStart = false;
		public string error = string.Empty;
		// 下载器
		Downloader downloader = new Downloader();
		Downloader.UrlInfo url;
		//long downloadLength = 0;
		long fileSize = 0;
		public float totalMB = 0f;
		float seed = 1024 * 1024;

		string serverDownloadFileURL = string.Empty;
		public string localPath = string.Empty;

		public Action<float, string> actUpdate = null;

		public YieldDownloadApp(string assetUrl, string appFileName, long size)
		{
			serverDownloadFileURL = assetUrl;

			// apk download local path
			localPath = AssetPath.SDExternalPath + appFileName;

			fileSize = size;
			totalMB = float.Parse((fileSize / seed).ToString("F2"));

			url = Downloader.UrlInfo.create(serverDownloadFileURL, localPath, size);
		}

		public void StartDownload()
		{
			Thread thread = new Thread(new ThreadStart(ThreadWorker));
			thread.Start();
		}

		void ThreadWorker()
		{
			downloader.state = Downloader.State.E_DOWNLOADING;

			long readLen = 0;
			bool success = false;

			for (int i = 0; i < 3; i++)
			{
				// 需要考虑断点续传
				if (!downloader.DownLoadHTTPFile(url, false, ref readLen))
				{
					// 减回去
					downloader.receivedLength -= readLen;
					continue;
				}
				else
				{
					// if success
					success = true;
					break;
				}
			}

			if (success == false)
			{
				downloader.state = Downloader.State.E_DOWNLOAD_ERROR;
				error = "App下载出错超过了3次";
			}

			if (downloader.state != Downloader.State.E_DOWNLOAD_ERROR)
			{
				downloader.state = Downloader.State.E_DOWNLOAD_FINISHED;
			}
		}
	}
}
