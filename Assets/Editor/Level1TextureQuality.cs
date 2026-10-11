using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Level1TextureQuality
{
    [Serializable] public class Entry { public string name; public float ppu; public int width,height; }
    [Serializable] class Manifest { public Entry[] entries; }
    public static Entry[] Entries => JsonUtility.FromJson<Manifest>("{\"entries\":"+File.ReadAllText("Assets/UI/Level1/TextureQuality.json")+"}").entries;
    public static void Configure(TextureImporter importer,string path)
    {
        string name=Path.GetFileNameWithoutExtension(path);
        importer.spritePixelsPerUnit=100;
        foreach(var entry in Entries) if(entry.name==name){importer.spritePixelsPerUnit=entry.ppu;break;}
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        bool tank = name == "56_3036";
        importer.filterMode=tank ? FilterMode.Trilinear : FilterMode.Bilinear;
        importer.mipmapEnabled=tank;
        importer.alphaIsTransparency=true;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=8192;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.crunchedCompression=false;
        var platform=importer.GetPlatformTextureSettings("Standalone");
        platform.overridden=false;platform.maxTextureSize=8192;
        platform.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(platform);
    }
}
