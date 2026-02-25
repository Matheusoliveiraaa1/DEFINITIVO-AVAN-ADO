using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[System.Serializable]

public class TutorialSlide
{
    public string message;
    public Sprite professoraSprite;
    public Sprite extraImage;

    public AudioClip voiceAudio; // 🔥 NOVO
}


public class TutorialManager : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject tutorialPanel;
    public Image professoraImage;
    public Image extraImageUI; // NOVO → arraste a Image extra do canvas aqui
    public TextMeshProUGUI tutorialText;
    public Button nextButton;

    [Header("Conteúdo do Tutorial")]
    public TutorialSlide[] slides; // NOVO → tudo em um lugar só!

    [Header("Áudio")]
    public AudioSource audioSource;


    [Header("Configurações de Animação")]
    public float slideInDuration = 1.5f;
    public float idleMovementAmount = 8f;
    public float idleMovementSpeed = 1.5f;
    public float idleRotationAmount = 2f;


    [Header("Animação Extra Image")]
    public float extraIdleAmount = 5f;
    public float extraIdleSpeed = 1.3f;
    public float extraIdleRotation = 1.5f;




    [Header("Imagem Especial Primeiro Slide")]
    public Image firstSlideImage;

    public float firstSlideEnterDuration = 1f;
    public float firstSlideIdleAmount = 6f;
    public float firstSlideIdleSpeed = 1.5f;
    public float firstSlideIdleRotation = 2f;







    [Header("Configurações de Digitação")]
    public float typingSpeed = 0.05f;

    private int currentSlide = 0;
    private Vector3 originalImagePosition;
    private Vector3 offScreenPosition;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentMessage;
    private bool hasPlayedEntrance = false;



    private Vector3 firstSlideOriginalPos;
    private Vector3 firstSlideOffScreenRight;
    private bool firstSlideAlreadyShown = false;
    private Coroutine firstSlideIdleCoroutine;

    void Awake()
    {
        if (PlayerPrefs.GetInt("TutorialVisto", 0) == 1)
        {
            tutorialPanel.SetActive(false);
            enabled = false; // desativa o script
            return;
        }

        tutorialPanel.SetActive(false);
    }

    void Start()
    {
        StartCoroutine(StartTutorialWithDelay());
    }

    IEnumerator StartTutorialWithDelay()
    {
        yield return new WaitForSeconds(1f); // ⏳ espera 2 segundos

        tutorialPanel.SetActive(true);

        firstSlideOriginalPos = firstSlideImage.rectTransform.anchoredPosition;
        firstSlideOffScreenRight = firstSlideOriginalPos + Vector3.right * 1200f;

        firstSlideImage.gameObject.SetActive(false);

        originalImagePosition = professoraImage.rectTransform.anchoredPosition;
        offScreenPosition = originalImagePosition + Vector3.left * 1000f;

        nextButton.onClick.AddListener(NextSlide);

        ShowSlide(0);
    }






    void ShowSlide(int index)
    {
        if (index >= slides.Length)
        {
            EndTutorial();
            return;
        }

        // Carrega informações do slide atual
        var slide = slides[index];
        // ===== IMAGEM ESPECIAL PRIMEIRO SLIDE =====
        if (index == 0 && !firstSlideAlreadyShown)
        {
            firstSlideAlreadyShown = true;
            firstSlideImage.gameObject.SetActive(true);
            StartCoroutine(FirstSlideEnter());
        }
        // 🔊 Controla áudio
        if (audioSource.isPlaying)
            audioSource.Stop();

        if (slide.voiceAudio != null)
        {
            audioSource.clip = slide.voiceAudio;
            audioSource.Play();
        }


        professoraImage.sprite = slide.professoraSprite;

        // --- TRATAMENTO DA EXTRA IMAGE ---
        if (slide.extraImage == null)
        {
            // Sem imagem extra → some e para animação
            extraImageUI.sprite = null;
            extraImageUI.color = new Color(1, 1, 1, 0);

            // Para animação sem usar StopAllCoroutines
            StopCoroutine("AnimateExtraImage");
        }
        else
        {
            // Troca sprite e ativa visibilidade
            extraImageUI.sprite = slide.extraImage;
            extraImageUI.color = new Color(1, 1, 1, 1);

            // Reinicia somente a animação da imagem extra
            StopCoroutine("AnimateExtraImage");
            StartCoroutine("AnimateExtraImage");
        }

        currentMessage = slide.message;

        // --- PARAR APENAS AS COROUTINES RELEVANTES ---
        StopCoroutine("AnimateProfessoraIdle");
        StopCoroutine("TypeText");

        // Animações de entrada ou idle
        if (!hasPlayedEntrance)
        {
            StartCoroutine(AnimateProfessoraEntrance());
            hasPlayedEntrance = true;
        }
        else
        {
            professoraImage.rectTransform.anchoredPosition = originalImagePosition;
            StartCoroutine(AnimateProfessoraIdle());
        }

        // Reinicia digitação
        typingCoroutine = StartCoroutine(TypeText(currentMessage));

        // Texto do botão
        nextButton.GetComponentInChildren<TextMeshProUGUI>().text =
            (index == slides.Length - 1) ? "Começar!" : "Próximo";
    }


    IEnumerator TypeText(string message)
    {
        isTyping = true;
        tutorialText.text = "";

        foreach (char letter in message)
        {
            tutorialText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    public void NextSlide()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            tutorialText.text = currentMessage;
            isTyping = false;

            // ❌ NÃO parar o áudio aqui
            return;
        }
        // Se estamos saindo do primeiro slide
        if (currentSlide == 0 && firstSlideImage.gameObject.activeSelf)
        {
            StartCoroutine(FirstSlideExit());
        }
        currentSlide++;
        ShowSlide(currentSlide);
    }


    IEnumerator FirstSlideEnter()
    {
        RectTransform rect = firstSlideImage.rectTransform;
        rect.anchoredPosition = firstSlideOffScreenRight;

        float elapsed = 0f;

        while (elapsed < firstSlideEnterDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / firstSlideEnterDuration);
            rect.anchoredPosition = Vector3.Lerp(firstSlideOffScreenRight, firstSlideOriginalPos, t);
            yield return null;
        }

        rect.anchoredPosition = firstSlideOriginalPos;

        firstSlideIdleCoroutine = StartCoroutine(FirstSlideIdle());
    }

    IEnumerator FirstSlideIdle()
    {
        RectTransform rect = firstSlideImage.rectTransform;

        while (true)
        {
            float time = Time.time * firstSlideIdleSpeed;

            float offsetX = Mathf.Sin(time * 1.2f) * firstSlideIdleAmount;
            float offsetY = Mathf.Cos(time * 0.8f) * (firstSlideIdleAmount * 0.4f);

            rect.anchoredPosition = firstSlideOriginalPos + new Vector3(offsetX, offsetY, 0);

            float rot = Mathf.Sin(time * 1.5f) * firstSlideIdleRotation;
            rect.localRotation = Quaternion.Euler(0, 0, rot);

            yield return null;
        }
    }






    IEnumerator FirstSlideExit()
    {
        if (firstSlideIdleCoroutine != null)
            StopCoroutine(firstSlideIdleCoroutine);

        RectTransform rect = firstSlideImage.rectTransform;

        Vector3 startPos = rect.anchoredPosition;
        Vector3 targetPos = firstSlideOriginalPos + Vector3.right * 1200f;

        float elapsed = 0f;

        while (elapsed < firstSlideEnterDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / firstSlideEnterDuration);
            rect.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        firstSlideImage.gameObject.SetActive(false);
    }




















    IEnumerator AnimateProfessoraEntrance()
    {
        RectTransform rect = professoraImage.rectTransform;
        rect.anchoredPosition = offScreenPosition;
        float elapsedTime = 0f;

        while (elapsedTime < slideInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / slideInDuration);
            rect.anchoredPosition = Vector3.Lerp(offScreenPosition, originalImagePosition, t);
            yield return null;
        }

        rect.anchoredPosition = originalImagePosition;
        StartCoroutine(AnimateProfessoraIdle());
    }

    IEnumerator AnimateProfessoraIdle()
    {
        RectTransform rect = professoraImage.rectTransform;

        while (true)
        {
            float time = Time.time * idleMovementSpeed;

            float targetX = Mathf.Sin(time * 1.1f) * idleMovementAmount;
            float targetY = Mathf.Cos(time * 0.8f) * (idleMovementAmount * 0.4f);

            float smoothX = Mathf.Lerp(rect.anchoredPosition.x - originalImagePosition.x, targetX, Time.deltaTime * 2f);
            float smoothY = Mathf.Lerp(rect.anchoredPosition.y - originalImagePosition.y, targetY, Time.deltaTime * 2f);

            rect.anchoredPosition = originalImagePosition + new Vector3(smoothX, smoothY, 0);

            float targetRotation = Mathf.Sin(time * 1.2f) * idleRotationAmount;
            float currentRotation = rect.localRotation.eulerAngles.z;
            if (currentRotation > 180) currentRotation -= 360;

            float smoothRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * 3f);
            rect.localRotation = Quaternion.Euler(0, 0, smoothRotation);

            yield return null;
        }
    }


    IEnumerator AnimateExtraImage()
    {
        RectTransform rect = extraImageUI.rectTransform;

        Vector3 originalPos = rect.anchoredPosition;

        while (true) // ← sempre anima até ser interrompida
        {
            float time = Time.time * extraIdleSpeed;

            float offsetX = Mathf.Sin(time * 1.4f) * extraIdleAmount;
            float offsetY = Mathf.Cos(time * 0.9f) * (extraIdleAmount * 0.5f);

            float smoothX = Mathf.Lerp(rect.anchoredPosition.x - originalPos.x, offsetX, Time.deltaTime * 2f);
            float smoothY = Mathf.Lerp(rect.anchoredPosition.y - originalPos.y, offsetY, Time.deltaTime * 2f);

            rect.anchoredPosition = originalPos + new Vector3(smoothX, smoothY, 0);

            float targetRotation = Mathf.Sin(time * 1.7f) * extraIdleRotation;
            float currentRotation = rect.localRotation.eulerAngles.z;
            if (currentRotation > 180) currentRotation -= 360;

            float smoothRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * 3f);
            rect.localRotation = Quaternion.Euler(0, 0, smoothRotation);

            yield return null;
        }
    }






    void EndTutorial()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();

        StopAllCoroutines();
        PlayerPrefs.SetInt("TutorialVisto", 1);
        PlayerPrefs.Save();
        tutorialPanel.SetActive(false);

        Debug.Log("Tutorial finalizado!");
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveAllListeners();
    }
}