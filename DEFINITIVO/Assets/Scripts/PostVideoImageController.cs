using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PostVideoImageController : MonoBehaviour
{
    [Header("Referências")]
    public RectTransform imageTransform;
    public Button cameraButton;

    [Header("Config")]
    public float slideDuration = 0.6f;
    public float stayTime = 30f;

    private Vector2 offLeft;
    private Vector2 offRight;
    private Vector2 center;

    private Coroutine autoHideRoutine;

    void Start()
    {
        float width = imageTransform.rect.width;

        // ✅ Fora da tela à esquerda (entrada)
        offLeft = new Vector2(-Screen.width - width, imageTransform.anchoredPosition.y);

        // ✅ Fora da tela à direita (saída)
        offRight = new Vector2(Screen.width + width, imageTransform.anchoredPosition.y);

        // ✅ Posição central original
        center = imageTransform.anchoredPosition;

        // ✅ Começa escondido à esquerda
        imageTransform.anchoredPosition = offLeft;
        imageTransform.gameObject.SetActive(false);

        if (cameraButton != null)
            cameraButton.onClick.AddListener(HideByCameraClick);

        Debug.Log("✅ PostVideoImageController inicializado");
    }

    // ✅ CHAMADO PELO VÍDEO QUANDO TERMINAR
    public void ShowAfterVideo()
    {
        Debug.Log("🖼️ ShowAfterVideo chamado");

        imageTransform.gameObject.SetActive(true);

        if (autoHideRoutine != null)
            StopCoroutine(autoHideRoutine);

        StartCoroutine(SlideIn());
        autoHideRoutine = StartCoroutine(AutoHide());
    }

    private IEnumerator SlideIn()
    {
        Debug.Log("➡️ Iniciando entrada da imagem");

        float t = 0f;
        Vector2 start = offLeft;

        while (t < slideDuration)
        {
            imageTransform.anchoredPosition = Vector2.Lerp(start, center, t / slideDuration);
            t += Time.deltaTime;
            yield return null;
        }

        imageTransform.anchoredPosition = center;
        Debug.Log("✅ Imagem posicionada no centro");
    }

    private IEnumerator SlideOut()
    {
        Debug.Log("⬅️ Iniciando saída da imagem para a ESQUERDA");

        float t = 0f;
        Vector2 start = center;
        Vector2 target = offLeft; // 👈 agora sai pela esquerda

        while (t < slideDuration)
        {
            imageTransform.anchoredPosition = Vector2.Lerp(start, target, t / slideDuration);
            t += Time.deltaTime;
            yield return null;
        }

        imageTransform.anchoredPosition = target;
        imageTransform.gameObject.SetActive(false);

        Debug.Log("✅ Imagem saiu da tela para a esquerda");
    }


    private IEnumerator AutoHide()
    {
        Debug.Log("⏳ AutoHide iniciado");
        yield return new WaitForSeconds(stayTime);
        StartCoroutine(SlideOut());
    }

    private void HideByCameraClick()
    {
        Debug.Log("📸 Botão da câmera clicado");

        if (imageTransform.gameObject.activeSelf)
        {
            if (autoHideRoutine != null)
                StopCoroutine(autoHideRoutine);

            StartCoroutine(SlideOut());
        }
    }
}
