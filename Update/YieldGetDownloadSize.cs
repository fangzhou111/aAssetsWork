using SuperMobs.AssetManager.Core;
namespace SuperMobs.AssetManager.Update
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using System.IO;
	using System.Threading;

	internal class YieldGetDownloadSize : CustomYieldInstruction
	{
		public override bool keepWaiting
		{
			get
			{
				return isEnd == false;
			}
		}

		public long size = 0;
		private string assetUrl = string.Empty;
		private bool isEnd = false;

		private YieldGetDownloadSize() { }
		public YieldGetDownloadSize(string assetUrl)
		{
			this.assetUrl = assetUrl;
			StartDownload();
		}

		public void StartDownload()
		{
			Thread thread = new Thread(new ThreadStart(ThreadWorker));
			thread.Start();
		}

		void ThreadWorker()
		{
			isEnd = false;
			Downloader downloader = new Downloader();
			try
			{
				this.size = downloader.GetDownloadFileSize(this.assetUrl);
			}
			catch (Exception e)
			{
				AssetLogger.LogException("获取download file size exception: " + e.Message, "Net");
				this.size = 0;
			}
			isEnd = true;
		}
	}
}
