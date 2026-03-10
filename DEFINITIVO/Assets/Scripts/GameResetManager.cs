using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameResetController : MonoBehaviour
{
    [Header("Painel")]
    public GameObject confirmPanel;

    [Header("Professor Image")]
    public RectTransform professorImage;
    public float floatAmplitude = 10f;
    public float floatSpeed = 1.5f;

    [Header("Texto Typewriter")]
    public TextMeshProUGUI messageText;
    [TextArea] public string fullMessage;
    public float typingSpeed = 0.03f;

    private Coroutine floatCoroutine;
    private Coroutine typingCoroutine;

    public void OpenResetPanel()
    {
        confirmPanel.SetActive(true);

        // inicia animação da imagem
        if (floatCoroutine != null)
            StopCoroutine(floatCoroutine);

        floatCoroutine = StartCoroutine(FloatProfessor());

        // inicia texto typewriter
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText());
    }

    public void CancelReset()
    {
        confirmPanel.SetActive(false);

        if (floatCoroutine != null)
            StopCoroutine(floatCoroutine);
    }

    public void ConfirmReset()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator FloatProfessor()
    {
        Vector2 basePos = professorImage.anchoredPosition;
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * floatSpeed;
            float offsetY = Mathf.Sin(t) * floatAmplitude;

            professorImage.anchoredPosition = basePos + new Vector2(0, offsetY);

            yield return null;
        }
    }

    IEnumerator TypeText()
    {
        messageText.text = "";

        foreach (char c in fullMessage)
        {
            messageText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}