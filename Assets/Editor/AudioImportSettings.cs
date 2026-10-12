using System.IO;
using UnityEditor;
using UnityEngine;

// Настройки импорта для Resources/Audio: эффекты в памяти без задержки, музыка и мотор потоком.
public class AudioImportSettings : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        string file = Path.GetFileName(assetPath);
        bool music = file.StartsWith("music");
        bool longLoop = music || file.StartsWith("tank_");
        var settings = importer.defaultSampleSettings;
        settings.loadType = longLoop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = music ? .6f : .8f;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = !music;
        importer.loadInBackground = longLoop;
    }
}
