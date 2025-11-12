using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DraggablePanel : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("Configurações")]
    public float swipeThreshold = 50f; // Distância mínima para um 'swipe' contar como abertura/fechamento
    public float animationSpeed = 0.3f; // Velocidade da animação de recolhimento/abertura
    public Vector2 initialPosition; // Posição inicial (recolhida)
    public Vector2 expandedPosition; // Posição totalmente aberta

    private RectTransform rectTransform;
    private Coroutine animationCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // Define a posição inicial na primeira vez que o componente é carregado
        initialPosition = rectTransform.anchoredPosition;
    }

    // Chamado a cada frame durante o arrasto
    public void OnDrag(PointerEventData eventData)
    {
        // Interrompe qualquer animação em andamento
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        // Move o painel verticalmente. EventData.delta é o quanto o mouse/toque moveu desde o último frame.
        rectTransform.anchoredPosition += new Vector2(0, eventData.delta.y);

        // Limita o painel entre as duas posições para não arrastar para fora da tela
        float clampedY = Mathf.Clamp(
            rectTransform.anchoredPosition.y,
            initialPosition.y,
            expandedPosition.y
        );

        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, clampedY);
    }

    // Chamado quando o usuário solta o mouse/toque
    public void OnEndDrag(PointerEventData eventData)
    {
        // 1. Verifica se houve um 'swipe' rápido o suficiente para cima (abrir) ou para baixo (fechar)
        float dragDistance = rectTransform.anchoredPosition.y - initialPosition.y;

        // Se o arrasto para baixo foi grande (ou se o arrasto foi para baixo com velocidade suficiente)
        if (eventData.delta.y < -swipeThreshold || dragDistance < (expandedPosition.y - initialPosition.y) / 2)
        {
            // Recolher o painel para a posição inicial
            animationCoroutine = StartCoroutine(AnimateToPosition(initialPosition));
        }
        // Se o arrasto para cima foi grande
        else
        {
            // Abrir o painel para a posição expandida
            animationCoroutine = StartCoroutine(AnimateToPosition(expandedPosition));
        }
    }

    // Coroutine para animar a posição de forma suave
    private IEnumerator AnimateToPosition(Vector2 targetPosition)
    {
        float elapsedTime = 0f;
        Vector2 startingPos = rectTransform.anchoredPosition;

        while (elapsedTime < animationSpeed)
        {
            // Usa Lerp para uma transição suave
            rectTransform.anchoredPosition = Vector2.Lerp(startingPos, targetPosition, (elapsedTime / animationSpeed));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        animationCoroutine = null;
    }
}