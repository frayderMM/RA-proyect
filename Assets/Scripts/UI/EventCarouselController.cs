using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PANTALLA 2 - Seleccion de evento. Carrusel con la card central destacada y las laterales
/// mas chicas, dots indicadores, panel de informacion y navegacion.
/// Las cards se generan por codigo a partir de AppCatalog.Events.
/// </summary>
public class EventCarouselController : ScreenController
{
    private const float CardWidth = 460f;
    private const float CardHeight = 760f;
    private const float Spacing = 520f;
    private const float SideScale = 0.72f;
    private const float CardCornerScale = 2f; // radio de esquina ~44 px

    private readonly List<RectTransform> cards = new();
    private readonly List<Image> dots = new();

    private RectTransform carousel;
    private GameObject infoPanel;
    private TextMeshProUGUI infoTitle;
    private TextMeshProUGUI infoBody;

    private int index;

    protected override void BuildUI()
    {
        // Fondo a pleno, sin velo oscuro ni tarjeta de vidrio encima.
        UITheme.CreateFullScreenImage(Root, "Background", Resources.Load<Sprite>("UI/fondo_evento"), Color.white);

        var safe = UITheme.CreateSafeArea(Root);

        // Contenedor invisible: solo agrupa y centra el contenido. Va a todo el ancho para que
        // las flechas y el carrusel no se salgan en pantallas angostas.
        var cardGO = new GameObject("EventContent", typeof(RectTransform));
        cardGO.transform.SetParent(safe, false);
        var card = cardGO.GetComponent<RectTransform>();
        card.anchorMin = new Vector2(0f, 0.5f);
        card.anchorMax = new Vector2(1f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(0f, 1500f);

        var top = new Vector2(0.5f, 1f);

        var title = UITheme.CreateText(card, "Title", "ELIGE TU EVENTO", 68f, UITheme.TextPrimary,
            new Vector2(0f, -40f), new Vector2(940f, 100f), FontStyles.Bold, TextAlignmentOptions.Center, top);
        UITheme.AddTextShadow(title);

        // Zona del carrusel (recorta las cards laterales)
        var carouselGO = new GameObject("Carousel", typeof(RectTransform));
        carouselGO.transform.SetParent(card, false);
        carousel = carouselGO.GetComponent<RectTransform>();
        carousel.anchorMin = new Vector2(0f, 1f);
        carousel.anchorMax = new Vector2(1f, 1f);
        carousel.pivot = top;
        carousel.anchoredPosition = new Vector2(0f, -170f);
        carousel.sizeDelta = new Vector2(0f, CardHeight + 40f);
        carouselGO.AddComponent<RectMask2D>();

        // Imagen casi invisible para poder recibir el arrastre (un RectMask2D solo no recibe eventos).
        var swipeArea = carouselGO.AddComponent<Image>();
        swipeArea.color = new Color(0f, 0f, 0f, 0.004f);
        var swipe = carouselGO.AddComponent<SwipeDetector>();
        swipe.OnSwipe += direction => SetIndex(index - direction);

        for (int i = 0; i < AppCatalog.Events.Length; i++)
        {
            cards.Add(BuildEventCard(AppCatalog.Events[i], i));
        }

        BuildArrows(card);
        BuildDots(card);

        var infoButton = UITheme.CreateButton(card, "InfoButton", "Información",
            new Vector2(-175f, -1050f), new Vector2(330f, 110f), UITheme.Blue, UITheme.TextPrimary, 36f, top);
        infoButton.onClick.AddListener(ToggleInfo);

        var goButton = UITheme.CreateButton(card, "GoButton", "Ir al evento",
            new Vector2(175f, -1050f), new Vector2(330f, 110f), UITheme.Gold, UITheme.GoldText, 36f, top);
        goButton.onClick.AddListener(OnGoToEvent);

        var backButton = UITheme.CreateButton(card, "BackButton", "← Volver",
            new Vector2(0f, -1180f), new Vector2(690f, 100f), UITheme.Red, UITheme.TextPrimary, 38f, top);
        backButton.onClick.AddListener(() => Go(AppScreen.Lobby));

        BuildInfoPanel(safe);
    }

    private RectTransform BuildEventCard(EventData data, int cardIndex)
    {
        var holderGO = new GameObject("EventCard_" + data.id, typeof(RectTransform));
        holderGO.transform.SetParent(carousel, false);
        var holder = holderGO.GetComponent<RectTransform>();
        holder.anchorMin = new Vector2(0.5f, 0.5f);
        holder.anchorMax = new Vector2(0.5f, 0.5f);
        holder.pivot = new Vector2(0.5f, 0.5f);
        holder.sizeDelta = new Vector2(CardWidth, CardHeight);

        var rim = holderGO.AddComponent<Image>();
        UITheme.ApplyRounded(rim, UITheme.GlassRim, CardCornerScale);

        var button = holderGO.AddComponent<Button>();
        button.targetGraphic = rim;
        button.onClick.AddListener(() => SetIndex(cardIndex));

        // Mascara redondeada: la foto y la franja de texto se recortan con la misma forma que
        // el borde, asi las 4 esquinas quedan curvas (antes la imagen era un rectangulo recto).
        var maskGO = new GameObject("RoundedMask", typeof(RectTransform));
        maskGO.transform.SetParent(holderGO.transform, false);
        var maskImage = maskGO.AddComponent<Image>();
        UITheme.ApplyRounded(maskImage, Color.white, CardCornerScale);
        maskImage.raycastTarget = false;
        maskGO.AddComponent<Mask>().showMaskGraphic = false;
        UITheme.Stretch(maskGO.GetComponent<RectTransform>(), 4f);

        // Imagen del evento
        var imageGO = new GameObject("Image", typeof(RectTransform));
        imageGO.transform.SetParent(maskGO.transform, false);
        var image = imageGO.AddComponent<Image>();
        image.sprite = data.Sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
        UITheme.Stretch(imageGO.GetComponent<RectTransform>());

        // Franja inferior para que el texto se lea (la mascara le redondea las esquinas de abajo)
        var scrimGO = new GameObject("Scrim", typeof(RectTransform));
        scrimGO.transform.SetParent(maskGO.transform, false);
        var scrim = scrimGO.AddComponent<Image>();
        scrim.color = new Color(0.03f, 0.05f, 0.14f, 0.82f);
        scrim.raycastTarget = false;
        var scrimRt = scrimGO.GetComponent<RectTransform>();
        scrimRt.anchorMin = new Vector2(0f, 0f);
        scrimRt.anchorMax = new Vector2(1f, 0f);
        scrimRt.pivot = new Vector2(0.5f, 0f);
        scrimRt.anchoredPosition = Vector2.zero;
        scrimRt.sizeDelta = new Vector2(0f, 250f);

        UITheme.CreateText(scrimRt, "Title", data.title, 38f, UITheme.TextPrimary,
            new Vector2(24f, -20f), new Vector2(CardWidth - 60f, 90f), FontStyles.Bold,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));

        UITheme.CreateText(scrimRt, "Description", data.shortDescription, 26f, UITheme.TextMuted,
            new Vector2(24f, -118f), new Vector2(CardWidth - 60f, 110f), FontStyles.Normal,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));

