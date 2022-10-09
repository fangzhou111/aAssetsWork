using UnityEngine;
using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
using System.Net;
using System.IO;
//using System.Threading;

///<summary>
/// DOWNLOAD API
/// @DownLoadFTPFile
/// @DownLoadHTTPFile
/// 
/// ChiuanWei 2014-5
///</summary>

using SuperMobs.AssetManager.Core;
using System.Net.Security;

namespace SuperMobs.AssetManager.Update
{
	public class Downloader
	{
        static bool isInit = false;

		public Downloader()
		{
            if (isInit == false)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => { return true; });
                isInit = true;
            }
		}

		/// <summary>
		/// each download url should contains infomation
		/// </summary>
		public class UrlInfo
		{
			public long fileSize;
			public string url;
			public string localPath;

			private UrlInfo() { }
			public static UrlInfo create(
				string downloadUrl,
				string localFullPath,
				long fileLength)
			{
				UrlInfo i = new UrlInfo();
				i.fileSize = fileLength; //0 mean unknown
				i.url = downloadUrl;
				i.localPath = localFullPath;
				return i;
			}

			public override string ToString()
			{
				return
					"url=[" + url + "]" +
					"length=[" + fileSize + "]" +
					"localPath=[" + localPath + "]";
			}
		}

		public enum State : int
		{
			E_IDLE = 0,
			E_DOWNLOADING,
			E_DOWNLOAD_FINISHED,
			E_DOWNLOAD_ERROR,
		}

		public long currentTargetFileLength = 0;
		public long totalLength;
		public float totalMB;
		public long receivedLength;
		public bool mStaForceToQuit = false;
		public State state = State.E_IDLE;

		const int MAX_READ_COUNT = 2048;

		///====================================================
		/// 从ftp服务器下载资源
		///====================================================

		/// <summary>   
		/// 下载文件,负责下载单个文件到指定目录
		/// "ftp://" + FtpAddress + "/" + filename
		/// </summary>
		//     public bool DownLoadFTPFile(
		//         UrlInfo urlInfo,
		//         string ftpUser,
		//         string ftpPw,
		//         bool isNeedReplace)
		//     {
		//         FileStream fs = null;
		//FtpWebRequest req = (FtpWebRequest)FtpWebRequest.Create(urlInfo.url);
		//         req.Method = WebRequestMethods.Ftp.DownloadFile;
		//         req.UseBinary = true;
		//         req.UsePassive = true;
		//         req.KeepAlive = false;
		//         req.Credentials = new NetworkCredential(ftpUser, ftpPw);
		//         try
		//         {
		//             long fileLength = 0;
		//             long recvedLen = 0;

		//	FileInfo fi = new FileInfo(urlInfo.localPath);
		//	if (!fi.Directory.Exists)
		//             {
		//		fi.Directory.Create();
		//             }
		//	if (fi.Exists)
		//             {
		//		fs = File.OpenWrite(urlInfo.localPath);
		//                 if (!isNeedReplace)
		//                 {
		//                     fileLength = fs.Length;
		//			receivedLength += fileLength;
		//                     fs.Seek(fileLength, SeekOrigin.Current);
		//                 }
		//                 else
		//                 {
		//                     fs.Seek(0, SeekOrigin.Begin);
		//                 }
		//             }
		//             else
		//             {
		//		fs = new FileStream(urlInfo.localPath, FileMode.Create);
		//             }

		//             if (fs == null)
		//             {
		//                 state = State.E_DOWNLOAD_ERROR;
		//                 return false;
		//             }

		//             if (fileLength > 0 && !isNeedReplace)
		//             {
		//                 //req.AddRange((int)fileLength);
		//                 req.ContentOffset = fileLength;
		//                 recvedLen = fileLength;
		//             }

		//             byte[] b = new byte[MAX_READ_COUNT + 1];
		//             int i = 0;
		//             Stream stream = req.GetResponse().GetResponseStream();
		//             //set timeout 10s
		//             stream.ReadTimeout = 10000;

		//             while ((i = stream.Read(b, 0, MAX_READ_COUNT)) > 0)
		//             {
		//                 recvedLen += i;
		//		receivedLength += i;
		//                 fs.Write(b, 0, i);
		//                 if (mStaForceToQuit) break;
		//             }

		//             fs.Close();
		//             stream.Close();

		//             //如果预先不知道文件大小呢？//
		//	TTDebuger.Log("recvedLen = " + recvedLen + " fileSize = " + urlInfo.fileSize,"NET");

		//	if (!mStaForceToQuit && (recvedLen == urlInfo.fileSize || urlInfo.fileSize == 0))
		//             {
		//                 return true;
		//             }
		//             else
		//             {
		//                 //maybe delete the download file before?
		//		if (fi.Exists)
		//                 {
		//			fi.Delete();
		//                 }
		//                 return false;
		//             }
		//         }
		//         catch (Exception ex)
		//         {
		//             TTDebuger.LogError("FTP Download Error:" + ex.Message,"NET");
		//             if (fs != null) fs.Close();
		//             state = State.E_DOWNLOAD_ERROR;
		//             return false;
		//         }
		//     }


		///====================================================
		/// 通过HTTP下载web资源
		///====================================================
		public bool DownLoadHTTPFile(
			UrlInfo urlInfo,
			bool isNeedReplace,
			ref long tempReadLength)
		{
			FileStream fs = null;
			try
			{
				long targetFileTotalLength = urlInfo.fileSize;
				currentTargetFileLength = targetFileTotalLength;
				long fileLength = 0;
				long recvedLen = 0;
				state = State.E_DOWNLOADING;

				FileInfo fi = new FileInfo(urlInfo.localPath);

				if (!fi.Directory.Exists)
				{
					fi.Directory.Create();
				}

				if (fi.Exists)
				{
					if (!isNeedReplace)
					{
						fs = File.OpenWrite(urlInfo.localPath);

						//FIX:416,error range.if full download before need return
						//Make sure the fileSize is right!
						//or download again.
						if (fs.Length > 0 && fs.Length == targetFileTotalLength)
						{
							receivedLength += fileLength;
							if (fs != null) fs.Close();
							return true;
						}

						fileLength = fs.Length;
						if (fileLength > 0)
						{
							fileLength -= 1;
						}

						receivedLength += fileLength;
					}
					else
					{
						// if new file should download again
						fi.Delete();
						fs = new FileStream(urlInfo.localPath, FileMode.Create);
					}
				}
				else
				{
					fs = new FileStream(urlInfo.localPath, FileMode.Create);
				}

				if (fs == null)
				{
					state = State.E_DOWNLOAD_ERROR;
					return false;
				}

				recvedLen = fileLength;

				System.IO.Stream ns = null;

				if (urlInfo.url.StartsWith("ftp"))
				{
					FtpWebRequest mWebReq = (FtpWebRequest)HttpWebRequest.Create(new Uri(urlInfo.url));
					if (fileLength > 0)
					{
						mWebReq.ContentOffset = fileLength;
					}

					ns = mWebReq.GetResponse().GetResponseStream();
				}
				else if (urlInfo.url.StartsWith("file"))
				{
					FileWebRequest mWebReq = (FileWebRequest)HttpWebRequest.Create(new Uri(urlInfo.url));
					fileLength = 0;
					receivedLength = 0;
					ns = mWebReq.GetResponse().GetResponseStream();
				}
				else
				{
					HttpWebRequest mWebReq = (HttpWebRequest)HttpWebRequest.Create(new Uri(urlInfo.url));

					// need continue download.
					//if (fileLength > 0)
					{
						mWebReq.AddRange((int)fileLength);
					}

					// bytes 0-800/801
					string contentRange = mWebReq.GetResponse().Headers[HttpRequestHeader.ContentRange];

					if (string.IsNullOrEmpty(contentRange))
					{
						mWebReq.AddRange(0);
						fs.Seek(0, SeekOrigin.Begin);
						receivedLength = 0;
					}
					else
					{
						//TTDebuger.Log("File Continue Download contentRange=" + contentRange, "NET");

						// set range base on contentRange from response.
						contentRange = contentRange.Replace(" ", "")
										.Replace("bytes", "");
						string[] contentInfos = contentRange.Split('/');
						if (contentInfos.Length == 2)
						{
							targetFileTotalLength = Convert.ToInt64(contentInfos[1]);
							currentTargetFileLength = targetFileTotalLength;
							long startIndex = Convert.ToInt64(contentInfos[0].Split('-')[0]);
							fs.Seek(startIndex, SeekOrigin.Begin);
						}
						else // wont happens..
						{
							receivedLength = 0;
							mWebReq.AddRange(0);
							fs.Seek(0, SeekOrigin.Begin);
						}
					}

					ns = mWebReq.GetResponse().GetResponseStream();

					//set this stream timeout.
					ns.ReadTimeout = 10000;
				}

				//TTDebuger.Log("FILE DOWNLOAD = " + urlInfo.ToString(), "Net");

				byte[] nbytes = new byte[MAX_READ_COUNT + 1];
				int nReadSize = 0;
				nReadSize = ns.Read(nbytes, 0, MAX_READ_COUNT);
				while (nReadSize > 0)
				{
					recvedLen += nReadSize;
					receivedLength += nReadSize;
					tempReadLength += nReadSize;

					fs.Write(nbytes, 0, nReadSize);
					fs.Flush();
					nReadSize = ns.Read(nbytes, 0, MAX_READ_COUNT);

					if (mStaForceToQuit == true)
					{
						break;
					}
				}

				fs.Close();
				ns.Close();

				AssetLogger.Log(Color.cyan, "finished：forcequit=" + mStaForceToQuit + ", recvedLen=" + recvedLen + ", target size=" + targetFileTotalLength + "\n" + urlInfo.ToString(), "Net");

				//if file size ==0 mean unknow this file size at first.
				if (!mStaForceToQuit && (recvedLen == targetFileTotalLength || targetFileTotalLength == 0))
				{
					return true;
				}
				else
				{
					//maybe delete the download file before?
					if (fi.Exists)
					{
						fi.Delete();
					}
					return false;
				}
			}
			catch (Exception e)
			{
				AssetLogger.LogError("Download exception:" + e.Message, "NET");
				if (fs != null) fs.Close();
				state = State.E_DOWNLOAD_ERROR;
				return false;
			}

		}

		public long GetDownloadFileSize(string fileUrl)
		{
			HttpWebRequest mWebReq = (HttpWebRequest)HttpWebRequest.Create(new Uri(fileUrl));

			// bytes 0-800/801
			string contentRange = mWebReq.GetResponse().Headers[HttpRequestHeader.ContentRange];

			if (string.IsNullOrEmpty(contentRange))
			{
				return 0;
			}
			else
			{
				//TTDebuger.Log("File Continue Download contentRange=" + contentRange, "NET");

				// set range base on contentRange from response.
				contentRange = contentRange.Replace(" ", "")
								.Replace("bytes", "");
				string[] contentInfos = contentRange.Split('/');
				if (contentInfos.Length == 2)
				{
					return Convert.ToInt64(contentInfos[1]);
				}
				else // wont happens..
				{
					return 0;
				}
			}
		}

		///<summary>
		/// 计算所有url大小
		///</summary>
		public long CalculateTotalSize(List<UrlInfo> tList)
		{
			long res = 0;
			for (int i = 0; tList != null && i < tList.Count; i++)
			{
				res += tList[i].fileSize;
			}

			this.totalLength = res;
			float seed = 1024 * 1024;
			this.totalMB = float.Parse((totalLength / seed).ToString("F2"));

			return res;
		}

	}
}