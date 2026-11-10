using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PANTALLA 4 - Guia AR. Es la unica pantalla sin fondo opaco: deja ver la camara de Vuforia.
/// Reutiliza el flujo AR existente (CharacterManager + ARCharacterSequence + VuforiaTargetHandler);
/// aca solo se le pone UI encima y se activa el personaje elegido.
/// </summary>
public class ARGuideUIController : ScreenController
{
    public static ARGuideUIController Instance { get; private set; }

    private static readonly Color DanceColor = new Color(0.55f, 0.35f, 0.9f, 0.95f);

    private TextMeshProUGUI eventLabel;
    private TextMeshProUGUI audioLabel;
    private TextMeshProUGUI guideMessage;
    private Image audioIcon;

    private GameObject danceButtonGO;

    private GameObject infoPanel;
    private TextMeshProUGUI infoTitle;
    private TextMeshProUGUI infoBody;

    private bool audioOn = true;

    private void Awake() => Instance = this;

    protected override void BuildUI()
    {
        var safe = UITheme.CreateSafeArea(Root);
        var top = new Vector2(0.5f, 1f);
        var topLeft = new Vector2(0f, 1f);
        var topRight = new Vector2(1f, 1f);
        var bottom = new Vector2(0.5f, 0f);

        // --- Barra superior ---
        var backButton = UITheme.CreateButton(safe, "BackButton", "‹  Volver", new Vector2(40f, -40f),
            new Vector2(260f, 100f), UITheme.GlassSoft, UITheme.TextPrimary, 34f, topLeft);
        backButton.onClick.AddListener(() => Go(AppScreen.CharacterSelection));

        UITheme.CreateText(safe, "Title", "RA GUIDE", 60f, UITheme.TextPrimary,
            new Vector2(0f, -35f), new Vector2(520f, 76f), FontStyles.Bold, TextAlignmentOptions.Center, top);

        UITheme.CreateText(safe, "Subtitle", "Guía AR", 30f, UITheme.TextSecondary,
            new Vector2(0f, -110f), new Vector2(520f, 42f), FontStyles.Normal, TextAlignmentOptions.Center, top);

        var audioButton = UITheme.CreateButton(safe, "AudioButton", "", new Vector2(-40f, -40f),
            new Vector2(110f, 110f), UITheme.GlassSoft, UITheme.TextPrimary, 40f, topRight);
        audioButton.onClick.AddListener(ToggleAudio);
        audioIcon = audioButton.GetComponent<Image>();
        var audioButtonLabel = audioButton.GetComponentInChildren<TextMeshProUGUI>();
        audioButtonLabel.text = "♪";
        audioButtonLabel.fontSize = 48f;

        audioLabel = UITheme.CreateText(safe, "AudioLabel", "Audio ON", 24f, UITheme.TextPrimary,
            new Vector2(-40f, -158f), new Vector2(200f, 36f), FontStyles.Bold,
            TextAlignmentOptions.Center, topRight);

        // --- Evento activo ---
        var eventChip = UITheme.CreateButton(safe, "EventChip", "", new Vector2(0f, -180f),
            new Vector2(760f, 96f), UITheme.GlassSoft, UITheme.TextPrimary, 32f, top);
        eventChip.onClick.AddListener(ToggleInfo);
        eventLabel = eventChip.GetComponentInChildren<TextMeshProUGUI>();
        eventLabel.alignment = TextAlignmentOptions.Center;

        // --- Bocadillo del guia ---
        var bubble = UITheme.CreateGlassCard(safe, "GuideBubble", new Vector2(700f, 220f),
            new Vector2(0f, 560f), bottom);
        guideMessage = UITheme.CreateText(bubble, "Message", "", 32f, UITheme.TextPrimary,
            Vector2.zero, Vector2.zero, FontStyles.Normal, TextAlignmentOptions.Center);
        UITheme.Stretch(guideMessage.rectTransform, 30f);

        // --- Barra inferior de acciones ---
        // Reparto en el ancho de 1080 (±540) dejando separacion entre los 4 botones.
        var homeButton = UITheme.CreateButton(safe, "HomeButton", "Inicio", new Vector2(-420f, 60f),
            new Vector2(170f, 120f), UITheme.GlassSoft, UITheme.TextPrimary, 28f, bottom);
        homeButton.onClick.AddListener(() => Go(AppScreen.Lobby));

        var repeatButton = UITheme.CreateButton(safe, "RepeatButton", "Repetir", new Vector2(-170f, 60f),
            new Vector2(290f, 120f), UITheme.Gold, UITheme.GoldText, 34f, bottom);
        repeatButton.onClick.AddListener(OnRepeat);

        var infoButton = UITheme.CreateButton(safe, "EventInfoButton", "Información\ndel evento",
            new Vector2(155f, 60f), new Vector2(290f, 120f), UITheme.Blue, UITheme.TextPrimary, 27f, bottom);
        infoButton.onClick.AddListener(ToggleInfo);

        var changeButton = UITheme.CreateButton(safe, "ChangeCharacterButton", "Cambiar\npersonaje",
            new Vector2(420f, 60f), new Vector2(190f, 120f), UITheme.GlassSoft, UITheme.TextPrimary, 25f, bottom);
        changeButton.onClick.AddListener(() => Go(AppScreen.CharacterSelection));

        // Aparece recien cuando la secuencia termina, justo encima de la barra inferior.
        var danceButton = UITheme.CreateButton(safe, "DanceButton", "♪  Bailar", new Vector2(0f, 200f),
            new Vector2(320f, 100f), DanceColor, UITheme.TextPrimary, 38f, bottom);
        danceButton.onClick.AddListener(OnDance);
        danceButtonGO = danceButton.gameObject;
        danceButtonGO.SetActive(false);

        BuildInfoPanel(safe);
    }

