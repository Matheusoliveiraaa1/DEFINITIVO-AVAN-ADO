using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GalleryManager : MonoBehaviour
{
    [System.Serializable]
    public class AreaSlot
    {
        public string areaName;           // Nome da área (ex: "Area1")
        public RawImage slotImage;        // Miniatura no painel da galeria
        public Texture defaultTexture;    // Textura padrão caso não tenha foto
    }

    [Header("Slots da Galeria")]
    public List<AreaSlot> slots = new List<AreaSlot>();

    [Header("Painel de Tela Cheia")]
    public GameObject fullImagePanel;
    public RawImage fullImageDisplay;

    [Header("Painel da Galeria")]
    public GameObject galeriaPainel;

    // Armazena as imagens salvas por área
    private Dictionary<string, Texture2D> savedImages = new Dictionary<string, Texture2D>();

    private void Start()
    {
        AtualizarMiniaturas(); // Preenche os slots ao iniciar (caso tenha algo salvo)
    }

    public void SaveImage(string areaName, Texture2D image)
    {
        foreach (var slot in slots)
        {
            if (slot.areaName == areaName)
            {
                slot.slotImage.texture = image;
                savedImages[areaName] = image;

                // Remove todos os ouvintes antigos e adiciona o correto
                Button slotButton = slot.slotImage.GetComponent<Button>();
                if (slotButton != null)
                {
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.onClick.AddListener(() => OpenFullScreen(areaName));
                }
                return;
            }
        }
    }

    public void OpenFullScreen(string areaName)
    {
        if (savedImages.ContainsKey(areaName))
        {
            fullImageDisplay.texture = savedImages[areaName];
            fullImagePanel.SetActive(true);
        }
    }

    public void CloseFullScreen()
    {
        fullImagePanel.SetActive(false);
    }

    public void AbrirGaleria()
    {
        AtualizarMiniaturas();
        galeriaPainel.SetActive(true);
    }

    public void FecharGaleria()
    {
        galeriaPainel.SetActive(false);
    }

    private void AtualizarMiniaturas()
    {
        foreach (var slot in slots)
        {
            if (savedImages.ContainsKey(slot.areaName))
            {
                slot.slotImage.texture = savedImages[slot.areaName];
            }
            else
            {
                slot.slotImage.texture = slot.defaultTexture;
            }

            // Garante que o botão funcione
            Button slotButton = slot.slotImage.GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.onClick.RemoveAllListeners();
                string areaName = slot.areaName;
                slotButton.onClick.AddListener(() => OpenFullScreen(areaName));
            }
        }
    }
}
