using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;



public class PostVideoImageController : MonoBehaviour
{
    [Header("Referências")]
    public RectTransform imageTransform;
    public Button cameraButton;

    [Header("Config")]
    public float slideDuration = 0.6f;
    public float stayTime = 30f;

    [Header("Animação Suave")]
    public float floatAmplitude = 10f;
    public float floatSpeed = 1.5f;

    [Header("Texto")]
    public TextMeshProUGUI infoText;



    private Coroutine floatRoutine;


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

        UpdateText(); // 👈 AQUI É A CHAVE

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

        // inicia animação suave
        if (floatRoutine != null)
            StopCoroutine(floatRoutine);

        floatRoutine = StartCoroutine(FloatImage(center));

        Debug.Log("✅ Imagem posicionada no centro + animação suave iniciada");

    }

    private IEnumerator SlideOut()


    {

        // para animação suave antes de sair
        if (floatRoutine != null)
        {
            StopCoroutine(floatRoutine);
            floatRoutine = null;
        }

        imageTransform.anchoredPosition = center;




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


    private string GetSuffixByArea(string areaName)
    {
        switch (areaName)
        {
            
            case "Serrapilheira":
                return "da";


            case "CursoDagua":
            case "Dossel":
            case "Subosque":
                return "do";

            case "Epifitas":
                return "das";

            default:
                Debug.LogWarning("Área desconhecida: " + areaName);
                return "";
        }
    }


    private void UpdateText()
    {
        NativeCameraExample cameraExample = FindObjectOfType<NativeCameraExample>();

        if (cameraExample == null || string.IsNullOrEmpty(cameraExample.currentArea))
        {
            Debug.LogWarning("Área atual não encontrada");
            return;
        }

        string suffix = GetSuffixByArea(cameraExample.currentArea);
        string areaName = cameraExample.currentArea;

        string areaFormatted = FormatAreaName(cameraExample.currentArea);

        infoText.text =
            "Agora que você sabe mais sobre esse ambiente, tire uma foto "
            + suffix + " " + areaFormatted;


    }

    private string FormatAreaName(string area)
    {
        switch (area)
        {
            case "CursoDagua": return "Curso d’água";
            case "Serrapilheira": return "Serrapilheira";
            case "Dossel": return "Dossel";
            case "Subosque": return "Sub-bosque";
            case "Epifitas": return "Epífitas";
            default: return area;
        }
    }





    IEnumerator FloatImage(Vector2 basePos)
    {
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * floatSpeed;
            float offsetY = Mathf.Sin(t) * floatAmplitude;
            imageTransform.anchoredPosition = basePos + new Vector2(0, offsetY);
            yield return null;
        }
    }









}
