using UnityEngine;

/// <summary>
/// Ajusta el RectTransform a la safe area del dispositivo (notch, barra de gestos).
/// Se agrega por codigo desde UITheme.CreateSafeArea.
/// </summary>
public class SafeAreaFitter : MonoBehaviour
{
    private Rect lastSafeArea;

    private void OnEnable() => Apply();

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea) Apply();
    }

    private void Apply()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        lastSafeArea = Screen.safeArea;

        Vector2 min = lastSafeArea.position;
        Vector2 max = lastSafeArea.position + lastSafeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