        // Etiqueta (Recomendado / Popular / Nuevo)
        if (!string.IsNullOrEmpty(data.badge))
        {
            var badgeGO = new GameObject("Badge", typeof(RectTransform));
            badgeGO.transform.SetParent(holderGO.transform, false);
            var badgeImage = badgeGO.AddComponent<Image>();
            UITheme.ApplyRounded(badgeImage, data.badge == "Recomendado" ? UITheme.Gold : UITheme.Blue);
            badgeImage.raycastTarget = false;

            var badgeRt = badgeGO.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 1f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(28f, -28f);
            badgeRt.sizeDelta = new Vector2(230f, 62f);

            var badgeText = UITheme.CreateText(badgeRt, "Text", data.badge, 26f,
                data.badge == "Recomendado" ? UITheme.GoldText : UITheme.TextPrimary,
                Vector2.zero, Vector2.zero, FontStyles.Bold);
            UITheme.Stretch(badgeText.rectTransform, 6f);
        }

        return holder;
    }

    private void BuildArrows(RectTransform card)
    {
        // Pegadas a los bordes (no a +-430 desde el centro) para que nunca queden fuera de pantalla.
        var prev = UITheme.CreateButton(card, "PrevButton", "‹", new Vector2(24f, -510f),
            new Vector2(110f, 110f), UITheme.Glass, UITheme.TextPrimary, 60f, new Vector2(0f, 1f));
        prev.onClick.AddListener(() => SetIndex(index - 1));

        var next = UITheme.CreateButton(card, "NextButton", "›", new Vector2(-24f, -510f),
            new Vector2(110f, 110f), UITheme.Glass, UITheme.TextPrimary, 60f, new Vector2(1f, 1f));
        next.onClick.AddListener(() => SetIndex(index + 1));
    }

    private void BuildDots(RectTransform card)
    {
        var rowGO = new GameObject("Dots", typeof(RectTransform));
        rowGO.transform.SetParent(card, false);
        var row = rowGO.GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0.5f, 1f);
        row.anchorMax = new Vector2(0.5f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, -970f);
        row.sizeDelta = new Vector2(600f, 40f);

        int count = AppCatalog.Events.Length;
        float spacing = 46f;
        float startX = -(count - 1) * spacing * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var dotGO = new GameObject("Dot_" + i, typeof(RectTransform));
            dotGO.transform.SetParent(rowGO.transform, false);
            var dot = dotGO.AddComponent<Image>();
            UITheme.ApplyRounded(dot, UITheme.GlassSoft);
            dot.raycastTarget = false;

            var dotRt = dotGO.GetComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0.5f, 0.5f);
            dotRt.anchorMax = new Vector2(0.5f, 0.5f);
            dotRt.pivot = new Vector2(0.5f, 0.5f);
            dotRt.sizeDelta = new Vector2(22f, 22f);
            dotRt.anchoredPosition = new Vector2(startX + i * spacing, 0f);

            dots.Add(dot);
        }
    }

    private void BuildInfoPanel(RectTransform safe)
    {
        infoPanel = new GameObject("InfoPanel", typeof(RectTransform));
        infoPanel.transform.SetParent(safe, false);
        UITheme.Stretch(infoPanel.GetComponent<RectTransform>());

        var blocker = infoPanel.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.6f);

        var modal = UITheme.CreateGlassCard(infoPanel.transform, "InfoCard",
            new Vector2(950f, 1250f), Vector2.zero);

        var top = new Vector2(0.5f, 1f);

        infoTitle = UITheme.CreateText(modal, "InfoTitle", "", 46f, UITheme.TextPrimary,
            new Vector2(0f, -45f), new Vector2(860f, 95f), FontStyles.Bold,
            TextAlignmentOptions.Top, top);

        infoBody = UITheme.CreateText(modal, "InfoBody", "", 28f, UITheme.TextMuted,
            new Vector2(0f, -160f), new Vector2(860f, 880f), FontStyles.Normal,
            TextAlignmentOptions.TopLeft, top);

        var close = UITheme.CreateButton(modal, "CloseButton", "Cerrar",
            new Vector2(0f, -1090f), new Vector2(600f, 100f), UITheme.Red, UITheme.TextPrimary, 38f, top);
        close.onClick.AddListener(() => infoPanel.SetActive(false));

        infoPanel.SetActive(false);
    }

    public override void OnShow()
    {
        infoPanel.SetActive(false);
        SetIndex(AppCatalog.GetEventIndex(State != null ? State.SelectedEventId : ""));

        // Al entrar, las cards se colocan de una en vez de deslizarse desde el centro.
        for (int i = 0; i < cards.Count; i++)
        {
            bool center = i == index;
            cards[i].anchoredPosition = new Vector2((i - index) * Spacing, 0f);
            cards[i].localScale = Vector3.one * (center ? 1f : SideScale);
        }
    }

    private void SetIndex(int newIndex)
    {
        int count = AppCatalog.Events.Length;
        index = (newIndex % count + count) % count;

        for (int i = 0; i < cards.Count; i++)
        {
            bool center = i == index;
            cards[i].SetSiblingIndex(center ? cards.Count : 0);
            cards[i].GetComponent<Image>().color = center ? UITheme.Blue : UITheme.GlassRim;
        }

        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i == index ? UITheme.Gold : UITheme.GlassSoft;
        }

        var data = AppCatalog.Events[index];
        infoTitle.text = data.title;
        infoBody.text = data.DetailsText;
    }

    /// <summary>Desplaza y escala las cards suavemente hacia su posicion objetivo.</summary>
    private void Update()
    {
        float t = Time.deltaTime * 12f;

        for (int i = 0; i < cards.Count; i++)
        {
            var rt = cards[i];
            bool center = i == index;

            var targetPosition = new Vector2((i - index) * Spacing, 0f);
            var targetScale = Vector3.one * (center ? 1f : SideScale);

            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPosition, t);
            rt.localScale = Vector3.Lerp(rt.localScale, targetScale, t);
        }
    }

    private void ToggleInfo()
    {
        infoPanel.SetActive(!infoPanel.activeSelf);
    }

    private void OnGoToEvent()
    {
        State.SetEvent(AppCatalog.Events[index].id);
        Go(AppScreen.CharacterSelection);
    }
}
