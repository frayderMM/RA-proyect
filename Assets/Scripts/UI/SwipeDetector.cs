using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Detecta deslizamientos horizontales sobre un area de UI y avisa la direccion.
/// Los clicks sobre los hijos (botones) siguen funcionando: Button no maneja drag, asi que
/// el arrastre sube hasta este componente.
/// </summary>
public class SwipeDetector : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>-1 = swipe hacia la izquierda (siguiente), +1 = hacia la derecha (anterior).</summary>
    public Action<int> OnSwipe;

    [SerializeField] private float threshold = 80f;

    private float startX;
    private bool dragging;

    public void OnBeginDrag(PointerEventData eventData)
    {
        startX = eventData.position.x;
        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Necesario para que Unity siga enviando OnEndDrag.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        float delta = eventData.position.x - startX;
        if (Mathf.Abs(delta) < threshold) return;

        OnSwipe?.Invoke(delta < 0f ? -1 : 1);
    }
}
