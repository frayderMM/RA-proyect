using UnityEngine;
using Vuforia;

/// <summary>
/// Se coloca en el mismo GameObject que el ObserverBehaviour del Image Target ("Image Target QR").
/// Se suscribe a OnTargetStatusChanged por codigo, sin depender del Default Observer Event Handler.
/// Dispara la secuencia del personaje ACTIVO (el que eligio CharacterManager segun la carrera).
/// Si ARCharacterSequence.ResetSequence() lo deshabilita, deja de reconocer el QR hasta que
/// se vuelva a habilitar (boton Start, o entrar de nuevo desde el lobby).
/// </summary>
[RequireComponent(typeof(ObserverBehaviour))]
public class VuforiaTargetHandler : MonoBehaviour
{
    private ObserverBehaviour observerBehaviour;
    private ARCharacterSequence cachedSequence;

    private void Awake()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
    }

    /// <summary>
    /// Si hay CharacterManager manda el; si no (escena con un solo personaje), usa el
    /// ARCharacterSequence que haya en los hijos.
    /// </summary>
    private ARCharacterSequence ResolveSequence()
    {
        if (CharacterManager.Instance != null && CharacterManager.Instance.Active != null)
        {
            return CharacterManager.Instance.Active;
        }

        if (cachedSequence == null)
        {
            cachedSequence = GetComponentInChildren<ARCharacterSequence>(true);
        }
        return cachedSequence;
    }

    private void OnEnable()
    {
        if (observerBehaviour == null) return;

        observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
        // Por si el target ya estaba siendo trackeado antes de que este componente se habilitara.
        OnTargetStatusChanged(observerBehaviour, observerBehaviour.TargetStatus);
    }

    private void OnDisable()
    {
        if (observerBehaviour == null) return;
        observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        bool isTracked = targetStatus.Status == Status.TRACKED || targetStatus.Status == Status.EXTENDED_TRACKED;
        if (!isTracked) return;

        var active = ResolveSequence();
        if (active == null) return;

        if (!active.gameObject.activeSelf)
        {
            active.gameObject.SetActive(true);
        }

        active.StartSequence();
    }
}
