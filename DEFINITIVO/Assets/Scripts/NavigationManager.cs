using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    public enum AppState { Principal, Mapa, Mochila, Galeria, Exploracao, Regras }
    public AppState currentState;

    public GameObject telaPrincipal;
    public GameObject telaMapa;
    public GameObject telaMochila;
    public GameObject telaGaleria;
    public GameObject telaExploracao;
    public GameObject telaRegras;

    void Start()
    {
        SetState(AppState.Principal);
    }

    private void SetState(AppState newState)
    {
        // Desativa todas as telas
        telaPrincipal.SetActive(false);
        telaMapa.SetActive(false);
        telaMochila.SetActive(false);
        telaGaleria.SetActive(false);
        telaExploracao.SetActive(false);
        telaRegras.SetActive(false);

        // Ativa a tela correta
        switch (newState)
        {
            case AppState.Principal: telaPrincipal.SetActive(true); break;
            case AppState.Mapa: telaMapa.SetActive(true); break;
            case AppState.Mochila: telaMochila.SetActive(true); break;
            case AppState.Galeria: telaGaleria.SetActive(true); break;
            case AppState.Exploracao: telaExploracao.SetActive(true); break;
            case AppState.Regras: telaRegras.SetActive(true); break;
        }

        currentState = newState;

        // 🔹 NOVO: se entrou na tela de exploração, tenta tocar o vídeo
        if (currentState == AppState.Exploracao)
        {
            VideoManager.Instance.TryPlayVideo();
        }
    }


    public void GoToPrincipal() => SetState(AppState.Principal);
    public void GoToMapa() => SetState(AppState.Mapa);
    public void GoToMochila() => SetState(AppState.Mochila);
    public void GoToGaleria() => SetState(AppState.Galeria);
    public void GoToExploracao() => SetState(AppState.Exploracao);
    public void GoToRegras() => SetState(AppState.Regras);
}