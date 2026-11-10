using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Arma el Canvas unico de la app (Screen Space Overlay, 1080x1920, Match 0.5) con las 4
/// pantallas como paneles y muestra una a la vez. Un solo scene: asi no se toca nada de la
/// configuracion de Vuforia ni del Build Settings.
/// Se crea sola al arrancar; no requiere colocar nada en la escena.
/// </summary>
public class ScreenNavigation : MonoBehaviour
{
    public static ScreenNavigation Instance { get; private set; }

    private readonly Dictionary<AppScreen, ScreenController> controllers = new();
    private readonly Dictionary<AppScreen, GameObject> panels = new();

    private AppScreen current = AppScreen.Lobby;
    public AppScreen Current => current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<ScreenNavigation>() != null) return;
        new GameObject("ScreenNavigation").AddComponent<ScreenNavigation>();
    }

    private void Awake()
    {
        Instance = this;

        EnsureEventSystem();

        var canvas = UITheme.CreateCanvas(transform, "AppCanvas", 100);

        CreateScreen<LobbyUIController>(AppScreen.Lobby, "LobbyPanel", canvas.transform);
        CreateScreen<EventCarouselController>(AppScreen.EventSelection, "EventSelectionPanel", canvas.transform);
        CreateScreen<CharacterSelectionController>(AppScreen.CharacterSelection, "CharacterSelectionPanel", canvas.transform);
        CreateScreen<ARGuideUIController>(AppScreen.ARGuide, "ARGuidePanel", canvas.transform);

        Show(AppScreen.Lobby);
    }

    private void CreateScreen<T>(AppScreen screen, string panelName, Transform canvasTransform)
        where T : ScreenController
    {
        var panelGO = new GameObject(panelName, typeof(RectTransform));
        panelGO.transform.SetParent(canvasTransform, false);
        UITheme.Stretch(panelGO.GetComponent<RectTransform>());

        var controller = panelGO.AddComponent<T>();
        controller.Build(panelGO.GetComponent<RectTransform>());

        panels[screen] = panelGO;
        controllers[screen] = controller;
        panelGO.SetActive(false);
    }

    public void Show(AppScreen screen)
    {
        foreach (var pair in panels)
        {
            bool active = pair.Key == screen;
            if (pair.Value.activeSelf != active)
            {
                if (!active && controllers.TryGetValue(pair.Key, out var hiding))
                {
                    hiding.OnHide();
                }
                pair.Value.SetActive(active);
            }
        }

        current = screen;
        if (controllers.TryGetValue(screen, out var shown))
        {
            shown.OnShow();
        }
    }

    private void EnsureEventSystem()
    {
        var eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        }

        // El proyecto usa el Input System nuevo: StandaloneInputModule no procesa clicks.
        var legacy = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacy != null) Destroy(legacy);
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }
}
