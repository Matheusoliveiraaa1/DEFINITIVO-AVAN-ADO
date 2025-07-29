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

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        StartCoroutine(SetInitialPositionNextFrame());
    }

    IEnumerator SetInitialPositionNextFrame()
    {
        yield return null; // Espera o pr�ximo frame (ap�s o LayoutGroup atualizar)
        initialPosition = rectTransform.anchoredPosition;
        positionInitialized = true;
    }

    public void SetRawImageRect(RectTransform rawImageRectTransform)
    {
        rawImageRect = rawImageRectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Opcional: efeito ao come�ar a arrastar
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!positionInitialized) return;

        // Verifica se o sticker est� dentro da RawImage
        if (!RectTransformUtility.RectangleContainsScreenPoint(rawImageRect, rectTransform.position, null))
        {
            // Volta para a posi��o inicial (agora correta)
            rectTransform.anchoredPosition = initialPosition;
        }
    }
}