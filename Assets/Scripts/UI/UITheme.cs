using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema visual compartido por las 4 pantallas: paleta, tipografia (TMP), tarjetas
/// glassmorphism, botones y campos. Todo se construye por codigo para que las pantallas
/// queden identicas entre si sin depender de prefabs armados a mano.
/// </summary>
public static class UITheme
{
    // ---------- Paleta ----------
    public static readonly Color Gold = new Color(0.96f, 0.74f, 0.16f, 1f);      // accion primaria
    public static readonly Color GoldText = new Color(0.10f, 0.09f, 0.06f, 1f);
    public static readonly Color Blue = new Color(0.16f, 0.50f, 0.95f, 1f);      // accion informativa
    public static readonly Color Red = new Color(0.78f, 0.26f, 0.22f, 1f);       // salir / volver
    public static readonly Color Glass = new Color(0.05f, 0.09f, 0.20f, 0.74f);  // tarjetas
    public static readonly Color GlassSoft = new Color(1f, 1f, 1f, 0.12f);       // campos / botones tenues
    public static readonly Color GlassRim = new Color(1f, 1f, 1f, 0.22f);        // borde de vidrio
    public static readonly Color TextPrimary = Color.white;
    public static readonly Color TextSecondary = new Color(0.72f, 0.85f, 1f, 1f);
    public static readonly Color TextMuted = new Color(1f, 1f, 1f, 0.68f);
    public static readonly Color Overlay = new Color(0.02f, 0.04f, 0.10f, 0.45f);

    public const float ReferenceWidth = 1080f;
    public const float ReferenceHeight = 1920f;

    private static Sprite roundedSprite;
    private static TMP_FontAsset cachedFont;

    /// <summary>Sprite 9-slice de esquinas redondeadas, generado en codigo.</summary>
    public static Sprite RoundedSprite
    {
        get
        {
            if (roundedSprite == null) roundedSprite = BuildRoundedSprite(64, 22);
            return roundedSprite;
        }
    }

    private static Sprite checkSprite;

    /// <summary>Tilde blanco generado en codigo (la fuente TMP por defecto no trae "✓").</summary>
    public static Sprite CheckSprite
    {
        get
        {
            if (checkSprite == null) checkSprite = BuildCheckSprite(64);
            return checkSprite;
        }
    }

    public static TMP_FontAsset Font
    {
        get
        {
            if (cachedFont != null) return cachedFont;

            cachedFont = TMP_Settings.defaultFontAsset;
            if (cachedFont == null)
            {
                cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
            if (cachedFont == null)
            {
                Debug.LogError("TextMeshPro sin font asset. Corre AR Guide > Ejecutar TODO.");
            }
            return cachedFont;
        }
    }

    // ---------- Canvas ----------

    /// <summary>Canvas Screen Space Overlay ya configurado para movil vertical 1080x1920.</summary>
    public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>Contenedor que respeta la safe area del dispositivo (notch / barra inferior).</summary>
    public static RectTransform CreateSafeArea(Transform parent, string name = "SafeArea")
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        Stretch(rt);
        go.AddComponent<SafeAreaFitter>();
        return rt;
    }

    // ---------- Bloques ----------

    public static Image CreateFullScreenImage(Transform parent, string name, Sprite sprite, Color tint)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = tint;
        image.preserveAspect = false;
        image.raycastTarget = false;

