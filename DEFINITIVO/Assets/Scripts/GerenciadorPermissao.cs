using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorPermissao : MonoBehaviour
{
    public string nomeCenaPrincipal;

    void Start()
    {
        // Verifica se j� aceitou antes
        if (PlayerPrefs.GetInt("AceitouTermos", 0) == 1)
        {
            StartCoroutine(LoadMainSceneWithDelay());
        }
        }

        private System.Collections.IEnumerator LoadMainSceneWithDelay()
        {
        yield return new WaitForSeconds(0.5f);
        if (!string.IsNullOrEmpty(nomeCenaPrincipal))
        {
            SceneManager.LoadScene(nomeCenaPrincipal);
        }
        else
        {
            SceneManager.LoadScene("MainScene");
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

        // Isso aqui � s� pra funcionar no Editor (Unity)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}