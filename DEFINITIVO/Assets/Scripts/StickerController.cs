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

    // Guardar posição/parent originais (no menu/base)
    private Transform originalParent;
    private Vector3 originalLocalPosition;

    // Pinch-to-zoom
    private float initialDistance;
    private Vector3 initialScale;
    private static bool isAnyPinching = false;
    private const float MIN_SCALE = 0.5f;
    private const float MAX_SCALE = 3f;
    private bool isBeingDragged = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        initialScale = rectTransform.localScale;

        // Salva o pai e posição originais dentro da base/scroll
        originalParent = rectTransform.parent;
        originalLocalPosition = rectTransform.localPosition;

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

        // Mover para frente da RawImage
        rectTransform.SetParent(rawImageRect.parent, true);
        rectTransform.SetAsLastSibling();

        // Pinch-to-zoom
        if (Input.touchCount >= 2 && !isAnyPinching)
        {
            StartPinch();
            isAnyPinching = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isAnyPinching) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImageRect,
            eventData.position,
            canvas.worldCamera,
            out localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isBeingDragged = false;

        if (isAnyPinching && Input.touchCount < 2)
            isAnyPinching = false;

        if (!positionInitialized) return;

        // Se não soltar dentro da RawImage, volta pro menu
        if (!RectTransformUtility.RectangleContainsScreenPoint(rawImageRect, rectTransform.position, canvas.worldCamera))
        {
            rectTransform.SetParent(originalParent, false); // false mantém o alinhamento do Layout Group
            rectTransform.localScale = initialScale;
        }
    }

    void Update()
    {
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
            float currentDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
            if (initialDistance == 0) { initialDistance = currentDistance; return; }

            float scaleFactor = currentDistance / initialDistance;
            Vector3 newScale = initialScale * scaleFactor;
            newScale.x = Mathf.Clamp(newScale.x, MIN_SCALE, MAX_SCALE);
            newScale.y = Mathf.Clamp(newScale.y, MIN_SCALE, MAX_SCALE);
            newScale.z = 1f;

            rectTransform.localScale = newScale;
        }
        else if (isAnyPinching)
        {
            isAnyPinching = false;
        }
    }
}
