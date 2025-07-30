using UnityEngine;
using UnityEngine.EventSystems;

public class PinchZoomCentered : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 0.5f;
    public float maxZoom = 3f;
    public float zoomLerpSpeed = 10f;
    public float zoomSensitivity = 0.001f;

    [Header("Movement Settings")]
    public float recenterSpeed = 5f;
    public float edgePadding = 50f; // Margem para não cortar o mapa

    private RectTransform mapRect;
    private Vector3 targetScale;
    private Vector3 targetPosition;
    private Vector2 pinchCenter;
    private bool isPinching;
    private Vector2 lastPinchPosition;
    private Vector2 mapSize;
    private Vector2 screenSize;

    void Awake()
    {
        mapRect = GetComponent<RectTransform>();
        targetScale = mapRect.localScale;
        targetPosition = mapRect.localPosition;
        mapSize = mapRect.rect.size;
        screenSize = new Vector2(Screen.width, Screen.height);
    }

    void Update()
    {
        HandlePinchZoom();
        ApplyTransformations();
        AutoRecenter();
    }

    void HandlePinchZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch2.phase == TouchPhase.Began)
            {
                isPinching = true;
                pinchCenter = (touch1.position + touch2.position) / 2f;
                lastPinchPosition = pinchCenter;
            }
            else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                Vector2 currentPinchCenter = (touch1.position + touch2.position) / 2f;

                // Calcula diferença de distância entre dedos
                float currentDistance = Vector2.Distance(touch1.position, touch2.position);
                float previousDistance = Vector2.Distance(
                    touch1.position - touch1.deltaPosition,
                    touch2.position - touch2.deltaPosition);

                float pinchAmount = (currentDistance - previousDistance) * zoomSensitivity;

                // Aplica zoom mantendo foco no ponto do pinch
                ZoomAtPoint(pinchCenter, pinchAmount);

                // Move o mapa para acompanhar o movimento dos dedos
                Vector2 pinchDelta = currentPinchCenter - lastPinchPosition;
                targetPosition += (Vector3)(pinchDelta * (1f / targetScale.x));

                lastPinchPosition = currentPinchCenter;
            }
            else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
            {
                isPinching = false;
            }
        }
    }

    void ZoomAtPoint(Vector2 screenPoint, float zoomDelta)
    {
        // Converte ponto da tela para espaço local do mapa
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRect, screenPoint, null, out localPoint);

        // Calcula novo scale com limites
        Vector3 newScale = targetScale + Vector3.one * zoomDelta;
        newScale = Vector3.Max(Vector3.one * minZoom, Vector3.Min(Vector3.one * maxZoom, newScale));

        // Calcula diferença de scale
        float scaleFactor = newScale.x / targetScale.x;

        // Ajusta posição para zoom no ponto correto
        targetPosition += (Vector3)(localPoint * (scaleFactor - 1f));

        targetScale = newScale;
    }

    void ApplyTransformations()
    {
        // Aplica com suavização
        mapRect.localScale = Vector3.Lerp(mapRect.localScale, targetScale, Time.deltaTime * zoomLerpSpeed);
        mapRect.localPosition = Vector3.Lerp(mapRect.localPosition, targetPosition, Time.deltaTime * zoomLerpSpeed);
    }

    void AutoRecenter()
    {
        if (!isPinching && Input.touchCount == 0)
        {
            // Calcula limites visíveis
            float visibleWidth = mapSize.x * targetScale.x;
            float visibleHeight = mapSize.y * targetScale.y;

            // Se o mapa estiver menor que a tela, centraliza
            if (visibleWidth <= screenSize.x + edgePadding &&
                visibleHeight <= screenSize.y + edgePadding)
            {
                targetPosition = Vector3.Lerp(
                    targetPosition,
                    Vector3.zero,
                    Time.deltaTime * recenterSpeed);
            }
            else
            {
                // Mantém dentro dos limites
                float maxOffsetX = Mathf.Max(0, (visibleWidth - screenSize.x) / 2f);
                float maxOffsetY = Mathf.Max(0, (visibleHeight - screenSize.y) / 2f);

                targetPosition.x = Mathf.Clamp(targetPosition.x, -maxOffsetX, maxOffsetX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, -maxOffsetY, maxOffsetY);
            }
        }
    }
}