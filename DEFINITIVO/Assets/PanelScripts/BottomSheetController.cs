using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DraggablePanel : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("Configurações")]
    public float animationSpeed = 0.3f; // Velocidade da animação
    public Vector2 initialPosition;     // Posição recolhida
    public Vector2 expandedPosition;    // Posição expandida

    [Header("Sensibilidade")]
    public float dragMultiplier = 1.8f;     // Amplificação do movimento
    public float velocityThreshold = 800f;  // Swipe rápido
    public float snapThreshold = 0.25f;     // % para decidir abrir/fechar

    private RectTransform rectTransform;
    private Coroutine animationCoroutine;

    // velocidade calculada manualmente
    private float currentVelocity;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
    }

    // ===== DRAG DIRETO NO PAINEL =====
    public void OnDrag(PointerEventData eventData)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        float deltaY = eventData.delta.y * dragMultiplier;

        rectTransform.anchoredPosition += new Vector2(0, deltaY);

        currentVelocity = deltaY / Time.deltaTime;

        ClampPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DecideFinalPosition(currentVelocity);
        currentVelocity = 0f;
    }

    // ===== DRAG VINDO DO SCROLLVIEW =====
    public void DragFromScroll(float deltaY)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        float adjustedDelta = deltaY * dragMultiplier;

        rectTransform.anchoredPosition += new Vector2(0, adjustedDelta);

        currentVelocity = adjustedDelta / Time.deltaTime;

        ClampPosition();
    }

    public void EndDragFromScroll()
    {
        DecideFinalPosition(currentVelocity);
        currentVelocity = 0f;
    }

    // ===== DECISÃO FINAL =====
    private void DecideFinalPosition(float velocity)
    {
        float totalHeight = expandedPosition.y - initialPosition.y;
        float currentOffset = rectTransform.anchoredPosition.y - initialPosition.y;
        float normalized = currentOffset / totalHeight;

        // Swipe rápido
        if (Mathf.Abs(velocity) > velocityThreshold)
        {
            if (velocity > 0)
                Animate(expandedPosition);
            else
                Animate(initialPosition);

            return;
        }

        // Swipe lento
        if (normalized > snapThreshold)
            Animate(expandedPosition);
        else
            Animate(initialPosition);
    }

    // ===== UTILIDADES =====
    private void Animate(Vector2 target)
    {
        animationCoroutine = StartCoroutine(AnimateToPosition(target));
    }

    private void ClampPosition()
    {
        float clampedY = Mathf.Clamp(
            rectTransform.anchoredPosition.y,
            initialPosition.y,
            expandedPosition.y
        );

        rectTransform.anchoredPosition =
            new Vector2(rectTransform.anchoredPosition.x, clampedY);
    }

    private IEnumerator AnimateToPosition(Vector2 targetPosition)
    {
        float elapsedTime = 0f;
        Vector2 startingPos = rectTransform.anchoredPosition;

        while (elapsedTime < animationSpeed)
        {
            rectTransform.anchoredPosition =
                Vector2.Lerp(startingPos, targetPosition, elapsedTime / animationSpeed);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        animationCoroutine = null;
    }

    public bool IsExpanded()
    {
        return Mathf.Approximately(rectTransform.anchoredPosition.y, expandedPosition.y);
    }
}
