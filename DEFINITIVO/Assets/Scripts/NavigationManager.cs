using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class NavigationManager : MonoBehaviour
{
    public enum AppState { Principal, Mapa, Mochila, Galeria, Exploracao }
    public AppState currentState;

    public GameObject telaPrincipal;
    public GameObject telaMapa;
    public GameObject telaMochila;
    public GameObject telaGaleria;
    public GameObject telaExploracao; // painel que contém RawImage + VideoPlayer

    private VideoPlayer videoPlayerExploracao;
    private RawImage videoRawImage;
    private CanvasGroup exploracaoCanvasGroup;
    private bool videoFinalizado = false;

    void Start()
    {
        // tenta achar o VideoPlayer mesmo que a tela esteja inativa no inspector
        videoPlayerExploracao = telaExploracao.GetComponentInChildren<VideoPlayer>(true);

        if (videoPlayerExploracao != null)
        {
            // desativa play automático
            videoPlayerExploracao.playOnAwake = false;
            videoPlayerExploracao.Pause();
            videoPlayerExploracao.loopPointReached += OnVideoFinished;
        }

        // encontra a RawImage que exibe o vídeo
        videoRawImage = videoPlayerExploracao.GetComponent<RawImage>();
        if (videoRawImage == null)
            videoRawImage = videoPlayerExploracao.GetComponentInChildren<RawImage>(true);

        // opcional: esconde a RawImage no começo
        if (videoRawImage != null)
            videoRawImage.gameObject.SetActive(false);

        // pega (ou adiciona) o CanvasGroup para podermos esconder a UI sem desativar o GameObject
        exploracaoCanvasGroup = telaExploracao.GetComponent<CanvasGroup>();
        if (exploracaoCanvasGroup == null)
            exploracaoCanvasGroup = telaExploracao.AddComponent<CanvasGroup>();

        Debug.Log("[Nav] VideoPlayer encontrado? " + (videoPlayerExploracao != null));
        SetState(AppState.Principal);
    }

    private void SetState(AppState newState)
    {
        // ativa/desativa todas as telas, EXCETO a telaExploracao (mantemos ela ativa)
        telaPrincipal.SetActive(false);
        telaMapa.SetActive(false);
        telaMochila.SetActive(false);
        telaGaleria.SetActive(false);

        switch (newState)
        {
            case AppState.Principal:
                telaPrincipal.SetActive(true);
                HideExploracao(true);
                PauseVideoIfPlaying();
                break;
            case AppState.Mapa:
                telaMapa.SetActive(true);
                HideExploracao(true);
                PauseVideoIfPlaying();
                break;
            case AppState.Mochila:
                telaMochila.SetActive(true);
                HideExploracao(true);
                PauseVideoIfPlaying();
                break;
            case AppState.Galeria:
                telaGaleria.SetActive(true);
                HideExploracao(true);
                PauseVideoIfPlaying();
                break;
            case AppState.Exploracao:
                HideExploracao(false);
                ResumeVideoIfNeeded();
                break;
        }

        currentState = newState;
    }

    private void HideExploracao(bool hide)
    {
        if (exploracaoCanvasGroup != null)
        {
            exploracaoCanvasGroup.alpha = hide ? 0f : 1f;
            exploracaoCanvasGroup.interactable = !hide;
            exploracaoCanvasGroup.blocksRaycasts = !hide;
        }
        else
        {
            foreach (Transform t in telaExploracao.transform)
            {
                if (t.GetComponentInChildren<VideoPlayer>(true) != null) continue;
                t.gameObject.SetActive(!hide);
            }
        }
    }

    private void PauseVideoIfPlaying()
    {
        if (videoPlayerExploracao != null && videoPlayerExploracao.isPlaying)
        {
            videoPlayerExploracao.Pause();
            Debug.Log("[Nav] Video pausado em: " + videoPlayerExploracao.time);
        }
    }

    private void ResumeVideoIfNeeded()
    {
        if (videoPlayerExploracao != null && !videoPlayerExploracao.isPlaying && !videoFinalizado)
        {
            if (videoRawImage != null)
                videoRawImage.gameObject.SetActive(true);

            videoPlayerExploracao.Play();
            Debug.Log("[Nav] Video iniciado/retomado em: " + videoPlayerExploracao.time);
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[Nav] Video finalizado");
        videoFinalizado = true;
        if (videoRawImage != null)
            videoRawImage.gameObject.SetActive(false);
        videoPlayerExploracao.Stop();
    }

    // Métodos públicos para botões
    public void GoToPrincipal() => SetState(AppState.Principal);
    public void GoToMapa() => SetState(AppState.Mapa);
    public void GoToMochila() => SetState(AppState.Mochila);
    public void GoToGaleria() => SetState(AppState.Galeria);
    public void GoToExploracao() => SetState(AppState.Exploracao);
}