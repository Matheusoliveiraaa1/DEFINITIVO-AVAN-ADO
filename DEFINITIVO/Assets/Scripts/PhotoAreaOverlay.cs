using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PhotoAreaOverlay : MonoBehaviour
{
    public static PhotoAreaOverlay Instance;

    [Header("Shared")]
    public GameObject overlayPanel;
    public Button okButton;

    [Header("UI Elements - Área")]
    public Image photo1;
    public Image photo2;
    public Image photo3;

    [Header("UI Elements - Sticker")]
    public Image stickerMain;
    public Image stickerExtra1;
    public Image stickerExtra2;

    [Header("Animation")]
    [Tooltip("Tempo da animação de crescimento em segundos")]
    public float stickerPopDuration = 2.0f;
    [Tooltip("Escala extra no meio da animação para dar 'pop'")]
    public float stickerPopScale = 1.15f;
    [Tooltip("Ângulo máximo de rotação para o efeito de balanço")]
    public float stickerWobbleAngle = 5f;
    [Tooltip("Velocidade do balanço")]
    public float stickerWobbleSpeed = 2f;
    [Tooltip("Amplitude do leve pulso após crescimento")]
    public float stickerPulseScale = 0.05f;
    [Tooltip("Velocidade do pulso")]
    public float stickerPulseSpeed = 2f;

    [Header("Fly to Backpack Animation")]
    public Transform backpackIconTarget;
    public float shrinkDuration = 0.5f;
    public float flyDuration = 0.8f;
    public float minScale = 0.2f;

    private bool showingSticker = false;
    private Vector3 stickerOriginalScale;
    private Coroutine wobbleCoroutine;
    private bool isAnimatingToBackpack = false;

    private void Awake()
    {
        Instance = this;

        overlayPanel.SetActive(false);
        okButton.onClick.AddListener(OnOkButtonClick);

        if (stickerMain != null)
            stickerOriginalScale = stickerMain.rectTransform.localScale;
    }

    private void OnOkButtonClick()
    {
        if (showingSticker && !isAnimatingToBackpack)
        {
            if (stickerExtra1 != null) stickerExtra1.gameObject.SetActive(false);
            if (stickerExtra2 != null) stickerExtra2.gameObject.SetActive(false);

            StartCoroutine(AnimateStickerToBackpack());
        }
        else
        {
            Hide();
        }
    }

    private IEnumerator AnimateStickerToBackpack()
    {
        isAnimatingToBackpack = true;

        if (stickerMain == null || backpackIconTarget == null)
        {
            Hide(false);
            yield break;
        }

        RectTransform stickerRT = stickerMain.rectTransform;
        Vector3 originalScale = stickerRT.localScale;

        // Fase 1: Encolher o sticker
        float shrinkTime = 0f;
        while (shrinkTime < shrinkDuration)
        {
            shrinkTime += Time.deltaTime;
            float progress = shrinkTime / shrinkDuration;
            float scale = Mathf.Lerp(1f, minScale, progress);
            stickerRT.localScale = originalScale * scale;
            yield return null;
        }

        // Fase 2: Voar até a mochila (mantém tamanho reduzido)
        Vector3 startPosition = stickerRT.position;
        Vector3 targetPosition = backpackIconTarget.position;

        float flyTime = 0f;
        while (flyTime < flyDuration)
        {
            flyTime += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, flyTime / flyDuration);

            stickerRT.position = Vector3.Lerp(startPosition, targetPosition, progress);
            stickerRT.localScale = originalScale * minScale; // mantém fixo
            yield return null;
        }

        // Some no final
        stickerRT.position = targetPosition;
        stickerRT.localScale = Vector3.zero;

        overlayPanel.SetActive(false);
        stickerMain.gameObject.SetActive(false);

        Hide(false);
        isAnimatingToBackpack = false;
    }

    // --- MODO ÁREA ---
    public static void Show(Sprite img1 = null, Sprite img2 = null, Sprite img3 = null)
    {
        if (Instance == null) return;

        Instance.showingSticker = false;
        Instance.overlayPanel.SetActive(true);

        Instance.photo1.gameObject.SetActive(true);
        Instance.photo2.gameObject.SetActive(true);
        Instance.photo3.gameObject.SetActive(true);

        if (Instance.stickerMain) Instance.stickerMain.gameObject.SetActive(false);
        if (Instance.stickerExtra1) Instance.stickerExtra1.gameObject.SetActive(false);
        if (Instance.stickerExtra2) Instance.stickerExtra2.gameObject.SetActive(false);

        if (Instance.wobbleCoroutine != null)
        {
            Instance.StopCoroutine(Instance.wobbleCoroutine);
            Instance.wobbleCoroutine = null;
        }

        if (img1 != null) Instance.photo1.sprite = img1;
        if (img2 != null) Instance.photo2.sprite = img2;
        if (img3 != null) Instance.photo3.sprite = img3;
    }

    // --- MODO STICKER ---
    public static void ShowSticker(Sprite mainSprite, Sprite overrideExtra1 = null, Sprite overrideExtra2 = null)
    {
        if (Instance == null) return;

        Instance.showingSticker = true;
        Instance.overlayPanel.SetActive(true);
        Instance.isAnimatingToBackpack = false;

        Instance.photo1.gameObject.SetActive(false);
        Instance.photo2.gameObject.SetActive(false);
        Instance.photo3.gameObject.SetActive(false);

        if (Instance.stickerMain != null)
        {
            Instance.stickerMain.sprite = mainSprite;
            Instance.stickerMain.gameObject.SetActive(true);
            Instance.stickerMain.rectTransform.localScale = Instance.stickerOriginalScale;
            Instance.stickerMain.rectTransform.localRotation = Quaternion.identity;

            if (Instance.wobbleCoroutine != null)
                Instance.StopCoroutine(Instance.wobbleCoroutine);

            Instance.StartCoroutine(Instance.AnimateStickerPop());
        }

        if (Instance.stickerExtra1 != null)
        {
            if (overrideExtra1 != null) Instance.stickerExtra1.sprite = overrideExtra1;
            bool hasSprite = Instance.stickerExtra1.sprite != null;
            Instance.stickerExtra1.gameObject.SetActive(hasSprite);
        }

        if (Instance.stickerExtra2 != null)
        {
            if (overrideExtra2 != null) Instance.stickerExtra2.sprite = overrideExtra2;
            bool hasSprite = Instance.stickerExtra2.sprite != null;
            Instance.stickerExtra2.gameObject.SetActive(hasSprite);
        }
    }

    private IEnumerator AnimateStickerPop()
    {
        RectTransform rt = stickerMain.rectTransform;

        rt.localScale = Vector3.zero;
        rt.localRotation = Quaternion.identity;

        float t = 0;
        while (t < stickerPopDuration)
        {
            t += Time.deltaTime;
            float progress = t / stickerPopDuration;

            float scale = 1f - Mathf.Pow(1f - progress, 2f);
            float popBoost = (progress < 0.8f) ? Mathf.Lerp(stickerPopScale, 1f, progress / 0.8f) : 1f;

            rt.localScale = stickerOriginalScale * scale * popBoost;
            yield return null;
        }

        rt.localScale = stickerOriginalScale;

        wobbleCoroutine = StartCoroutine(AnimateStickerWobbleAndPulse(rt));
    }

    private IEnumerator AnimateStickerWobbleAndPulse(RectTransform rt)
    {
        float time = 0;
        while (overlayPanel.activeSelf && showingSticker && !isAnimatingToBackpack)
        {
            time += Time.deltaTime * stickerWobbleSpeed;
            float angle = Mathf.Sin(time) * stickerWobbleAngle;
            float pulse = 1f + Mathf.Sin(Time.time * stickerPulseSpeed) * stickerPulseScale;

            rt.localRotation = Quaternion.Euler(0, 0, angle);
            rt.localScale = stickerOriginalScale * pulse;
            yield return null;
        }
        rt.localRotation = Quaternion.identity;
        rt.localScale = stickerOriginalScale;
    }

    public void Hide(bool playVideo = true)
    {
        overlayPanel.SetActive(false);

        if (wobbleCoroutine != null)
        {
            StopCoroutine(wobbleCoroutine);
            wobbleCoroutine = null;
        }

        if (!showingSticker && playVideo)
        {
            VideoPlayState.IsAuthorized = true;
            var nav = FindObjectOfType<NavigationManager>();
            if (nav != null && nav.currentState == NavigationManager.AppState.Exploracao)
            {
                nav.TryPlayExploracaoVideo();
            }
        }

        showingSticker = false;
    }
}