        Stretch(go.GetComponent<RectTransform>());
        return image;
    }

    /// <summary>
    /// Tarjeta glassmorphism: borde claro translucido + relleno oscuro translucido.
    /// Devuelve el RectTransform del relleno, que es donde va el contenido.
    /// </summary>
    public static RectTransform CreateGlassCard(Transform parent, string name, Vector2 size,
        Vector2 anchoredPosition, Vector2? anchor = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);

        var rimGO = new GameObject(name, typeof(RectTransform));
        rimGO.transform.SetParent(parent, false);
        var rim = rimGO.AddComponent<Image>();
        ApplyRounded(rim, GlassRim);

        var rimRt = rimGO.GetComponent<RectTransform>();
        rimRt.anchorMin = a;
        rimRt.anchorMax = a;
        rimRt.pivot = a;
        rimRt.anchoredPosition = anchoredPosition;
        rimRt.sizeDelta = size;

        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(rimGO.transform, false);
        var fill = fillGO.AddComponent<Image>();
        ApplyRounded(fill, Glass);
        var fillRt = fillGO.GetComponent<RectTransform>();
        Stretch(fillRt, 2f);

        return fillRt;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize,
        Color color, Vector2 anchoredPosition, Vector2 size, FontStyles style = FontStyles.Normal,
        TextAlignmentOptions align = TextAlignmentOptions.Center, Vector2? anchor = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        if (Font != null) text.font = Font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = a;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        return text;
    }

    public static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition,
        Vector2 size, Color background, Color textColor, float fontSize = 40f, Vector2? anchor = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        ApplyRounded(image, background);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = a;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        var text = CreateText(go.transform, "Label", label, fontSize, textColor, Vector2.zero, size,
            FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, 12f);

        return button;
    }

    public static TMP_InputField CreateInputField(Transform parent, string name, string placeholder,
        Vector2 anchoredPosition, Vector2 size, Vector2? anchor = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var background = go.AddComponent<Image>();
        ApplyRounded(background, GlassSoft);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = a;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        var viewportGO = new GameObject("TextArea", typeof(RectTransform));
        viewportGO.transform.SetParent(go.transform, false);
        var viewportRt = viewportGO.GetComponent<RectTransform>();
        Stretch(viewportRt, 0f);
        viewportRt.offsetMin = new Vector2(30f, 12f);
        viewportRt.offsetMax = new Vector2(-30f, -12f);
        viewportGO.AddComponent<RectMask2D>();

        var placeholderText = CreateText(viewportGO.transform, "Placeholder", placeholder, 34f, TextMuted,
            Vector2.zero, size, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
        Stretch(placeholderText.rectTransform);

        var valueText = CreateText(viewportGO.transform, "Text", "", 34f, TextPrimary,
            Vector2.zero, size, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        Stretch(valueText.rectTransform);
        valueText.raycastTarget = true;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = viewportRt;
        input.textComponent = valueText;
        input.placeholder = placeholderText;
        input.targetGraphic = background;
        input.fontAsset = Font;
        input.pointSize = 34f;

        return input;
    }

    /// <summary>TMP_Dropdown armado por codigo. La opcion 0 es el placeholder.</summary>
    public static TMP_Dropdown CreateDropdown(Transform parent, string name, string placeholder, string[] options,
        Vector2 anchoredPosition, Vector2 size, Vector2? anchor = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var background = go.AddComponent<Image>();
        ApplyRounded(background, GlassSoft);
        var dropdown = go.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = background;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = a;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        var caption = CreateText(go.transform, "Label", placeholder, 34f, TextPrimary, Vector2.zero, size,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        Stretch(caption.rectTransform);
        caption.rectTransform.offsetMin = new Vector2(30f, 8f);
        caption.rectTransform.offsetMax = new Vector2(-70f, -8f);
        dropdown.captionText = caption;

        var arrow = CreateText(go.transform, "Arrow", "▼", 28f, TextMuted, new Vector2(-28f, 0f),
            new Vector2(40f, 40f), FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(1f, 0.5f));
        arrow.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        arrow.rectTransform.anchorMax = new Vector2(1f, 0.5f);

        int totalItems = options.Length + 1;
        const float itemHeight = 62f;

        var templateGO = new GameObject("Template", typeof(RectTransform));
        templateGO.transform.SetParent(go.transform, false);
        var templateRt = templateGO.GetComponent<RectTransform>();
        templateRt.anchorMin = new Vector2(0f, 0f);
        templateRt.anchorMax = new Vector2(1f, 0f);
        templateRt.pivot = new Vector2(0.5f, 1f);
        templateRt.anchoredPosition = new Vector2(0f, 4f);
        templateRt.sizeDelta = new Vector2(0f, itemHeight * Mathf.Min(totalItems, 5));
        ApplyRounded(templateGO.AddComponent<Image>(), new Color(0.06f, 0.09f, 0.20f, 0.98f));

        var scrollRect = templateGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        templateGO.SetActive(false);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform));
        viewportGO.transform.SetParent(templateGO.transform, false);
        var viewportRt = viewportGO.GetComponent<RectTransform>();
        Stretch(viewportRt);
        viewportRt.pivot = new Vector2(0f, 1f);
        viewportGO.AddComponent<Image>().color = Color.white;
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewportRt;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, itemHeight * totalItems);
        scrollRect.content = contentRt;

        var itemGO = new GameObject("Item", typeof(RectTransform));
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRt = itemGO.GetComponent<RectTransform>();
        itemRt.anchorMin = new Vector2(0f, 0.5f);
        itemRt.anchorMax = new Vector2(1f, 0.5f);
        itemRt.sizeDelta = new Vector2(0f, itemHeight);
        var itemToggle = itemGO.AddComponent<Toggle>();

        var itemBgGO = new GameObject("Item Background", typeof(RectTransform));
        itemBgGO.transform.SetParent(itemGO.transform, false);
        var itemBg = itemBgGO.AddComponent<Image>();
        itemBg.color = new Color(1f, 1f, 1f, 0.06f);
        Stretch(itemBgGO.GetComponent<RectTransform>());

        var itemCheckGO = new GameObject("Item Checkmark", typeof(RectTransform));
        itemCheckGO.transform.SetParent(itemGO.transform, false);
        var itemCheck = itemCheckGO.AddComponent<Image>();
        itemCheck.color = Blue;
        var checkRt = itemCheckGO.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0f, 0.5f);
        checkRt.anchorMax = new Vector2(0f, 0.5f);
        checkRt.pivot = new Vector2(0.5f, 0.5f);
        checkRt.sizeDelta = new Vector2(20f, 20f);
        checkRt.anchoredPosition = new Vector2(22f, 0f);

        var itemLabel = CreateText(itemGO.transform, "Item Label", "", 30f, TextPrimary, Vector2.zero,
            Vector2.zero, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        Stretch(itemLabel.rectTransform);
        itemLabel.rectTransform.offsetMin = new Vector2(48f, 2f);
        itemLabel.rectTransform.offsetMax = new Vector2(-10f, -2f);

        itemToggle.targetGraphic = itemBg;
        itemToggle.graphic = itemCheck;
        itemToggle.isOn = true;

        dropdown.itemText = itemLabel;
        dropdown.template = templateRt;

        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData(placeholder));
        foreach (var option in options)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));
        }
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        return dropdown;
    }

    // ---------- Helpers ----------

    /// <summary>
    /// Fondo redondeado. cornerScale agranda las esquinas (2 = el doble de radio) sin generar
    /// otro sprite: el 9-slice se dibuja con menos pixeles por unidad.
    /// </summary>
    public static void ApplyRounded(Image image, Color color, float cornerScale = 1f)
    {
        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.01f, cornerScale);
        image.color = color;
    }

    /// <summary>
    /// Sombra suave detras del texto (underlay de TMP) para que se lea sobre fotos claras sin
    /// tener que poner un panel oscuro detras.
    /// </summary>
    public static void AddTextShadow(TextMeshProUGUI text)
    {
        if (text.fontSharedMaterial == null) return;
        var material = new Material(text.fontSharedMaterial);
        material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.75f));
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.4f);
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.4f);
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
        material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.5f);
        text.fontSharedMaterial = material;
    }

    /// <summary>
    /// Estira un RectTransform a lo ancho del padre dejando un margen a cada lado, sin tocar su
    /// alto ni su posicion vertical. En pantallas mas altas que 16:9 el canvas queda mas angosto
    /// que 1080, y los anchos fijos se salian por los costados.
    /// </summary>
    public static void StretchHorizontally(RectTransform rt, float leftMargin, float rightMargin)
    {
        rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
        rt.pivot = new Vector2(0f, rt.pivot.y);
        rt.anchoredPosition = new Vector2(leftMargin, rt.anchoredPosition.y);
        rt.sizeDelta = new Vector2(-(leftMargin + rightMargin), rt.sizeDelta.y);
    }

    public static void Stretch(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    private static Sprite BuildCheckSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        // Dos trazos: bajada corta a la izquierda y subida larga a la derecha.
        var a = new Vector2(size * 0.20f, size * 0.52f);
        var b = new Vector2(size * 0.42f, size * 0.30f);
        var c = new Vector2(size * 0.80f, size * 0.72f);
        float halfThickness = size * 0.075f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float dist = Mathf.Min(DistanceToSegment(p, a, b), DistanceToSegment(p, b, c));
                float alpha = Mathf.Clamp01(halfThickness - dist + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static Sprite BuildRoundedSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 1f;
                bool left = x < radius, right = x > size - radius;
                bool bottom = y < radius, top = y > size - radius;

                if ((left || right) && (bottom || top))
                {
                    float cx = left ? radius : size - radius;
                    float cy = bottom ? radius : size - radius;
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
}
