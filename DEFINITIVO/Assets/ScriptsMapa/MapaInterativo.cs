using UnityEngine;
using UnityEngine.UI;

public class MapTouchController : MonoBehaviour
{
    [Header("Zoom & Pan Settings")]
    public float minZoom = 1f;
    public float maxZoom = 4f;
    public float zoomSmoothness = 5f;
    public float moveSmoothness = 8f;
    public float rotationSmoothness = 10f;
    public float minMoveThreshold = 5f;

    [Header("UI References")]
    public RectTransform mapRectTransform;
    public RectTransform containerRectTransform;
    public GameObject mapPanel;          // Painel do mapa principal
    public GameObject areaInfoPanel;     // Painel de informações da área
    public GameObject stickerPanel;      // Painel dos stickers

    private float initialDistance;
    private Vector3 initialScale;
    private float initialRotation;
    private Quaternion initialRotationQuat;
    private Vector2 initialMidPoint;
    private bool isPinching = false;

    private Vector3 targetScale;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private Vector2 lastTouchPosition;
    private bool isDragging = false;

    void Start()
    {
        targetScale = mapRectTransform.localScale;
        targetPosition = mapRectTransform.localPosition;
        targetRotation = mapRectTransform.rotation;
    }

    void Update()
    {
        HandlePinchGesture();
        HandleSingleFingerDrag();
        ApplySmoothTransitions();
    }

    private void HandlePinchGesture()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 currentMidPoint = (t0.position + t1.position) / 2f;
            float currentDistance = Vector2.Distance(t0.position, t1.position);

            if (!isPinching)
            {
                initialDistance = currentDistance;
                initialScale = mapRectTransform.localScale;
                initialRotation = Vector2.SignedAngle(t1.position - t0.position, Vector2.right);
                initialRotationQuat = mapRectTransform.rotation;
                initialMidPoint = currentMidPoint;
                isPinching = true;

                targetScale = initialScale;
                targetPosition = mapRectTransform.localPosition;
                targetRotation = initialRotationQuat;
            }
            else
            {
                float scaleFactor = currentDistance / initialDistance;
                Vector3 newTargetScale = initialScale * scaleFactor;

                newTargetScale.x = Mathf.Clamp(newTargetScale.x, minZoom, maxZoom);
                newTargetScale.y = Mathf.Clamp(newTargetScale.y, minZoom, maxZoom);
                newTargetScale.z = 1;

                Vector2 localPointBeforeZoom;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    containerRectTransform, initialMidPoint, null, out localPointBeforeZoom);

                Vector2 localPointAfterZoom;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    containerRectTransform, initialMidPoint, null, out localPointAfterZoom);

                Vector2 positionDelta = localPointAfterZoom - localPointBeforeZoom;
                Vector3 positionAdjustment = new Vector3(
                    positionDelta.x * newTargetScale.x,
                    positionDelta.y * newTargetScale.y,
                    0);

                float currentRotation = Vector2.SignedAngle(t1.position - t0.position, Vector2.right);
                float deltaRotation = initialRotation - currentRotation;
                Quaternion newTargetRotation = initialRotationQuat * Quaternion.Euler(0, 0, deltaRotation);

                Vector2 midDelta = currentMidPoint - initialMidPoint;

                Vector3 panMovement = Vector3.zero;
                if (midDelta.magnitude > minMoveThreshold)
                    panMovement = new Vector3(midDelta.x, midDelta.y, 0);

                Vector3 newTargetPosition = mapRectTransform.localPosition - positionAdjustment + panMovement;

                Vector2 containerSize = containerRectTransform.rect.size;
                Vector2 mapSize = mapRectTransform.rect.size;
                Vector2 scaledMapSize = new Vector2(mapSize.x * newTargetScale.x, mapSize.y * newTargetScale.y);

                Vector2 maxOffset = (scaledMapSize - containerSize) / 2f;
                maxOffset.x = Mathf.Max(0, maxOffset.x);
                maxOffset.y = Mathf.Max(0, maxOffset.y) * 1.5f;

                newTargetPosition.x = Mathf.Clamp(newTargetPosition.x, -maxOffset.x, maxOffset.x);
                newTargetPosition.y = Mathf.Clamp(newTargetPosition.y, -maxOffset.y, maxOffset.y);

                targetScale = newTargetScale;
                targetPosition = newTargetPosition;
                targetRotation = newTargetRotation;
            }
        }
        else
        {
            isPinching = false;
        }
    }

    private void ApplySmoothTransitions()
    {
        mapRectTransform.localScale = Vector3.Lerp(
            mapRectTransform.localScale, targetScale, Time.deltaTime * zoomSmoothness);

        mapRectTransform.localPosition = Vector3.Lerp(
            mapRectTransform.localPosition, targetPosition, Time.deltaTime * moveSmoothness);

        mapRectTransform.rotation = Quaternion.Slerp(
            mapRectTransform.rotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    // -----------------------------
    // FUNÇÕES DOS BOTÕES DE VOLTAR
    // -----------------------------

    // Fecha o painel de stickers e volta pro painel da área
    public void BackToAreaInfoPanel()
    {
        stickerPanel.SetActive(false);
        areaInfoPanel.SetActive(true);
    }

    // Fecha o painel da área e volta pro mapa principal
    public void BackToMap()
    {
        areaInfoPanel.SetActive(false);
        mapPanel.SetActive(true);
    }





    private void HandleSingleFingerDrag()
    {
        if (Input.touchCount == 1 && !isPinching)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 delta = touch.position - lastTouchPosition;

                Vector3 newTargetPosition = targetPosition + new Vector3(delta.x, delta.y, 0);

                // ----- LIMITES DO MAPA -----
                Vector2 containerSize = containerRectTransform.rect.size;
                Vector2 mapSize = mapRectTransform.rect.size;

                Vector2 scaledMapSize = new Vector2(
                    mapSize.x * targetScale.x,
                    mapSize.y * targetScale.y
                );

                Vector2 maxOffset = (scaledMapSize - containerSize) / 2f;

                maxOffset.x = Mathf.Max(0, maxOffset.x);
                maxOffset.y = Mathf.Max(0, maxOffset.y) * 1.5f;

                newTargetPosition.x = Mathf.Clamp(newTargetPosition.x, -maxOffset.x, maxOffset.x);
                newTargetPosition.y = Mathf.Clamp(newTargetPosition.y, -maxOffset.y, maxOffset.y);

                targetPosition = newTargetPosition;

                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
    }
}










