using System;
using System.IO;
using SuperMobs.AssetManager.Assets;
using SuperMobs.AssetManager.Core;
using System.Collections.Generic;
namespace SuperMobs.AssetManager.Package
{
	public class PackageManifest : IStreamAsset
	{
		public List<PackageAsset> assets;

		Dictionary<uint, PackageAsset> _nameDict = new Dictionary<uint, PackageAsset>();

		public PackageAsset Check(uint name)
		{
			PackageAsset a;
			if (_nameDict.TryGetValue(name, out a))
			{
				return a;
			}
			else
			{
				return null;
			}
		}

		public void FromStream(BinaryReader br)
		{
			var length = br.ReadInt32();
			assets = new List<PackageAsset>();
			for (int i = 0; i < length; i++)
			{
				assets.Add(new PackageAsset());
				assets[i].FromStream(br);

				// record for fast seaching..
				_nameDict[assets[i].nameCrc] = assets[i];
			}
		}

		public void ToStream(BinaryWriter bw)
		{
			bw.Write(assets != null ? assets.Count : 0);
			foreach (var item in assets)
			{
				item.ToStream(bw);
			}
		}
	}
}
