using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject tutorialPanel;
    public Image professoraImage;
    public TextMeshProUGUI tutorialText;
    public Button nextButton;

    [Header("Conteúdo do Tutorial")]
    public Sprite[] professoraSprites;
    public string[] tutorialMessages;

    [Header("Configurações de Animação")]
    public float slideInDuration = 1.5f;
    public float idleMovementAmount = 8f;
    public float idleMovementSpeed = 1.5f;
    public float idleRotationAmount = 2f;

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
        tutorialPanel.SetActive(true);
    }

    void Start()
    {
        originalImagePosition = professoraImage.rectTransform.anchoredPosition;
        offScreenPosition = originalImagePosition + Vector3.left * 1000f;

        nextButton.onClick.AddListener(NextSlide);
        StartTutorial();
    }

    void StartTutorial()
    {
        currentSlide = 0;
        ShowSlide(currentSlide);
    }

    void ShowSlide(int slideIndex)
    {
        if (slideIndex >= professoraSprites.Length || slideIndex >= tutorialMessages.Length)
        {
            EndTutorial();
            return;
        }

        professoraImage.sprite = professoraSprites[slideIndex];
        currentMessage = tutorialMessages[slideIndex];

        StopAllCoroutines();

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

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentMessage));

        nextButton.GetComponentInChildren<TextMeshProUGUI>().text =
            (slideIndex == professoraSprites.Length - 1) ? "Começar!" : "Próximo";
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
        // Se ainda está digitando → completa o texto e não avança
        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            tutorialText.text = currentMessage;
            isTyping = false;
            return;
        }

        // Se já terminou de digitar → avança para o próximo slide
        currentSlide++;
        if (currentSlide < professoraSprites.Length)
        {
            ShowSlide(currentSlide);
        }
        else
        {
            EndTutorial();
        }
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

    void EndTutorial()
    {
        StopAllCoroutines();
        tutorialPanel.SetActive(false);
        Debug.Log("Tutorial finalizado! Iniciando jogo...");
    }

    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveAllListeners();
    }
}
