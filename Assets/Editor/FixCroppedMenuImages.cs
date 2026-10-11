using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FixCroppedMenuImages
{
    [System.Serializable] private class Layer { public string id; public float x, y, w, h; }
    [System.Serializable] private class Layout { public Layer[] layers; }
    [MenuItem("Bounce Club/Fix Cropped Menu Image Sizes")]
    public static void Fix()
    {
        if (EditorApplication.isPlaying) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != BuildFigmaMainMenu.ScenePath) return;
        string json = System.IO.File.ReadAllText("Assets/UI/MainMenu/ModalLayout.json").TrimStart('\uFEFF');
        var layout = JsonUtility.FromJson<Layout>("{\"layers\":" + json + "}");
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null) continue;
            foreach (var layer in layout.layers)
            {
                if (!image.name.EndsWith(" " + layer.id) || !layer.id.StartsWith("48:269")) continue;
                var sprite = image.sprite;
                var crop = sprite.rect;
                // Figma PNGs were exported at 1x. The current texture may itself have been cropped.
                float sx = layer.w / Mathf.Ceil(layer.w);
                float sy = layer.h / Mathf.Ceil(layer.h);
                float width = crop.width * sx;
                float height = crop.height * sy;
                float x = layer.x + (layer.w - sprite.texture.width * sx) / 2 + crop.x * sx;
                float y = layer.y + (layer.h - sprite.texture.height * sy) / 2 + (sprite.texture.height - crop.yMax) * sy;
                var rect = image.rectTransform;
                Undo.RecordObject(rect, "Fix cropped button size");
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = new Vector2(x + width / 2 - 960, 540 - y - height / 2);
                rect.localScale = Vector3.one;
                image.preserveAspect = true;
                count++;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Cropped menu image sizes fixed: " + count);
    }
}

