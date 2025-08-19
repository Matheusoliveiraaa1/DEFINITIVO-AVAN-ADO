using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class StickerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler


{

    [HideInInspector]
    public string AreaName; // NOVO: guarda a área de origem do sticker


    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform rawImageRect;
    private Vector2 initialPosition;
    private bool positionInitialized = false;

    // Guardar posição/parent originais (no menu/base)
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalRotation;

    // Pinch-to-zoom e rotação
    private float initialDistance;
    private float initialAngle;
    private Vector3 initialScale;
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
        initialScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;

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
        isOverPhotoArea = RectTransformUtility.RectangleContainsScreenPoint(rawImageRect, eventData.position, canvas.worldCamera);

        // Mover para frente da RawImage
        rectTransform.SetParent(rawImageRect.parent, true);
        rectTransform.SetAsLastSibling();

        // Inicia pinch/rotate se tiver dois dedos
        if (Input.touchCount >= 2 && !isAnyPinching)
        {
            StartPinchAndRotate();
            isAnyPinching = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isAnyPinching) return;

        // Atualiza se está sobre a área da foto
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

        // 🔹 Checa se o sticker está dentro da área da foto pelos cantos
        bool isInsideRawImage = IsRectTransformInside(rawImageRect, rectTransform);

        if (isInsideRawImage)
        {
            NativeCameraExample cameraExample = FindObjectOfType<NativeCameraExample>();
            if (cameraExample != null)
            {
                // Se já está registrado, não faz a checagem de limite de novo
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
        Vector3[] targetCorners = new Vector3[4];
        container.GetWorldCorners(containerCorners);
        target.GetWorldCorners(targetCorners);

        Rect containerRect = new Rect(containerCorners[0], containerCorners[2] - containerCorners[0]);

        // Se qualquer canto do target estiver dentro, já consideramos válido
        foreach (var corner in targetCorners)
        {
            if (containerRect.Contains(corner))
                return true;
        }
        return false;
    }





    private void ReturnToBase()
    {
        rectTransform.SetParent(originalParent, false);
        rectTransform.localScale = initialScale;
        rectTransform.localRotation = originalRotation;

        // Remove da lista de ativos se estiver lá
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
            initialScale = rectTransform.localScale;

            // Calcula o ângulo inicial entre os dois dedos
            Vector2 touch0Pos = Input.GetTouch(0).position;
            Vector2 touch1Pos = Input.GetTouch(1).position;
            initialAngle = Mathf.Atan2(touch1Pos.y - touch0Pos.y, touch1Pos.x - touch0Pos.x) * Mathf.Rad2Deg;

            // Usa a rotação atual como base
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

            // Escala (pinch)
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

            // Rotação (twist) - cálculo mais estável
            Vector2 currentTouch0Pos = Input.GetTouch(0).position;
            Vector2 currentTouch1Pos = Input.GetTouch(1).position;
            Vector2 previousTouch0Pos = Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition;
            Vector2 previousTouch1Pos = Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition;

            // Vetores entre os dedos
            Vector2 currentVector = currentTouch1Pos - currentTouch0Pos;
            Vector2 previousVector = previousTouch1Pos - previousTouch0Pos;

            // Ângulo entre os vetores
            float angle = Vector2.SignedAngle(previousVector, currentVector);

            // Aplica a rotação incremental
            rectTransform.Rotate(0, 0, angle, Space.Self);
        }
        else if (isAnyPinching)
        {
            isAnyPinching = false;
            rotationInitialized = false;
        }
    }
}