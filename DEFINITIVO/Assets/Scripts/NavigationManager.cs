using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    public enum AppState { Principal, Mapa, Mochila, Galeria }
    public AppState currentState;

    public GameObject telaPrincipal;
    public GameObject telaMapa;
    public GameObject telaMochila;
    public GameObject telaGaleria;

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
        }

        currentState = newState;
    }

    // Métodos públicos para os botões (sem parâmetros)
    public void GoToPrincipal() => SetState(AppState.Principal);
    public void GoToMapa() => SetState(AppState.Mapa);
    public void GoToMochila() => SetState(AppState.Mochila);
    public void GoToGaleria() => SetState(AppState.Galeria);
}