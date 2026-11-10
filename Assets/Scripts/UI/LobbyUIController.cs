using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PANTALLA 1 - Lobby / registro. Pide Nombre y Carrera, los guarda en AppStateManager
/// y navega a la seleccion de evento.
/// </summary>
public class LobbyUIController : ScreenController
{
    private TMP_InputField nameInput;
    private TMP_Dropdown careerDropdown;
    private TextMeshProUGUI errorLabel;

    protected override void BuildUI()
    {
        UITheme.CreateFullScreenImage(Root, "Background", Resources.Load<Sprite>("fondo"), Color.white);
        UITheme.CreateFullScreenImage(Root, "Overlay", null, UITheme.Overlay);

        var safe = UITheme.CreateSafeArea(Root);
        var card = UITheme.CreateGlassCard(safe, "LobbyCard", new Vector2(880f, 1120f), Vector2.zero);

        // Todo el contenido se ancla al borde superior de la tarjeta (pivot 0.5,1 o 0,1),
        // asi las posiciones en Y se leen como "distancia desde arriba".
        var top = new Vector2(0.5f, 1f);
        var topLeft = new Vector2(0f, 1f);

        UITheme.CreateText(card, "Title", "RA GUIDE", 96f, UITheme.TextPrimary,
            new Vector2(0f, -60f), new Vector2(820f, 110f), FontStyles.Bold, TextAlignmentOptions.Center, top);

        UITheme.CreateText(card, "Subtitle", "DESCUBRE · APRENDE · EXPLORA", 32f, UITheme.TextSecondary,
            new Vector2(0f, -180f), new Vector2(820f, 46f), FontStyles.Bold, TextAlignmentOptions.Center, top);

        UITheme.CreateText(card, "NameCaption", "Nombre", 28f, UITheme.TextMuted,
            new Vector2(60f, -300f), new Vector2(400f, 36f), FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, topLeft);

        nameInput = UITheme.CreateInputField(card, "NameInput", "Escribe tu nombre",
            new Vector2(0f, -344f), new Vector2(760f, 110f), top);

        UITheme.CreateText(card, "CareerCaption", "Carrera", 28f, UITheme.TextMuted,
            new Vector2(60f, -490f), new Vector2(400f, 36f), FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, topLeft);

        careerDropdown = UITheme.CreateDropdown(card, "CareerDropdown", "Selecciona tu carrera",
            AppCatalog.Careers, new Vector2(0f, -534f), new Vector2(760f, 110f), top);

        errorLabel = UITheme.CreateText(card, "ErrorLabel", "", 26f, new Color(1f, 0.45f, 0.4f, 1f),
            new Vector2(0f, -664f), new Vector2(760f, 40f), FontStyles.Italic,
            TextAlignmentOptions.Center, top);

        var continueButton = UITheme.CreateButton(card, "ContinueButton", "Continuar",
            new Vector2(0f, -730f), new Vector2(660f, 120f), UITheme.Gold, UITheme.GoldText, 46f, top);
        continueButton.onClick.AddListener(OnContinue);

        var exitButton = UITheme.CreateButton(card, "ExitButton", "Salir",
            new Vector2(0f, -870f), new Vector2(660f, 100f), UITheme.Red, UITheme.TextPrimary, 40f, top);
        exitButton.onClick.AddListener(OnExit);
    }

    public override void OnShow()
    {
        errorLabel.text = "";

        if (State == null) return;

        if (!string.IsNullOrEmpty(State.UserName))
        {
            nameInput.SetTextWithoutNotify(State.UserName);
        }

        int careerIndex = System.Array.IndexOf(AppCatalog.Careers, State.Career);
        if (careerIndex >= 0)
        {
            careerDropdown.SetValueWithoutNotify(careerIndex + 1); // +1 por el placeholder
            careerDropdown.RefreshShownValue();
        }
    }

    private void OnContinue()
    {
        string typedName = nameInput.text.Trim();
        if (string.IsNullOrEmpty(typedName))
        {
            errorLabel.text = "Escribe tu nombre para continuar.";
            return;
        }

        if (careerDropdown.value <= 0)
        {
            errorLabel.text = "Selecciona tu carrera para continuar.";
            return;
        }

        string selectedCareer = AppCatalog.Careers[careerDropdown.value - 1];
        State.SetUser(typedName, selectedCareer);
        errorLabel.text = "";

        Go(AppScreen.EventSelection);
    }

    private void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
