using UnityEngine;
using UnityEngine.UI;

public class PhotoAreaOverlay : MonoBehaviour
{
    public static PhotoAreaOverlay Instance;

    [Header("UI Elements")]
    public GameObject overlayPanel;
    public Image photo1;
    public Image photo2;
    public Image photo3;
    public Button okButton;

    private void Awake()
    {
        Instance = this;

        overlayPanel.SetActive(false);
        okButton.onClick.AddListener(Hide);
    }

    public static void Show(Sprite img1 = null, Sprite img2 = null, Sprite img3 = null)
    {
        if (Instance == null) return;

        Instance.overlayPanel.SetActive(true);

        if (img1 != null) Instance.photo1.sprite = img1;
        if (img2 != null) Instance.photo2.sprite = img2;
        if (img3 != null) Instance.photo3.sprite = img3;
    }

    public void Hide()
    {
        overlayPanel.SetActive(false);

        // Libera o vídeo
        VideoPlayState.IsAuthorized = true;

        // Se já estamos na tela exploração, força a checagem
        var nav = FindObjectOfType<NavigationManager>();
        if (nav != null && nav.currentState == NavigationManager.AppState.Exploracao)
        {
            nav.TryPlayExploracaoVideo();
        }
    }
}