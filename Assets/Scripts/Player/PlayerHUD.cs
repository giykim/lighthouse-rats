using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : NetworkBehaviour
{
    private GameObject _hudCanvas;

    public override void OnStartLocalPlayer()
    {
        CreateHUD();
    }

    private void CreateHUD()
    {
        _hudCanvas = new GameObject("PlayerHUD");
        DontDestroyOnLoad(_hudCanvas);

        Canvas canvas = _hudCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        _hudCanvas.AddComponent<CanvasScaler>();

        AddCrosshairBar("CrosshairH", new Vector2(16f, 2f));
        AddCrosshairBar("CrosshairV", new Vector2(2f, 16f));
    }

    private void AddCrosshairBar(string name, Vector2 size)
    {
        GameObject bar = new GameObject(name);
        bar.transform.SetParent(_hudCanvas.transform, false);

        Image img = bar.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);

        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (_hudCanvas != null)
            Destroy(_hudCanvas);
    }
}
