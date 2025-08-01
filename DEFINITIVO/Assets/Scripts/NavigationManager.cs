using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    public enum AppState { Principal, Mapa, Mochila, Galeria, Exploracao }
    public AppState currentState;

    public GameObject telaPrincipal;
    public GameObject telaMapa;
    public GameObject telaMochila;
    public GameObject telaGaleria;
    public GameObject telaExploracao; // Novo campo adicionado

    void Start()
    {
        SetState(AppState.Principal);
    }

    // Método principal (privado)
    private void SetState(AppState newState)
    {
        // Desativa todas as telas
        telaPrincipal.SetActive(false);
        telaMapa.SetActive(false);
        telaMochila.SetActive(false);
        telaGaleria.SetActive(false);
        telaExploracao.SetActive(false); // Nova linha adicionada

        // Ativa apenas a tela desejada
        switch (newState)
        {
            case AppState.Principal:
                telaPrincipal.SetActive(true);
                break;
            case AppState.Mapa:
                telaMapa.SetActive(true);
                break;
            case AppState.Mochila:
                telaMochila.SetActive(true);
                break;
            case AppState.Galeria:
                telaGaleria.SetActive(true);
                break;
            case AppState.Exploracao: // Novo caso adicionado
                telaExploracao.SetActive(true);
                break;
        }

        currentState = newState;
    }

    // Métodos públicos para os botões (sem parâmetros)
    public void GoToPrincipal() => SetState(AppState.Principal);
    public void GoToMapa() => SetState(AppState.Mapa);
    public void GoToMochila() => SetState(AppState.Mochila);
    public void GoToGaleria() => SetState(AppState.Galeria);
    public void GoToExploracao() => SetState(AppState.Exploracao); // Novo método adicionado
}