using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class StickerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform rawImageRect;
    private Vector2 initialPosition;
    private bool positionInitialized = false;

    // Variáveis para pinch-to-zoom
    private float initialDistance;
    private Vector3 initialScale;
    private static bool isAnyPinching = false; // Agora estático para controlar todos os stickers
    private const float MIN_SCALE = 0.5f;
    private const float MAX_SCALE = 3f;
    private bool isBeingDragged = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        initialScale = rectTransform.localScale; // Guarda a escala inicial
        StartCoroutine(SetInitialPositionNextFrame());
    }

    IEnumerator SetInitialPositionNextFrame()
    {
        yield return null;
        initialPosition = rectTransform.anchoredPosition;
        positionInitialized = true;
    }

    public void SetRawImageRect(RectTransform rawImageRectTransform)
    {
        rawImageRect = rawImageRectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isBeingDragged = true;

        // Se for multi-toque, começa o pinch-to-zoom
        if (Input.touchCount >= 2 && !isAnyPinching)
        {
            StartPinch();
            isAnyPinching = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Se estiver fazendo pinch, não arrasta
        if (isAnyPinching) return;

        // Arrasta normalmente
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isBeingDragged = false;

        // Só libera o pinch se este era o sticker sendo pinched
        if (isAnyPinching && Input.touchCount < 2)
        {
            isAnyPinching = false;
        }

        if (!positionInitialized) return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rawImageRect, rectTransform.position, null))
        {
            rectTransform.anchoredPosition = initialPosition;
            rectTransform.localScale = initialScale; // Reseta a escala também
        }
    }

    void Update()
    {
        // Só processa pinch se este for o sticker sendo arrastado
        if (isBeingDragged)
        {
            HandlePinchZoom();
        }
    }

    private void StartPinch()
    {
        if (Input.touchCount >= 2)
        {
            initialDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
            initialScale = rectTransform.localScale;
        }
    }

    private void HandlePinchZoom()
    {
        if (Input.touchCount >= 2)
        {
            // Calcula a nova distância entre os dedos
            float currentDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);

            // Se a distância inicial for zero, ajusta
            if (initialDistance == 0)
            {
                initialDistance = currentDistance;
                return;
            }

            // Calcula o fator de escala
            float scaleFactor = currentDistance / initialDistance;
            Vector3 newScale = initialScale * scaleFactor;

            // Limita o tamanho mínimo e máximo
            newScale.x = Mathf.Clamp(newScale.x, MIN_SCALE, MAX_SCALE);
            newScale.y = Mathf.Clamp(newScale.y, MIN_SCALE, MAX_SCALE);
            newScale.z = 1f; // Mantém a profundidade

            // Aplica a escala apenas a este sticker
            rectTransform.localScale = newScale;
        }
        else if (isAnyPinching)
        {
            isAnyPinching = false;
        }
    }
}