using UnityEngine;

public enum AppScreen
{
    Lobby = 0,
    EventSelection = 1,
    CharacterSelection = 2,
    ARGuide = 3
}

/// <summary>
/// Base de cada pantalla. El controlador construye su UI dentro del panel que le entrega
/// ScreenNavigation y reacciona cuando la pantalla se muestra u oculta.
/// </summary>
public abstract class ScreenController : MonoBehaviour
{
    protected RectTransform Root { get; private set; }
    protected AppStateManager State => AppStateManager.Instance;

    public void Build(RectTransform root)
    {
        Root = root;
        BuildUI();
    }

    protected abstract void BuildUI();

    public virtual void OnShow() { }
    public virtual void OnHide() { }

    protected void Go(AppScreen screen) => ScreenNavigation.Instance.Show(screen);
}
