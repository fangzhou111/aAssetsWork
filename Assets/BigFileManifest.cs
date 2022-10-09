namespace SuperMobs.AssetManager.Assets
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using SuperMobs.AssetManager.Core;

	public class BigFileManifest : IStreamAsset
	{
		public BigFileInfo[] fileInfos;

		[NonSerialized]
		Dictionary<uint, int> cachedFileIDToIndex = new Dictionary<uint, int>();

		public void ToStream(BinaryWriter bw)
		{
			bw.Write(fileInfos != null ? fileInfos.Length : 0);
			foreach (BigFileInfo asset in fileInfos)
				asset.ToStream(bw);
		}

		public void FromStream(BinaryReader br)
		{
			int assCount = br.ReadInt32();
			fileInfos = new BigFileInfo[assCount];
			for (int i = 0; i < assCount; i++)
			{
				fileInfos[i] = new BigFileInfo();
				fileInfos[i].FromStream(br);
				cachedFileIDToIndex[fileInfos[i].id] = i;
			}
		}

		public BigFileInfo GetFileInfo(uint fileID)
		{
			int index = -1;
			if (cachedFileIDToIndex.TryGetValue(fileID, out index))
			{
				return fileInfos[index];
			}
			else
			{
				return null;
			}
		}
	}
}
