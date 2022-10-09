using System;
using System.IO;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;

namespace SuperMobs.AssetManager.Assets
{
	public class BundleCrcCollector : StreamAsset
	{
		public uint[] names;
		public uint[] crcs;

		Dictionary<uint, int> _nameDict = new Dictionary<uint, int>();

		public BundleCrcCollector()
		{
			names = new uint[0];
			crcs = new uint[0];
		}

		public int Check(uint crc)
		{
			int index = 0;
			if (_nameDict.TryGetValue(crc, out index))
			{
				return index;
			}
			else
			{
				for (int i = 0; i < names.Length; i++)
				{
					if (names[i] == crc)
					{
						_nameDict[crc] = i;
						return i;
					}
				}
				return -1;
			}
		}

		public override void ToStream(BinaryWriter bw)
		{
			bw.WriteArray(names);
			bw.WriteArray(crcs);
		}

		public override void FromStream(BinaryReader br)
		{
			names = br.ReadArrayUint();
			crcs = br.ReadArrayUint();
		}

	}
}
