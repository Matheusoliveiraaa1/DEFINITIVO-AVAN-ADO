using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.IO;
using System.Text.RegularExpressions;

public class PhotoAreaOverlay : MonoBehaviour
{
    public static PhotoAreaOverlay Instance;


    public TMP_Text stickerMessageText;



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

    [Header("UI Elements - Sticker Glow (novo)")]
    public Image stickerGlow;
    [Tooltip("Multiplicador do tamanho do glow em relação ao sticker")]
    public float stickerGlowScaleMultiplier = 1.35f;
    [Tooltip("Alpha base do glow (0..1)")]
    public float stickerGlowAlpha = 0.5f;

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

    [Header("Sticker Position")]
    [Tooltip("Posição vertical do sticker (valores positivos sobem)")]
    public float stickerVerticalOffset = 150f;





    private bool showingSticker = false;
    private Vector3 stickerOriginalScale;
    private Vector3 stickerGlowOriginalScale;
    private Coroutine wobbleCoroutine;
    private bool isAnimatingToBackpack = false;

    // --- NOVO: Controle da animação da photo1 ---
    private Coroutine photo1AnimationCoroutine;
    private Vector3 photo1OriginalScale;

    private void Awake()
    {
        Instance = this;
        overlayPanel.SetActive(false);
        okButton.onClick.AddListener(OnOkButtonClick);

        if (stickerMain != null)
            stickerOriginalScale = stickerMain.rectTransform.localScale;

        if (stickerGlow != null)
        {
            stickerGlowOriginalScale = stickerGlow.rectTransform.localScale;
            stickerGlow.gameObject.SetActive(false);
            stickerGlow.raycastTarget = false;
        }
    }

    private void OnOkButtonClick()
    {
        if (showingSticker && !isAnimatingToBackpack)
        {
            if (stickerExtra1 != null)
                stickerExtra1.gameObject.SetActive(false);
            if (stickerExtra2 != null)
                stickerExtra2.gameObject.SetActive(false);
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

            if (stickerGlow != null)
            {
                stickerGlow.rectTransform.localScale = originalScale * scale * stickerGlowScaleMultiplier;
                stickerGlow.rectTransform.position = stickerRT.position;
            }
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

            if (stickerGlow != null)
            {
                stickerGlow.rectTransform.position = stickerRT.position;
                stickerGlow.rectTransform.localScale = originalScale * minScale * stickerGlowScaleMultiplier;
                Color c = stickerGlow.color;
                c.a = Mathf.Lerp(stickerGlowAlpha, 0f, progress); // fade out
                stickerGlow.color = c;
            }
            yield return null;
        }

        // Some no final
        stickerRT.position = targetPosition;
        stickerRT.localScale = Vector3.zero;
        if (stickerGlow != null)
        {
            stickerGlow.gameObject.SetActive(false);
        }

        overlayPanel.SetActive(false);
        stickerMain.gameObject.SetActive(false);
        Hide(false);
        isAnimatingToBackpack = false;
    }

    // --- MODO ÁREA ---
    public static void Show(Sprite img1 = null, Sprite img2 = null, Sprite img3 = null)
    {
        if (Instance == null)
            return;

        Instance.showingSticker = false;
        Instance.overlayPanel.SetActive(true);

        Instance.photo1.gameObject.SetActive(true);
        Instance.photo2.gameObject.SetActive(true);
        Instance.photo3.gameObject.SetActive(true);

        if (Instance.stickerMain)
            Instance.stickerMain.gameObject.SetActive(false);
        if (Instance.stickerExtra1)
            Instance.stickerExtra1.gameObject.SetActive(false);
        if (Instance.stickerExtra2)
            Instance.stickerExtra2.gameObject.SetActive(false);
        if (Instance.stickerGlow)
            Instance.stickerGlow.gameObject.SetActive(false);

        if (Instance.wobbleCoroutine != null)
        {
            Instance.StopCoroutine(Instance.wobbleCoroutine);
            Instance.wobbleCoroutine = null;
        }

        // --- ANIMAÇÃO DA PROFESSORINHA ---
        if (Instance.photo1AnimationCoroutine != null)
        {
            Instance.StopCoroutine(Instance.photo1AnimationCoroutine);
        }

        Instance.photo1OriginalScale = Instance.photo1.rectTransform.localScale;
        Instance.photo1.rectTransform.localScale = Vector3.zero;
        Instance.photo1AnimationCoroutine = Instance.StartCoroutine(Instance.AnimatePhoto1());

        if (img1 != null)
            Instance.photo1.sprite = img1;
        if (img2 != null)
            Instance.photo2.sprite = img2;
        if (img3 != null)
            Instance.photo3.sprite = img3;
    }


