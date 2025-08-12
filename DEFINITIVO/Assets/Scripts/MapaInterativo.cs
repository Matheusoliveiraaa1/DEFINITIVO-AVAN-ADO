using UnityEngine;
using UnityEngine.UI;

public class MapTouchController : MonoBehaviour
{
    public float minZoom = 1f;
    public float maxZoom = 4f;
    public float zoomSmoothness = 5f;
    public float moveSmoothness = 8f;
    public float rotationSmoothness = 10f;
    public float minMoveThreshold = 5f; // Threshold mínimo de movimento em pixels

    public RectTransform mapRectTransform;
    public RectTransform containerRectTransform;

    private float initialDistance;
    private Vector3 initialScale;
    private float initialRotation;
    private Quaternion initialRotationQuat;
    private Vector2 initialMidPoint;
    private bool isPinching = false;

    private Vector3 targetScale;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        targetScale = mapRectTransform.localScale;
        targetPosition = mapRectTransform.localPosition;
        targetRotation = mapRectTransform.rotation;
    }

    void Update()
    {
        HandlePinchGesture();
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
                // Calcula o zoom target
                float scaleFactor = currentDistance / initialDistance;
                Vector3 newTargetScale = initialScale * scaleFactor;

                newTargetScale.x = Mathf.Clamp(newTargetScale.x, minZoom, maxZoom);
                newTargetScale.y = Mathf.Clamp(newTargetScale.y, minZoom, maxZoom);
                newTargetScale.z = 1;

                // Calcula a posição target considerando o zoom
                Vector2 localPointBeforeZoom;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    containerRectTransform,
                    initialMidPoint,
                    null,
                    out localPointBeforeZoom);

                Vector2 localPointAfterZoom;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    containerRectTransform,
                    initialMidPoint,
                    null,
                    out localPointAfterZoom);

                Vector2 positionDelta = localPointAfterZoom - localPointBeforeZoom;
                Vector3 positionAdjustment = new Vector3(
                    positionDelta.x * newTargetScale.x,
                    positionDelta.y * newTargetScale.y,
                    0);

                // Calcula rotação target
                float currentRotation = Vector2.SignedAngle(t1.position - t0.position, Vector2.right);
                float deltaRotation = initialRotation - currentRotation;
                Quaternion newTargetRotation = initialRotationQuat * Quaternion.Euler(0, 0, deltaRotation);

                // Calcula o movimento dos dedos
                Vector2 midDelta = currentMidPoint - initialMidPoint;

                // Só aplica o pan se o movimento for maior que o threshold
                Vector3 panMovement = Vector3.zero;
                if (midDelta.magnitude > minMoveThreshold)
                {
                    panMovement = new Vector3(midDelta.x, midDelta.y, 0);
                }

                Vector3 newTargetPosition = mapRectTransform.localPosition - positionAdjustment + panMovement;

                // Aplica limites ao pan
                Vector2 containerSize = containerRectTransform.rect.size;
                Vector2 mapSize = mapRectTransform.rect.size;
                Vector2 scaledMapSize = new Vector2(mapSize.x * newTargetScale.x, mapSize.y * newTargetScale.y);

                Vector2 maxOffset = (scaledMapSize - containerSize) / 2f;
                maxOffset.x = Mathf.Max(0, maxOffset.x);
                maxOffset.y = Mathf.Max(0, maxOffset.y) * 1.5f;

                newTargetPosition.x = Mathf.Clamp(newTargetPosition.x, -maxOffset.x, maxOffset.x);
                newTargetPosition.y = Mathf.Clamp(newTargetPosition.y, -maxOffset.y, maxOffset.y);

                // Atualiza os targets
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
            mapRectTransform.localScale,
            targetScale,
            Time.deltaTime * zoomSmoothness);

        mapRectTransform.localPosition = Vector3.Lerp(
            mapRectTransform.localPosition,
            targetPosition,
            Time.deltaTime * moveSmoothness);

        mapRectTransform.rotation = Quaternion.Slerp(
            mapRectTransform.rotation,
            targetRotation,
            Time.deltaTime * rotationSmoothness);
    }
}