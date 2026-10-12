using UnityEngine;
using UnityEngine.UI;

public class PlayerAmmoHud : MonoBehaviour
{
    private TankMovement player;
    private Camera hudCamera;
    private Canvas canvas;
    private RectTransform panel;
    private TutorialPlayer health;
    private RectTransform healthPanel;
    private readonly Image[] hearts = new Image[TutorialPlayer.MaxHealth];
    private readonly Image[] icons = new Image[TankMovement.MagazineSize];
    private Image reloadBackground;
    private Image reloadFill;

    public void Initialize(TankMovement owner, Camera camera)
    {
        player = owner;
        health = owner.GetComponent<TutorialPlayer>();
        hudCamera = camera;
        transform.SetParent(camera.transform, false);
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = camera.nearClipPlane + 0.5f;
        canvas.sortingOrder = 10;
        ResizeToCamera();

        panel = new GameObject("Magazine", typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(transform, false);
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(24f, 64f);
        panel.sizeDelta = new Vector2(152f, 32f);

        for (int i = 0; i < icons.Length; i++)
        {
            icons[i] = CreateImage("Ammo " + (i + 1), new Vector2(i * 40f, 0f), new Vector2(32f, 32f));
            icons[i].sprite = owner.AmmoIcon;
            icons[i].preserveAspect = true;
        }

        reloadBackground = CreateImage("Reload Track", new Vector2(0f, 6f), new Vector2(152f, 20f));
        reloadBackground.color = new Color(0.12f, 0.16f, 0.21f, 0.95f);
        reloadFill = CreateImage("Reload Fill", new Vector2(2f, 8f), new Vector2(0f, 16f));
        reloadFill.color = new Color(1f, 0.48f, 0.08f);

        healthPanel = new GameObject("Health", typeof(RectTransform)).GetComponent<RectTransform>();
        healthPanel.SetParent(transform, false);
        healthPanel.anchorMin = healthPanel.anchorMax = healthPanel.pivot = new Vector2(0f, 1f);
        healthPanel.anchoredPosition = new Vector2(24f, -24f);
        healthPanel.sizeDelta = new Vector2(112f, 32f);
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i] = CreateImage("Heart " + (i + 1), new Vector2(i * 40f, 0f),
                new Vector2(32f, 32f), healthPanel);
            hearts[i].sprite = health != null ? health.HeartIcon : null;
            hearts[i].preserveAspect = true;
        }
        Refresh();
    }

    private Image CreateImage(string imageName, Vector2 position, Vector2 size, RectTransform parent = null)
    {
        var image = new GameObject(imageName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent != null ? parent : panel, false);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = Vector2.zero;
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        image.raycastTarget = false;
        return image;
    }

    private void LateUpdate()
    {
        if (player == null || hudCamera == null)
        {
            Destroy(gameObject);
            return;
        }
        ResizeToCamera();
        Refresh();
    }

    private void ResizeToCamera()
    {
        // Fit the HUD's reference pixels to this camera's viewport, including
        // smaller Game views, render textures, and changes in aspect ratio.
        canvas.scaleFactor = Mathf.Max(0.01f,
            Mathf.Min(hudCamera.pixelWidth / 1280f, hudCamera.pixelHeight / 720f));
    }

    private void Refresh()
    {
        bool visible = player.isActiveAndEnabled;
        panel.gameObject.SetActive(visible);
        healthPanel.gameObject.SetActive(visible && health != null);
        if (!visible) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].gameObject.SetActive(health != null && health.HealthHalves>i*2);
            if(health!=null)hearts[i].sprite=FirstLevelHealthHud.HeartSprite(health.HealthHalves,i,health.HeartIcon,
                Resources.Load<Sprite>("FirstLevelEnemies/HeartHalfAligned"),null);
        }

        for (int i = 0; i < icons.Length; i++)
            icons[i].gameObject.SetActive(!player.IsReloading && i < player.BulletsRemaining);
        reloadBackground.gameObject.SetActive(player.IsReloading);
        reloadFill.gameObject.SetActive(player.IsReloading);
        // Left-anchored width keeps the fill moving from left to right.
        reloadFill.rectTransform.sizeDelta = new Vector2(148f * player.ReloadProgress, 16f);
    }
}