    private static string FormatStickerName(string rawName)
    {
        // Remove extensão, exemplo: AraraAzul -> araraazul
        rawName = Path.GetFileNameWithoutExtension(rawName);

        // Insere espaços antes de letras maiúsculas
        var result = Regex.Replace(rawName, "([A-Z])", " $1").Trim();

        // Primeira letra maiúscula
        return char.ToUpper(result[0]) + result.Substring(1);
    }











    // --- MODO STICKER ---
    // --- MODO STICKER ---
    public static void ShowSticker(Sprite mainSprite, Sprite overrideExtra1 = null, Sprite overrideExtra2 = null)
    {
        if (Instance == null)
            return;

        Instance.showingSticker = true;
        Instance.overlayPanel.SetActive(true);
        Instance.isAnimatingToBackpack = false;

        // Esconde as fotos da área
        Instance.photo1.gameObject.SetActive(false);
        Instance.photo2.gameObject.SetActive(false);
        Instance.photo3.gameObject.SetActive(false);

        if (Instance.stickerMain != null)
        {
            Instance.stickerMain.sprite = mainSprite;

            // === GERAR TEXTO AUTOMÁTICO DO BALÃO ===
            string prettyName = FormatStickerName(mainSprite.name);
            Instance.stickerMessageText.text =
                $"Parece que você encontrou a espécie <b>{prettyName}</b>!";



            Instance.stickerMain.gameObject.SetActive(true);
            Instance.stickerMain.rectTransform.localScale = Instance.stickerOriginalScale;
            Instance.stickerMain.rectTransform.localRotation = Quaternion.identity;

            // ========== MUDANÇA AQUI ==========
            // Antes: Instance.stickerMain.rectTransform.anchoredPosition = Vector2.zero;
            // Agora: Usa o offset vertical configurável
            Instance.stickerMain.rectTransform.anchoredPosition = new Vector2(0, Instance.stickerVerticalOffset);
            // ==================================

            if (Instance.wobbleCoroutine != null)
                Instance.StopCoroutine(Instance.wobbleCoroutine);

            // Setup do glow
            if (Instance.stickerGlow != null)
            {
                if (Instance.stickerGlow.sprite == null)
                {
                    // fallback: usa o próprio sprite do sticker se não houver um glow separado
                    Instance.stickerGlow.sprite = mainSprite;
                }
                Instance.stickerGlow.gameObject.SetActive(true);
                Instance.stickerGlow.color = new Color(1f, 0.95f, 0.6f, Instance.stickerGlowAlpha);

                // O glow também recebe o mesmo offset vertical
                Instance.stickerGlow.rectTransform.anchoredPosition = new Vector2(0, Instance.stickerVerticalOffset);
                Instance.stickerGlow.rectTransform.localScale = Instance.stickerOriginalScale * Instance.stickerGlowScaleMultiplier;

                // Garantir que o glow esteja por trás
                int stickerIndex = Instance.stickerMain.transform.GetSiblingIndex();
                int glowIndex = Mathf.Max(0, stickerIndex - 1);
                Instance.stickerGlow.transform.SetSiblingIndex(glowIndex);
            }

            Instance.StartCoroutine(Instance.AnimateStickerPop());
        }

        if (Instance.stickerExtra1 != null)
        {
            if (overrideExtra1 != null)
                Instance.stickerExtra1.sprite = overrideExtra1;
            bool hasSprite = Instance.stickerExtra1.sprite != null;
            Instance.stickerExtra1.gameObject.SetActive(hasSprite);
        }

        if (Instance.stickerExtra2 != null)
        {
            if (overrideExtra2 != null)
                Instance.stickerExtra2.sprite = overrideExtra2;
            bool hasSprite = Instance.stickerExtra2.sprite != null;
            Instance.stickerExtra2.gameObject.SetActive(hasSprite);
        }
    }

