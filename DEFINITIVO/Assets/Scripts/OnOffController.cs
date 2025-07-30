using UnityEngine;
using UnityEngine.UI;

public class NavBarController : MonoBehaviour
{
    [Header("Referências")]
    public RawImage rawImage;      // Arraste sua RawImage aqui
    public GameObject navBar;      // Arraste sua NavBar aqui
    public Image imagemExtra1;     // Arraste a 1ª imagem adicional aqui
    public Image imagemExtra2;     // Arraste a 2ª imagem adicional aqui

    void Update()
    {
        // Se a RawImage estiver ativa, desativa NavBar + Imagens Extras
        if (rawImage != null && rawImage.gameObject.activeSelf)
        {
            if (navBar != null) navBar.SetActive(false);
            if (imagemExtra1 != null) imagemExtra1.gameObject.SetActive(false);
            if (imagemExtra2 != null) imagemExtra2.gameObject.SetActive(false);
        }
        else // Se a RawImage estiver INATIVA, reativa tudo
        {
            if (navBar != null) navBar.SetActive(true);
            if (imagemExtra1 != null) imagemExtra1.gameObject.SetActive(true);
            if (imagemExtra2 != null) imagemExtra2.gameObject.SetActive(true);
        }
    }
}