    private void BuildInfoPanel(RectTransform safe)
    {
        infoPanel = new GameObject("EventInfoPanel", typeof(RectTransform));
        infoPanel.transform.SetParent(safe, false);
        UITheme.Stretch(infoPanel.GetComponent<RectTransform>());
        infoPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        var modal = UITheme.CreateGlassCard(infoPanel.transform, "InfoCard", new Vector2(950f, 1250f), Vector2.zero);
        var top = new Vector2(0.5f, 1f);

        infoTitle = UITheme.CreateText(modal, "InfoTitle", "", 46f, UITheme.TextPrimary,
            new Vector2(0f, -45f), new Vector2(860f, 95f), FontStyles.Bold, TextAlignmentOptions.Top, top);

        infoBody = UITheme.CreateText(modal, "InfoBody", "", 28f, UITheme.TextMuted,
            new Vector2(0f, -160f), new Vector2(860f, 880f), FontStyles.Normal, TextAlignmentOptions.TopLeft, top);

        var close = UITheme.CreateButton(modal, "CloseButton", "Cerrar", new Vector2(0f, -1090f),
            new Vector2(600f, 100f), UITheme.Red, UITheme.TextPrimary, 38f, top);
        close.onClick.AddListener(() => infoPanel.SetActive(false));

        infoPanel.SetActive(false);
    }

    public override void OnShow()
    {
        infoPanel.SetActive(false);
        if (danceButtonGO != null) danceButtonGO.SetActive(false);

        var selectedEvent = State != null ? State.SelectedEvent : AppCatalog.Events[0];
        eventLabel.text = "Evento:  " + selectedEvent.title;
        infoTitle.text = selectedEvent.title;
        infoBody.text = selectedEvent.DetailsText;

        var character = State != null ? State.SelectedCharacter : AppCatalog.Characters[0];
        guideMessage.text = $"¡Hola{FirstName()}! Soy {character.displayName}.\nApunta al código QR para comenzar.";

        // Activa el personaje elegido y rearma la deteccion del QR.
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.ApplySelection();
        }
        else
        {
            guideMessage.text = "Falta conectar la escena AR.\nCorre AR Guide → Ejecutar TODO y guarda.";
            Debug.LogWarning("ARGuideUIController: no hay CharacterManager en la escena. " +
                             "Corre AR Guide > Ejecutar TODO y guarda la escena (Ctrl+S).");
        }
    }

    public override void OnHide()
    {
        // Fuera de la pantalla AR el personaje no debe verse ni reaccionar al QR.
        if (CharacterManager.Instance == null) return;

        var active = CharacterManager.Instance.Active;
        if (active != null && active.gameObject.activeSelf)
        {
            active.ResetSequence(); // corta corrutinas y audio
        }
        CharacterManager.Instance.HideAll();
    }

    private string FirstName()
    {
        if (State == null || string.IsNullOrEmpty(State.UserName)) return "";
        return ", " + State.UserName.Split(' ')[0];
    }

    /// <summary>La llama ARCharacterSequence cuando termina la secuencia completa.</summary>
    public void OnSequenceFinished()
    {
        guideMessage.text = "¡Sigamos!\nTu próximo destino está por aquí.";
        if (danceButtonGO != null) danceButtonGO.SetActive(true);
    }

    /// <summary>La llama ARCharacterSequence al arrancar.</summary>
    public void OnSequenceStarted()
    {
        var character = State != null ? State.SelectedCharacter : AppCatalog.Characters[0];
        guideMessage.text = $"{character.displayName} te está guiando…";
    }

    /// <summary>La llama ARCharacterSequence al hacer Reset.</summary>
    public void OnSequenceReset()
    {
        guideMessage.text = "Apunta al código QR para comenzar.";
        if (danceButtonGO != null) danceButtonGO.SetActive(false);
    }

    private void OnRepeat()
    {
        var active = ActiveSequence;
        if (active == null) return;

        active.ResetSequence();
        active.StartSequence();
    }

    private void OnDance()
    {
        ActiveSequence?.PlayDance();
    }

    private ARCharacterSequence ActiveSequence =>
        CharacterManager.Instance != null ? CharacterManager.Instance.Active : null;

    private void ToggleAudio()
    {
        audioOn = !audioOn;
        AudioListener.volume = audioOn ? 1f : 0f;
        audioLabel.text = audioOn ? "Audio ON" : "Audio OFF";
        audioIcon.color = audioOn ? UITheme.GlassSoft : UITheme.Red;
    }

    private void ToggleInfo()
    {
        infoPanel.SetActive(!infoPanel.activeSelf);
    }
}