    private IEnumerator AnimateStickerPop()
    {
        RectTransform rt = stickerMain.rectTransform;
        rt.localScale = Vector3.zero;
        rt.localRotation = Quaternion.identity;

        if (stickerGlow != null)
            stickerGlow.rectTransform.localScale = Vector3.zero;

        float t = 0;
        while (t < stickerPopDuration)
        {
            t += Time.deltaTime;
            float progress = t / stickerPopDuration;
            float scale = 1f - Mathf.Pow(1f - progress, 2f);
            float popBoost = (progress < 0.8f) ? Mathf.Lerp(stickerPopScale, 1f, progress / 0.8f) : 1f;

            rt.localScale = stickerOriginalScale * scale * popBoost;

            if (stickerGlow != null)
            {
                stickerGlow.rectTransform.localScale = stickerOriginalScale * stickerGlowScaleMultiplier * scale * popBoost;
                Color c = stickerGlow.color;
                c.a = Mathf.Lerp(0f, stickerGlowAlpha, progress);
                stickerGlow.color = c;
                stickerGlow.rectTransform.position = rt.position;
            }
            yield return null;
        }

        rt.localScale = stickerOriginalScale;
        if (stickerGlow != null)
        {
            stickerGlow.rectTransform.localScale = stickerOriginalScale * stickerGlowScaleMultiplier;
            Color c = stickerGlow.color;
            c.a = stickerGlowAlpha;
            stickerGlow.color = c;
        }

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

            if (stickerGlow != null)
            {
                float glowPulse = 1f + Mathf.Sin(Time.time * stickerPulseSpeed * 1.2f) * (stickerPulseScale * 1.5f);
                stickerGlow.rectTransform.localScale = stickerOriginalScale * stickerGlowScaleMultiplier * glowPulse;
                stickerGlow.rectTransform.position = rt.position;
                Color c = stickerGlow.color;
                c.a = stickerGlowAlpha * (0.8f + 0.2f * (Mathf.Sin(Time.time * stickerPulseSpeed * 1.2f) * 0.5f + 0.5f));
                stickerGlow.color = c;
            }
            yield return null;
        }

        rt.localRotation = Quaternion.identity;
        rt.localScale = stickerOriginalScale;
        if (stickerGlow != null)
        {
            stickerGlow.rectTransform.localScale = stickerOriginalScale * stickerGlowScaleMultiplier;
        }
    }

    // --- NOVO: Animação da professorinha (photo1) ---
    private IEnumerator AnimatePhoto1()
    {
        RectTransform rt = photo1.rectTransform;

        // Guarda posição e escala originais
        Vector3 targetPos = rt.anchoredPosition;
        Vector3 offScreenStart = targetPos + new Vector3(-Screen.width * 0.6f, 0f, 0f); // começa fora da tela à esquerda
        rt.anchoredPosition = offScreenStart;

        // Começa um pouco inclinada e menor
        rt.localScale = photo1OriginalScale * 0.95f;
        rt.localRotation = Quaternion.Euler(0, 0, 10f); // inclinada pra direita

        // --- Entrada suave com curva e leve "bounce" natural ---
        float duration = 1.2f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            // Suavização (easeOutCubic)
            float eased = 1f - Mathf.Pow(1f - progress, 3f);

            // Movimento lateral fluido
            rt.anchoredPosition = Vector3.Lerp(offScreenStart, targetPos, eased);

            // Escala com leve overshoot, mas suavizando no final
            float scale = Mathf.Lerp(0.95f, 1.0f + Mathf.Sin(progress * Mathf.PI) * 0.02f, eased);
            rt.localScale = photo1OriginalScale * scale;

            // Rotação vai suavizando até ficar reta
            float rotation = Mathf.Lerp(10f, 0f, eased);
            rt.localRotation = Quaternion.Euler(0, 0, rotation);

            yield return null;
        }

        // Garante estado final exato
        rt.anchoredPosition = targetPos;
        rt.localScale = photo1OriginalScale;
        rt.localRotation = Quaternion.identity;

        // --- Transição suave para o loop contínuo ---
        float transitionTime = 0.2f;
        float startScale = rt.localScale.x;
        float endScale = 1f;
        float elapsed = 0f;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / transitionTime);
            rt.localScale = Vector3.Lerp(photo1OriginalScale * startScale, photo1OriginalScale * endScale, p);
            yield return null;
        }

        // --- Loop contínuo de balanço e pulso suaves ---
        float time = 0f;
        while (overlayPanel.activeSelf && !showingSticker)
        {
            time += Time.deltaTime;

            // Balanço natural
            float angle = Mathf.Sin(time * 1.5f) * 3f;
            float pulse = 1f + Mathf.Sin(time * 2f) * 0.015f; // pulso mais sutil (antes era 0.02f)

            rt.localRotation = Quaternion.Euler(0, 0, angle);
            rt.localScale = photo1OriginalScale * pulse;

            yield return null;
        }

        rt.localRotation = Quaternion.identity;
        rt.localScale = photo1OriginalScale;
    }

    public void Hide(bool playVideo = true)
    {
        overlayPanel.SetActive(false);

        if (wobbleCoroutine != null)
        {
            StopCoroutine(wobbleCoroutine);
            wobbleCoroutine = null;
        }

        if (photo1AnimationCoroutine != null)
        {
            StopCoroutine(photo1AnimationCoroutine);
            photo1AnimationCoroutine = null;
            photo1.rectTransform.localRotation = Quaternion.identity;
            photo1.rectTransform.localScale = photo1OriginalScale;
        }

        if (stickerGlow != null)
        {
            stickerGlow.gameObject.SetActive(false);
            stickerGlow.rectTransform.localScale = stickerGlowOriginalScale;
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