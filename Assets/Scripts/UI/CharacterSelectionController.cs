using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// PANTALLA 3 - Galeria de personajes. Fila de logos arriba, preview 3D real del personaje
/// seleccionado en el centro, panel de skins a la derecha y ficha con nombre, descripcion y
/// atributos abajo.
///
/// El preview 3D se renderiza en un "escenario" lejos del origen, con camara y luz propias,
/// hacia una RenderTexture: asi no interfiere con la camara AR de Vuforia.
/// </summary>
public class CharacterSelectionController : ScreenController
{
    private static readonly Vector3 StagePosition = new Vector3(0f, -1000f, 0f);
    private const float RotationSpeed = 30f;
    private const float LogoRowHeight = 200f;
    private const float CardPadding = 36f;
    private const float ChipWidth = 300f;
    private const float SkinPanelWidth = 270f;
    private const float SkinHeaderHeight = 80f;
    private const float SkinItemHeight = 100f;
    private const float SkinItemSpacing = 10f;
    private const float ThumbYaw = 20f; // leve giro para que la miniatura tenga volumen

    private readonly List<Image> logoFrames = new();

    private RawImage preview;
    private TextMeshProUGUI nameLabel;
    private TextMeshProUGUI taglineLabel;
    private TextMeshProUGUI descriptionLabel;
    private readonly List<TextMeshProUGUI> tagLabels = new();
    private readonly List<Image> tagChips = new();

    // Panel lateral de skins
    private RectTransform skinPanel;
    private RectTransform skinList;
    private readonly List<(Image outline, GameObject check)> skinItems = new();
    // Una miniatura renderizada por skin (se reusan entre personajes).
    private readonly List<RenderTexture> skinThumbs = new();
    private int skinCount;
    private int selectedSkin;

    private Transform stage;
    private Camera stageCamera;
    private RenderTexture stageTexture;
    private GameObject currentModel;

    private Coroutine frameRoutine;
    private int index;

    protected override void BuildUI()
    {
        UITheme.CreateFullScreenImage(Root, "Background", Resources.Load<Sprite>("UI/fondo_galeria"), Color.white);
        UITheme.CreateFullScreenImage(Root, "Overlay", null, UITheme.Overlay);

        var safe = UITheme.CreateSafeArea(Root);
        var top = new Vector2(0.5f, 1f);
        var topLeft = new Vector2(0f, 1f);

        var title = UITheme.CreateText(safe, "Title", "Galería de personajes", 70f, UITheme.TextPrimary,
            new Vector2(50f, -50f), new Vector2(950f, 90f), FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, topLeft);
        UITheme.StretchHorizontally(title.rectTransform, 50f, 50f);

        BuildLogoRow(safe);

        // Preview 3D
        var previewGO = new GameObject("Preview", typeof(RectTransform));
        previewGO.transform.SetParent(safe, false);
        preview = previewGO.AddComponent<RawImage>();
        preview.raycastTarget = false;
        var previewRt = previewGO.GetComponent<RectTransform>();
        previewRt.anchorMin = top;
        previewRt.anchorMax = top;
        previewRt.pivot = top;
        // Debajo de la fila de logos (termina en 340) y algo corrido a la izquierda para que el
        // panel de skins no tape al personaje.
        previewRt.anchoredPosition = new Vector2(-70f, -370f);
        previewRt.sizeDelta = new Vector2(840f, 950f);

        BuildSkinPanel(safe);
        BuildInfoCard(safe);

        var useButton = UITheme.CreateButton(safe, "UseButton", "Usar este personaje",
            new Vector2(0f, 150f), new Vector2(900f, 130f), UITheme.Gold, UITheme.GoldText, 46f,
            new Vector2(0.5f, 0f));
        useButton.onClick.AddListener(OnUseCharacter);

        var backButton = UITheme.CreateButton(safe, "BackButton", "← Volver",
            new Vector2(0f, 40f), new Vector2(900f, 95f), UITheme.Red, UITheme.TextPrimary, 38f,
            new Vector2(0.5f, 0f));
        backButton.onClick.AddListener(() => Go(AppScreen.EventSelection));
    }

    private void BuildLogoRow(RectTransform safe)
    {
        var rowGO = new GameObject("LogoRow", typeof(RectTransform));
        rowGO.transform.SetParent(safe, false);
        var row = rowGO.GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, -140f);
        row.sizeDelta = new Vector2(0f, LogoRowHeight);

