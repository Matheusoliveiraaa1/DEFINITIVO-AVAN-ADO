using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[System.Serializable]
public class TutorialSlide
{
    public string message;            // Texto da fala
    public Sprite professoraSprite;   // Sprite da professora
    public Sprite extraImage;         // NOVO → sprite extra por fala
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

    [Header("Configurações de Animação")]
    public float slideInDuration = 1.5f;
    public float idleMovementAmount = 8f;
    public float idleMovementSpeed = 1.5f;
    public float idleRotationAmount = 2f;


    [Header("Animação Extra Image")]
    public float extraIdleAmount = 5f;
    public float extraIdleSpeed = 1.3f;
    public float extraIdleRotation = 1.5f;






    [Header("Configurações de Digitação")]
    public float typingSpeed = 0.05f;

    private int currentSlide = 0;
    private Vector3 originalImagePosition;
    private Vector3 offScreenPosition;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentMessage;
    private bool hasPlayedEntrance = false;

    void Awake()
    {
        /*
     if (PlayerPrefs.GetInt("TutorialVisto", 0) == 1)
     {
         tutorialPanel.SetActive(false);
         enabled = false;
         return;
     }
     */

        tutorialPanel.SetActive(true);
    }

    void Start()
    {
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
            return;
        }

        currentSlide++;
        ShowSlide(currentSlide);
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