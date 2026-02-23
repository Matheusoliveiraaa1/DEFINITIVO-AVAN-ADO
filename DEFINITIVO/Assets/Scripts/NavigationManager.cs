using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    public enum AppState { Principal, Mapa, Mochila, Galeria, Exploracao, Regras }
    public AppState currentState;

    [Header("Telas Principais")]
    public GameObject telaPrincipal;
    public GameObject telaMapa;
    public GameObject telaMochila;
    public GameObject telaGaleria;
    public GameObject telaExploracao;
    public GameObject telaRegras;

    [Header("Configurações do Mapa (Reset)")]
    public GameObject mapaBase; // Coloque aqui o conteúdo principal do mapa que SEMPRE deve aparecer
    public GameObject[] mapaSubTelas; // Coloque aqui as subtelas/popups que devem SUMIR ao resetar

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
            case AppState.Mapa:
                telaMapa.SetActive(true);
                ResetarMapa(); // 🔹 NOVO: Toda vez que o mapa for chamado, ele reseta o estado interno!
                break;
            case AppState.Mochila: telaMochila.SetActive(true); break;
            case AppState.Galeria: telaGaleria.SetActive(true); break;
            case AppState.Exploracao: telaExploracao.SetActive(true); break;
            case AppState.Regras: telaRegras.SetActive(true); break;
        }

        currentState = newState;

        // 🔹 se entrou na tela de exploração, tenta tocar o vídeo
        if (currentState == AppState.Exploracao)
        {
            // É sempre uma boa prática checar se a Instance não é nula antes de chamar!
            if (VideoManager.Instance != null)
                VideoManager.Instance.TryPlayVideo();
        }
    }

    // 🔹 NOVO: Função que limpa a tela do mapa
    private void ResetarMapa()
    {
        // Garante que os elementos base do mapa estão visíveis (se você usar um objeto específico pra isso)
        if (mapaBase != null)
            mapaBase.SetActive(true);

        // Desliga todas as subtelas abertas dentro do mapa
        foreach (GameObject subTela in mapaSubTelas)
        {
            if (subTela != null)
                subTela.SetActive(false);
        }
    }

    public void GoToPrincipal() => SetState(AppState.Principal);
    public void GoToMapa() => SetState(AppState.Mapa);
    public void GoToMochila() => SetState(AppState.Mochila);
    public void GoToGaleria() => SetState(AppState.Galeria);
    public void GoToExploracao() => SetState(AppState.Exploracao);
    public void GoToRegras() => SetState(AppState.Regras);
}