        // El layout reparte el ancho REAL de la pantalla entre los logos. Con anchos fijos
        // (1070 px) los de los costados quedaban recortados en celulares mas altos que 16:9.
        var layout = rowGO.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 0);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < AppCatalog.Characters.Length; i++)
        {
            var data = AppCatalog.Characters[i];
            int captured = i;

            var frameGO = new GameObject("Logo_" + data.id, typeof(RectTransform));
            frameGO.transform.SetParent(rowGO.transform, false);
            var frame = frameGO.AddComponent<Image>();
            UITheme.ApplyRounded(frame, UITheme.GlassSoft);

            var button = frameGO.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => SetIndex(captured));

            var logoGO = new GameObject("Icon", typeof(RectTransform));
            logoGO.transform.SetParent(frameGO.transform, false);
            var logo = logoGO.AddComponent<Image>();
            logo.sprite = data.Logo;
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            var logoRt = logoGO.GetComponent<RectTransform>();
            UITheme.Stretch(logoRt);
            logoRt.offsetMin = new Vector2(14f, 54f); // deja lugar al nombre abajo
            logoRt.offsetMax = new Vector2(-14f, -12f);

            var nameText = UITheme.CreateText(frameGO.transform, "Name", data.displayName, 26f,
                UITheme.TextPrimary, Vector2.zero, Vector2.zero, FontStyles.Bold, TextAlignmentOptions.Center);
            var nameRt = nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(0f, 10f);
            nameRt.sizeDelta = new Vector2(-8f, 40f);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 18f;
            nameText.fontSizeMax = 26f;

            logoFrames.Add(frame);
        }
    }

    private void BuildInfoCard(RectTransform safe)
    {
        var card = UITheme.CreateGlassCard(safe, "CharacterInfoCard", new Vector2(1000f, 310f),
            new Vector2(0f, 300f), new Vector2(0.5f, 0f));
        // La tarjeta va al ancho real de la pantalla (el relleno es hijo del borde).
        UITheme.StretchHorizontally((RectTransform)card.parent, 36f, 36f);

        var topLeft = new Vector2(0f, 1f);
        // Columna de texto: todo el ancho menos la columna de caracteristicas de la derecha.
        const float textRightMargin = CardPadding + ChipWidth + 24f;

        nameLabel = UITheme.CreateText(card, "Name", "", 58f, UITheme.TextPrimary,
            new Vector2(0f, -20f), new Vector2(0f, 75f), FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, topLeft);
        UITheme.StretchHorizontally(nameLabel.rectTransform, CardPadding, textRightMargin);

        taglineLabel = UITheme.CreateText(card, "Tagline", "", 32f, UITheme.TextSecondary,
            new Vector2(0f, -100f), new Vector2(0f, 46f), FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, topLeft);
        UITheme.StretchHorizontally(taglineLabel.rectTransform, CardPadding, textRightMargin);

        descriptionLabel = UITheme.CreateText(card, "Description", "", 27f, UITheme.TextMuted,
            new Vector2(0f, -156f), new Vector2(0f, 130f), FontStyles.Normal,
            TextAlignmentOptions.TopLeft, topLeft);
        UITheme.StretchHorizontally(descriptionLabel.rectTransform, CardPadding, textRightMargin);

        // Columna derecha: tres caracteristicas con el color del personaje.
        for (int i = 0; i < 3; i++)
        {
            var chipRt = CreateChipRect(card, "Tag_" + i, -28f - i * 84f);
            var chip = chipRt.gameObject.AddComponent<Image>();
            UITheme.ApplyRounded(chip, UITheme.GlassSoft);
            chip.raycastTarget = false;

            var label = UITheme.CreateText(chipRt, "Text", "", 28f, UITheme.TextPrimary,
                Vector2.zero, Vector2.zero, FontStyles.Bold);
            UITheme.Stretch(label.rectTransform, 10f);
            tagChips.Add(chip);
            tagLabels.Add(label);
        }
    }

    /// <summary>
    /// Panel vertical "Skins" a la derecha del preview. Las tarjetas se arman en SetIndex
    /// porque cada personaje puede tener una cantidad distinta de skins.
    /// </summary>
    private void BuildSkinPanel(RectTransform safe)
    {
        var fill = UITheme.CreateGlassCard(safe, "SkinPanel", new Vector2(SkinPanelWidth, 300f),
            new Vector2(-24f, -360f), new Vector2(1f, 1f));
        skinPanel = (RectTransform)fill.parent;

        UITheme.CreateText(fill, "Header", "Skins", 34f, UITheme.TextPrimary,
            new Vector2(24f, -12f), new Vector2(SkinPanelWidth - 40f, SkinHeaderHeight - 20f),
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f));

        var listGO = new GameObject("List", typeof(RectTransform));
        listGO.transform.SetParent(fill, false);
        skinList = listGO.GetComponent<RectTransform>();
        UITheme.Stretch(skinList);
        skinList.offsetMin = new Vector2(12f, 12f);
        skinList.offsetMax = new Vector2(-12f, -SkinHeaderHeight);

        var layout = listGO.AddComponent<VerticalLayoutGroup>();
        layout.spacing = SkinItemSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private void BuildSkinItems(CharacterData data)
    {
        // Destroy recien actua al final del frame: se apagan antes para que el layout no las cuente.
        foreach (Transform child in skinList)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        skinItems.Clear();

        var skins = data.skins != null && data.skins.Length > 0 ? data.skins : new[] { "Base" };
        var skinSet = FindSkins(data);
        if (skinSet != null && skinSet.Count > 1 && skinSet.Count != skins.Length)
        {
            Debug.LogWarning($"Galería: '{data.id}' tiene {skinSet.Count} texturas de skin y " +
                             $"{skins.Length} nombres en AppCatalog. Revisá que coincidan.");
        }
        selectedSkin = Mathf.Clamp(CharacterManager.GetSavedSkin(data.id), 0, skins.Length - 1);
        skinCount = skins.Length;

        // El panel crece segun la cantidad de skins.
        float listHeight = skins.Length * SkinItemHeight + (skins.Length - 1) * SkinItemSpacing;
        skinPanel.sizeDelta = new Vector2(SkinPanelWidth, SkinHeaderHeight + listHeight + 16f);

        for (int i = 0; i < skins.Length; i++)
        {
            int captured = i;

            // Borde (color del personaje si esta elegida) + relleno oscuro.
            var itemGO = new GameObject("Skin_" + skins[i], typeof(RectTransform));
            itemGO.transform.SetParent(skinList, false);
            itemGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, SkinItemHeight);
            var outline = itemGO.AddComponent<Image>();
            UITheme.ApplyRounded(outline, UITheme.GlassRim, 0.7f);

            var button = itemGO.AddComponent<Button>();
            button.targetGraphic = outline;
            button.onClick.AddListener(() => SelectSkin(captured));

            var itemFillGO = new GameObject("Fill", typeof(RectTransform));
            itemFillGO.transform.SetParent(itemGO.transform, false);
            var itemFill = itemFillGO.AddComponent<Image>();
            UITheme.ApplyRounded(itemFill, new Color(0.05f, 0.09f, 0.20f, 0.85f), 0.7f);
            itemFill.raycastTarget = false;
            UITheme.Stretch(itemFillGO.GetComponent<RectTransform>(), 3f);

            // Miniatura con esquinas redondeadas.
            var thumbMaskGO = new GameObject("ThumbMask", typeof(RectTransform));
            thumbMaskGO.transform.SetParent(itemGO.transform, false);
            var thumbMask = thumbMaskGO.AddComponent<Image>();
            UITheme.ApplyRounded(thumbMask, new Color(1f, 1f, 1f, 0.10f), 0.6f);
            thumbMask.raycastTarget = false;
            thumbMaskGO.AddComponent<Mask>().showMaskGraphic = true;
            var thumbMaskRt = thumbMaskGO.GetComponent<RectTransform>();
            thumbMaskRt.anchorMin = new Vector2(0f, 0.5f);
            thumbMaskRt.anchorMax = new Vector2(0f, 0.5f);
            thumbMaskRt.pivot = new Vector2(0f, 0.5f);
            thumbMaskRt.anchoredPosition = new Vector2(10f, 0f);
            thumbMaskRt.sizeDelta = new Vector2(SkinItemHeight - 20f, SkinItemHeight - 20f);
            BuildSkinThumbnail(thumbMaskRt, data, skins[i], i);

            var label = UITheme.CreateText(itemGO.transform, "Name", skins[i], 28f, UITheme.TextPrimary,
                Vector2.zero, Vector2.zero, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UITheme.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(SkinItemHeight + 4f, 8f);
            label.rectTransform.offsetMax = new Vector2(-12f, -8f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 28f;

            // Check de "elegida": circulo del color del personaje con tilde blanco.
            var checkGO = new GameObject("Check", typeof(RectTransform));
            checkGO.transform.SetParent(itemGO.transform, false);
            var circle = checkGO.AddComponent<Image>();
            UITheme.ApplyRounded(circle, data.accent);
            circle.raycastTarget = false;
            var checkRt = checkGO.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(1f, 1f);
            checkRt.anchorMax = new Vector2(1f, 1f);
            checkRt.pivot = new Vector2(1f, 1f);
            checkRt.anchoredPosition = new Vector2(-8f, -8f);
            checkRt.sizeDelta = new Vector2(44f, 44f); // 44 = 2 x radio del sprite: queda circulo

            var tickGO = new GameObject("Tick", typeof(RectTransform));
            tickGO.transform.SetParent(checkGO.transform, false);
            var tick = tickGO.AddComponent<Image>();
            tick.sprite = UITheme.CheckSprite;
            tick.raycastTarget = false;
            UITheme.Stretch(tickGO.GetComponent<RectTransform>(), 4f);

            skinItems.Add((outline, checkGO));
        }

        RefreshSkinSelection(data);
    }

    /// <summary>
    /// Usa la imagen de Resources/UI/skins/&lt;id&gt;_&lt;skin&gt; si existe; si no, una miniatura del
    /// personaje con esa skin, renderizada por la camara del preview (ver CaptureSkinThumbnails).
    /// </summary>
    private void BuildSkinThumbnail(RectTransform parent, CharacterData data, string skin, int skinIndex)
    {
        var go = new GameObject("Thumb", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        UITheme.Stretch(go.GetComponent<RectTransform>());

        var sprite = Resources.Load<Sprite>($"UI/skins/{data.id}_{skin.ToLowerInvariant()}");
        if (sprite != null)
        {
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return;
        }

        var raw = go.AddComponent<RawImage>();
        raw.texture = GetSkinThumb(skinIndex);
        // Recorte de la mitad superior del encuadre (cabeza y torso), casi cuadrado.
        raw.uvRect = new Rect(0.2f, 0.45f, 0.6f, 0.53f);
        raw.raycastTarget = false;
    }

    private RenderTexture GetSkinThumb(int skinIndex)
    {
        while (skinThumbs.Count <= skinIndex)
        {
            // Misma proporcion que el preview (840x950) para que el encuadre coincida.
            var rt = new RenderTexture(420, 475, 24);
            rt.Create();
            skinThumbs.Add(rt);
        }

        // Limpia la del personaje anterior hasta que llegue la captura nueva.
        var thumb = skinThumbs[skinIndex];
        var previous = RenderTexture.active;
        RenderTexture.active = thumb;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;
        return thumb;
    }

    /// <summary>
    /// Renderiza al personaje una vez por skin en las miniaturas. Se hace todo en el mismo
    /// frame y despues se deja la skin elegida y el giro original, asi el preview no parpadea.
    /// </summary>
    private void CaptureSkinThumbnails(GameObject model)
    {
        var skins = model.GetComponent<CharacterSkins>();
        var rotation = model.transform.rotation;
        model.transform.rotation = stage.rotation * Quaternion.Euler(0f, ThumbYaw, 0f);

        for (int i = 0; i < skinCount && i < skinThumbs.Count; i++)
        {
            if (skins != null) skins.Apply(i);
            RenderStageTo(skinThumbs[i]);
        }

        model.transform.rotation = rotation;
        if (skins != null) skins.Apply(selectedSkin);
    }

    private void RenderStageTo(RenderTexture target)
    {
        // En URP (Unity 6) el render manual de una camara va por un RenderRequest.
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
        if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(stageCamera, request))
        {
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(stageCamera, request);
            return;
        }

        var previous = stageCamera.targetTexture;
        stageCamera.targetTexture = target;
        stageCamera.Render();
        stageCamera.targetTexture = previous;
    }

    private void SelectSkin(int skinIndex)
    {
        selectedSkin = skinIndex;
        RefreshSkinSelection(AppCatalog.Characters[index]);
        ApplySkinToPreview();
    }

    /// <summary>El preview es un clon del personaje de la escena, asi que trae su CharacterSkins.</summary>
    private void ApplySkinToPreview()
    {
        if (currentModel == null) return;
        var skins = currentModel.GetComponent<CharacterSkins>();
        if (skins != null) skins.Apply(selectedSkin);
    }

    private CharacterSkins FindSkins(CharacterData data)
    {
        var model = ResolveModel(data);
        return model != null ? model.GetComponent<CharacterSkins>() : null;
    }

    private void RefreshSkinSelection(CharacterData data)
    {
        for (int i = 0; i < skinItems.Count; i++)
        {
            bool selected = i == selectedSkin;
            skinItems[i].outline.color = selected ? data.accent : UITheme.GlassRim;
            skinItems[i].check.SetActive(selected);
        }
    }

    /// <summary>Fila de la columna derecha de la ficha, alineada al borde derecho.</summary>
    private static RectTransform CreateChipRect(RectTransform card, string name, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(card, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-CardPadding, y);
        rt.sizeDelta = new Vector2(ChipWidth, 72f);
        return rt;
    }

    public override void OnShow()
    {
        EnsureStage();
        stage.gameObject.SetActive(true);
        SetIndex(AppCatalog.GetCharacterIndex(State != null ? State.SelectedCharacterId : ""));
    }

    public override void OnHide()
    {
        if (stage != null) stage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // El escenario vive suelto en la escena, asi que hay que limpiarlo a mano.
        if (stage != null) Destroy(stage.gameObject);
        if (stageTexture != null)
        {
            stageTexture.Release();
            Destroy(stageTexture);
        }
        foreach (var thumb in skinThumbs)
        {
            thumb.Release();
            Destroy(thumb);
        }
        skinThumbs.Clear();
    }

    private void Update()
    {
        if (currentModel != null && Root != null && Root.gameObject.activeInHierarchy)
        {
            currentModel.transform.Rotate(Vector3.up, RotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void SetIndex(int newIndex)
    {
        int count = AppCatalog.Characters.Length;
        index = (newIndex % count + count) % count;
        var data = AppCatalog.Characters[index];

        for (int i = 0; i < logoFrames.Count; i++)
        {
            logoFrames[i].color = i == index ? data.accent : UITheme.GlassSoft;
        }

        nameLabel.text = data.displayName;
        taglineLabel.text = data.tagline;
        // Frase en una version clara del color del personaje, para que combine con sus chips.
        taglineLabel.color = Color.Lerp(data.accent, Color.white, 0.45f);
        descriptionLabel.text = data.description;

        for (int i = 0; i < tagLabels.Count; i++)
        {
            bool has = data.tags != null && i < data.tags.Length;
            tagLabels[i].text = has ? data.tags[i] : "";
            tagChips[i].color = data.accent;
            tagChips[i].gameObject.SetActive(has);
        }

        BuildSkinItems(data);

        ShowModel(data);
        ApplySkinToPreview();
    }

    private void ShowModel(CharacterData data)
    {
        if (currentModel != null) Destroy(currentModel);

        GameObject source = ResolveModel(data);
        if (source == null)
        {
            Debug.LogWarning($"Galería: no se encontró el modelo de '{data.id}'.");
            return;
        }

        currentModel = Instantiate(source, stage);
        currentModel.name = data.id + "_preview";
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.transform.localScale = Vector3.one;
        currentModel.SetActive(true);

        // Vuforia (DefaultObserverEventHandler) apaga los Renderer de todos los hijos del
        // Image Target mientras no detecta el QR. Como el original es hijo de ese target,
        // el clon hereda los renderers apagados y no se veia nada en la galeria.
        foreach (var renderer in currentModel.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }

        // La copia es solo para mirar. Se desactivan (no se destruyen) porque
        // ARCharacterSequence tiene [RequireComponent] sobre Animator/AudioSource.
        foreach (var sequence in currentModel.GetComponentsInChildren<ARCharacterSequence>(true))
        {
            sequence.enabled = false;
        }
        foreach (var audio in currentModel.GetComponentsInChildren<AudioSource>(true))
        {
            audio.enabled = false;
        }

        var animator = currentModel.GetComponentInChildren<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.applyRootMotion = false;
            animator.Play("Idle", 0, 0f);
        }

        // El encuadre se hace el frame siguiente: los bounds de un SkinnedMeshRenderer recien
        // instanciado todavia no son validos (sobre todo si el original estaba desactivado),
        // y la camara terminaba apuntando al origen del mundo, a 1000 unidades del escenario.
        // Ese era el motivo de que el personaje no se viera la primera vez que entrabas.
        if (frameRoutine != null) StopCoroutine(frameRoutine);
        frameRoutine = StartCoroutine(FrameNextFrame(currentModel));
    }

    private IEnumerator FrameNextFrame(GameObject model)
    {
        yield return null;
        if (model != null) FrameModel(model);

        // Con el encuadre ya calculado se sacan las miniaturas de las skins.
        if (model != null) CaptureSkinThumbnails(model);
        frameRoutine = null;
    }

    /// <summary>
    /// Busca primero en la escena (asi el personaje original conserva su Animator ya configurado)
    /// y si no esta, lo carga desde Resources.
    /// </summary>
    private GameObject ResolveModel(CharacterData data)
    {
        foreach (var sequence in FindObjectsByType<ARCharacterSequence>(FindObjectsInactive.Include))
        {
            if (sequence.name == data.id) return sequence.gameObject;
        }

        return string.IsNullOrEmpty(data.modelResource)
            ? null
            : Resources.Load<GameObject>(data.modelResource);
    }

    /// <summary>Encuadra la camara segun el tamano real del modelo (cada FBX trae su escala).</summary>
    private void FrameModel(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();

        Bounds bounds;
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        }
        else
        {
            bounds = new Bounds(model.transform.position, Vector3.one * 2f);
        }

        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        // Si los bounds salen degenerados, se encuadra sobre la posicion real del modelo.
        // Nunca sobre (0,0,0): el escenario vive lejos del origen y la camara quedaria ciega.
        if (size <= 0.01f)
        {
            bounds = new Bounds(model.transform.position, Vector3.one * 2f);
            size = 2f;
        }

        // 1.15 deja al personaje ocupando ~75% del alto del encuadre (con 1.9 quedaba en ~46%).
        float distance = size * 1.15f;
        stageCamera.transform.position = bounds.center + new Vector3(0f, size * 0.04f, distance);
        stageCamera.transform.LookAt(bounds.center);
        stageCamera.nearClipPlane = Mathf.Max(0.01f, distance * 0.01f);
        stageCamera.farClipPlane = distance * 5f;
    }

    private void EnsureStage()
    {
        if (stage != null) return;

        // OJO: el escenario NO puede colgar del panel, porque el panel es hijo de un Canvas
        // Screen Space Overlay y le pasaria su factor de escala a la camara, la luz y el modelo.
        // Va suelto en la raiz de la escena, lejos del origen.
        var stageGO = new GameObject("CharacterPreviewStage");
        stageGO.transform.position = StagePosition;
        stage = stageGO.transform;

        stageTexture = new RenderTexture(840, 950, 16);
        stageTexture.Create();

        var camGO = new GameObject("PreviewCamera");
        camGO.transform.SetParent(stage, false);
        stageCamera = camGO.AddComponent<Camera>();
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        stageCamera.targetTexture = stageTexture;
        // En URP toda camara necesita sus datos adicionales; si falta, el render sale en negro.
        if (camGO.GetComponent<UniversalAdditionalCameraData>() == null)
        {
            camGO.AddComponent<UniversalAdditionalCameraData>();
        }

        var lightGO = new GameObject("PreviewLight");
        lightGO.transform.SetParent(stage, false);
        lightGO.transform.localRotation = Quaternion.Euler(35f, 160f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.35f;

        preview.texture = stageTexture;
    }

    private void OnUseCharacter()
    {
        var data = AppCatalog.Characters[index];
        State.SetCharacter(data.id);

        // CharacterManager sigue siendo el que activa el personaje real en la escena AR.
        CharacterManager.SaveSelection(data.id);
        CharacterManager.SaveSkin(data.id, selectedSkin);

        Go(AppScreen.ARGuide);
    }
}
