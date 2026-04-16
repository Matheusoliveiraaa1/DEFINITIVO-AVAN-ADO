using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorPermissao : MonoBehaviour
{
    public string nomeCenaPrincipal;

    void Start()
    {
        // Verifica se já aceitou antes
        if (PlayerPrefs.GetInt("AceitouTermos", 0) == 1)
        {
            SceneManager.LoadScene(nomeCenaPrincipal);
        }
    }

    public void Aceitar()
    {
        // Salva que aceitou
        PlayerPrefs.SetInt("AceitouTermos", 1);
        PlayerPrefs.Save();

        // Vai para a cena principal
        SceneManager.LoadScene("MainScene");
    }

    public void Cancelar()
    {
        // Fecha o app
        Application.Quit();

        // Isso aqui é só pra funcionar no Editor (Unity)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}