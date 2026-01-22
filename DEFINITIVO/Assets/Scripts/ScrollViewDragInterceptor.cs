using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollViewDragInterceptor : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ScrollRect scrollRect;
    public DraggablePanel panel;

    [Header("Sensibilidade")]
    public float deadZone = 8f; // ignora micro-movimentos

    private bool draggingPanel = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Só intercepta se:
        // 1) Scroll está no topo
        // 2) Swipe para baixo
        // 3) Movimento significativo
        // 4) Painel está expandido
        if (scrollRect.verticalNormalizedPosition >= 0.99f &&
            eventData.delta.y < -deadZone &&
            panel.IsExpanded())
        {
            draggingPanel = true;
            scrollRect.enabled = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!draggingPanel)
            return;

        panel.DragFromScroll(eventData.delta.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!draggingPanel)
            return;

        panel.EndDragFromScroll();
        scrollRect.enabled = true;
        draggingPanel = false;
    }
}
