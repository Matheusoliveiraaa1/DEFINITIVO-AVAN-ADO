using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class StickerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector]
    public string AreaName;

    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform rawImageRect;
    private Vector2 initialPosition;
    private bool positionInitialized = false;

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalRotation;

    private float initialDistance;
    private float initialAngle;
    private Vector3 initialScale; // Escala original (spawn)
    private Vector3 pinchInitialScale; // Escala no início do pinch
    private static bool isAnyPinching = false;
    private const float MIN_SCALE = 0.5f;
    private const float MAX_SCALE = 3f;
    private bool isBeingDragged = false;
    private bool isOverPhotoArea = false;
    private bool rotationInitialized = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        initialScale = rectTransform.localScale; // Tamanho original
        originalRotation = rectTransform.localRotation;

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
        isOverPhotoArea = RectTransformUtility.RectangleContainsScreenPoint(rawImageRect, eventData.position, canvas.worldCamera);

        rectTransform.SetParent(rawImageRect.parent, true);
        rectTransform.SetAsLastSibling();

        if (Input.touchCount >= 2 && !isAnyPinching)
        {
            StartPinchAndRotate();
            isAnyPinching = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isAnyPinching) return;

        isOverPhotoArea = RectTransformUtility.RectangleContainsScreenPoint(rawImageRect, eventData.position, canvas.worldCamera);

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImageRect, eventData.position, canvas.worldCamera, out localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isBeingDragged = false;

        if (isAnyPinching && Input.touchCount < 2)
        {
            isAnyPinching = false;
            rotationInitialized = false;
        }

        if (!positionInitialized)
            return;

        // Verifica se o sticker está dentro da área da foto pelo centro
        bool isInsideRawImage = IsRectTransformInside(rawImageRect, rectTransform);

        if (isInsideRawImage)
        {
            NativeCameraExample cameraExample = FindObjectOfType<NativeCameraExample>();
            if (cameraExample != null)
            {
                if (!cameraExample.IsStickerAlreadyRegistered(this))
                {
                    if (!cameraExample.CanAddSticker())
                    {
                        ReturnToBase();
                        cameraExample.ShowErrorMessage("Limite de stickers por foto excedido!");
                        return;
                    }

                    cameraExample.RegisterSticker(this);
                }
            }
        }
        else
        {
            ReturnToBase();
        }
    }

    private bool IsRectTransformInside(RectTransform container, RectTransform target)
    {
        Vector3[] containerCorners = new Vector3[4];
        container.GetWorldCorners(containerCorners);
        Rect containerRect = new Rect(containerCorners[0], containerCorners[2] - containerCorners[0]);

        // Verifica se o centro do sticker está dentro
        Vector3 center = target.position;
        return containerRect.Contains(center);
    }

    private void ReturnToBase()
    {
        rectTransform.SetParent(originalParent, false);
        rectTransform.localScale = initialScale; // Volta ao tamanho original
        rectTransform.localRotation = originalRotation;

        NativeCameraExample cameraExample = FindObjectOfType<NativeCameraExample>();
        if (cameraExample != null)
        {
            cameraExample.UnregisterSticker(this);
        }
    }

    void Update()
    {
        if (isBeingDragged)
        {
            HandleMultiTouch();
        }
    }

    private void StartPinchAndRotate()
    {
        if (Input.touchCount >= 2)
        {
            initialDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
            pinchInitialScale = rectTransform.localScale; // Usa uma variável temporária para o pinch

            Vector2 touch0Pos = Input.GetTouch(0).position;
            Vector2 touch1Pos = Input.GetTouch(1).position;
            initialAngle = Mathf.Atan2(touch1Pos.y - touch0Pos.y, touch1Pos.x - touch0Pos.x) * Mathf.Rad2Deg;

            originalRotation = rectTransform.localRotation;
            rotationInitialized = true;
        }
    }

    private void HandleMultiTouch()
    {
        if (Input.touchCount >= 2)
        {
            if (!rotationInitialized)
            {
                StartPinchAndRotate();
                return;
            }

            float currentDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
            if (initialDistance == 0)
            {
                initialDistance = currentDistance;
                return;
            }

            float scaleFactor = currentDistance / initialDistance;
            Vector3 newScale = pinchInitialScale * scaleFactor; // Usa a escala do início do pinch
            newScale.x = Mathf.Clamp(newScale.x, MIN_SCALE, MAX_SCALE);
            newScale.y = Mathf.Clamp(newScale.y, MIN_SCALE, MAX_SCALE);
            newScale.z = 1f;
            rectTransform.localScale = newScale;

            // Rotação (twist)
            Vector2 currentTouch0Pos = Input.GetTouch(0).position;
            Vector2 currentTouch1Pos = Input.GetTouch(1).position;
            Vector2 previousTouch0Pos = Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition;
            Vector2 previousTouch1Pos = Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition;

            Vector2 currentVector = currentTouch1Pos - currentTouch0Pos;
            Vector2 previousVector = previousTouch1Pos - previousTouch0Pos;

            float angle = Vector2.SignedAngle(previousVector, currentVector);
            rectTransform.Rotate(0, 0, angle, Space.Self);
        }
        else if (isAnyPinching)
        {
            isAnyPinching = false;
            rotationInitialized = false;
        }
    }
}