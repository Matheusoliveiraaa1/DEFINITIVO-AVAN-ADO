using UnityEngine;
using System.Collections;

public class LockedStickerHintController : MonoBehaviour
{
    [Header("Referência")]
    public RectTransform panel; // A PRÓPRIA IMAGE

    [Header("Configuração")]
    public float slideDuration = 0.5f;
    public float baseStayTime = 3f;

    [Header("Movimento Suave")]
    public float floatAmplitude = 10f;
    public float floatSpeed = 1.5f;

    private Vector2 offRight;
    private Vector2 offLeft;
    private Vector2 center;

    private Coroutine slideRoutine;
    private Coroutine floatRoutine;
    private Coroutine stayRoutine;

    private float remainingTime = 0f;
    private bool isVisible = false;
    private bool isExiting = false;

    void Awake()
    {
        Debug.Log("[LockedStickerHint] Awake chamado");

        if (panel == null)
        {
            Debug.LogError("[LockedStickerHint] PANEL NÃO ESTÁ ATRIBUÍDO NO INSPECTOR");
            return;
        }

        // posição FINAL definida no editor
        center = panel.anchoredPosition;
        Debug.Log("[LockedStickerHint] Center (posição final): " + center);

        RectTransform canvasRect = panel.root.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogError("[LockedStickerHint] NÃO ACHOU Canvas (RectTransform root)");
            return;
        }

        float canvasWidth = canvasRect.rect.width;

        offRight = new Vector2(canvasWidth + panel.rect.width, center.y);
        offLeft = new Vector2(-canvasWidth - panel.rect.width, center.y);

        Debug.Log("[LockedStickerHint] OffRight: " + offRight);
        Debug.Log("[LockedStickerHint] OffLeft: " + offLeft);

        // começa fora da tela à DIREITA
        panel.anchoredPosition = offRight;
        panel.gameObject.SetActive(false);

        Debug.Log("[LockedStickerHint] Panel desativado e movido para offRight");
    }

    // 🔥 CHAMAR AO CLICAR EM STICKER BLOQUEADO
    public void ShowOrExtend()
    {
        Debug.Log("[LockedStickerHint] ShowOrExtend chamado");

        remainingTime += baseStayTime;
        Debug.Log("[LockedStickerHint] RemainingTime: " + remainingTime);

        if (!isVisible || isExiting)
        {
            Debug.Log("[LockedStickerHint] Chamando Show()");
            Show();
        }
        else
        {
            Debug.Log("[LockedStickerHint] Já visível, apenas estendendo tempo");
        }
    }

    void Show()
    {
        Debug.Log("[LockedStickerHint] Show()");

        isVisible = true;
        isExiting = false;

        panel.gameObject.SetActive(true);
        Debug.Log("[LockedStickerHint] Panel ATIVADO");

        // 🔑 FORÇA SEMPRE entrada da DIREITA
        panel.anchoredPosition = offRight;

        if (slideRoutine != null)
        {
            Debug.Log("[LockedStickerHint] Parando slide anterior");
            StopCoroutine(slideRoutine);
        }

        Debug.Log("[LockedStickerHint] Slide ENTRADA: offRight → center");
        slideRoutine = StartCoroutine(Slide(offRight, center));

        if (floatRoutine == null)
        {
            Debug.Log("[LockedStickerHint] Iniciando FloatEffect");
            floatRoutine = StartCoroutine(FloatEffect());
        }

        if (stayRoutine != null)
        {
            Debug.Log("[LockedStickerHint] Reiniciando StayCountdown");
            StopCoroutine(stayRoutine);
        }

        stayRoutine = StartCoroutine(StayCountdown());
    }

    IEnumerator StayCountdown()
    {
        Debug.Log("[LockedStickerHint] StayCountdown iniciado");

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("[LockedStickerHint] Tempo acabou → Exit()");
        Exit();
    }

    void Exit()
    {
        Debug.Log("[LockedStickerHint] Exit()");

        isExiting = true;

        if (slideRoutine != null)
        {
            Debug.Log("[LockedStickerHint] Parando slide atual para saída");
            StopCoroutine(slideRoutine);
        }

        Debug.Log("[LockedStickerHint] Slide SAÍDA: center → offLeft");
        slideRoutine = StartCoroutine(Slide(center, offRight, true));

    }

    IEnumerator Slide(Vector2 from, Vector2 to, bool disableAtEnd = false)
    {
        Debug.Log("[LockedStickerHint] Slide iniciado | From: " + from + " To: " + to);

        float t = 0f;

        while (t < slideDuration)
        {
            panel.anchoredPosition = Vector2.Lerp(from, to, t / slideDuration);
            t += Time.deltaTime;
            yield return null;
        }

        panel.anchoredPosition = to;
        Debug.Log("[LockedStickerHint] Slide finalizado");

        if (disableAtEnd)
        {
            Debug.Log("[LockedStickerHint] Desativando panel no fim do slide");

            isVisible = false;
            isExiting = false;
            remainingTime = 0f;

            if (floatRoutine != null)
            {
                Debug.Log("[LockedStickerHint] Parando FloatEffect");
                StopCoroutine(floatRoutine);
                floatRoutine = null;
            }

            panel.gameObject.SetActive(false);
        }
    }

    IEnumerator FloatEffect()
    {
        Debug.Log("[LockedStickerHint] FloatEffect iniciado");

        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * floatSpeed;
            float y = Mathf.Sin(t) * floatAmplitude;

            // 🔹 NÃO mexe no X (slide controla)
            panel.anchoredPosition = new Vector2(
                panel.anchoredPosition.x,
                center.y + y
            );

            yield return null;
        }
    }
}
