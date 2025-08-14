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

    // Controle de hierarquia
    private Transform originalParent;
    private Transform dragParent; // Parent temporário para aparecer sobre a RawImage

    // Variáveis para pinch-to-zoom
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
        dragParent = rawImageRectTransform.parent; // Usamos o mesmo parent da RawImage
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isBeingDragged = true;

        // Guarda parent original e move para o parent da RawImage
        originalParent = transform.parent;
        transform.SetParent(dragParent, true);

        if (Input.touchCount >= 2 && !isAnyPinching)
        {
            StartPinch();
            isAnyPinching = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isAnyPinching) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isBeingDragged = false;

        if (isAnyPinching && Input.touchCount < 2)
        {
            isAnyPinching = false;
        }

        if (!positionInitialized) return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rawImageRect, rectTransform.position, null))
        {
            // Fora da área -> volta para posição original e parent original
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = initialPosition;
            rectTransform.localScale = initialScale;
        }
        else
        {
            // Dentro da área -> mantém no novo parent
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

            if (initialDistance == 0)
            {
                initialDistance = currentDistance;
                return;
            }

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
