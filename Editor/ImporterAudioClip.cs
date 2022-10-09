using System;
using UnityEditor;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
	public class ImporterAudioClip : AssetPostprocessor
	{
		void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{

		}

        public void OnPostprocessAudio(AudioClip clip)
        {
            AudioImporter ac = assetImporter as AudioImporter;
            ac.forceToMono = true;
            ac.preloadAudioData = true;

            AudioImporterSampleSettings setting = ac.GetOverrideSampleSettings("Standalone");
            setting.loadType = AudioClipLoadType.DecompressOnLoad;
            setting.compressionFormat = AudioCompressionFormat.Vorbis;
            setting.quality = 0;
            ac.SetOverrideSampleSettings("Standalone", setting);

            setting = ac.GetOverrideSampleSettings("iOS");
            setting.loadType = AudioClipLoadType.DecompressOnLoad;
            setting.compressionFormat = AudioCompressionFormat.MP3;
            setting.quality = 0;
            ac.SetOverrideSampleSettings("iOS", setting);

            setting = ac.GetOverrideSampleSettings("Android");
            setting.loadType = AudioClipLoadType.DecompressOnLoad;
            setting.compressionFormat = AudioCompressionFormat.Vorbis;
            setting.quality = 0;
            ac.SetOverrideSampleSettings("Android", setting);
        }
	}
}
