using System.IO;
using UnityEditor;
using UnityEngine;

// Настройки импорта для Resources/Audio: эффекты в памяти без задержки, музыка потоком.
public class AudioImportSettings : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        bool music = Path.GetFileName(assetPath).StartsWith("music");
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = music ? .6f : .8f;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = !music;
        importer.loadInBackground = music;
    }
}
