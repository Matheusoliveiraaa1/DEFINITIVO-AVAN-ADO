using UnityEngine;
using UnityEngine.UI;

public class NavBarController : MonoBehaviour
{
    [Header("Referências")]
    public RawImage rawImage;      // Arraste sua RawImage aqui
    public GameObject navBar;      // Arraste sua NavBar aqui
    public Image imagemExtra1;     // Arraste a 1ª imagem adicional aqui
    public Image imagemExtra2;     // Arraste a 2ª imagem adicional aqui

    [Header("Botões extras para ocultar")]
    public GameObject botao1;      // Arraste o primeiro botão aqui
    public GameObject botao2;      // Arraste o segundo botão aqui

    [HideInInspector]
    public bool ignoreWhileVideoPlaying = false; // flag para ignorar Update

    void Update()
    {
        if (ignoreWhileVideoPlaying) return;

        if (rawImage != null && rawImage.gameObject.activeSelf)
        {
            if (navBar != null) navBar.SetActive(false);
            if (imagemExtra1 != null) imagemExtra1.gameObject.SetActive(false);
            if (imagemExtra2 != null) imagemExtra2.gameObject.SetActive(false);
            if (botao1 != null) botao1.SetActive(false);
            if (botao2 != null) botao2.SetActive(false);
        }
        else
        {
            if (navBar != null) navBar.SetActive(true);
            if (imagemExtra1 != null) imagemExtra1.gameObject.SetActive(true);
            if (imagemExtra2 != null) imagemExtra2.gameObject.SetActive(true);
            if (botao1 != null) botao1.SetActive(true);
            if (botao2 != null) botao2.SetActive(true);
        }
    }
}
