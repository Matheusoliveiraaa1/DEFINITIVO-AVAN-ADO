using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

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

    // Armazena as imagens salvas em tempo de execução
    private Dictionary<string, Texture2D> savedImages = new Dictionary<string, Texture2D>();

    private void Start()
    {
        AtualizarMiniaturas(); // Carrega imagens do disco e atualiza slots
    }

    public void SaveImage(string areaName, Texture2D image)
    {
        savedImages[areaName] = image;

        // Atualiza miniatura
        foreach (var slot in slots)
        {
            if (slot.areaName == areaName)
            {
                slot.slotImage.texture = image;

                Button slotButton = slot.slotImage.GetComponent<Button>();
                if (slotButton != null)
                {
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.onClick.AddListener(() => OpenFullScreen(areaName));
                }
                break;
            }
        }

        // SALVAR IMAGEM EM DISCO (APENAS NO CACHE INTERNO)
        string path = GetImageFilePath(areaName);
        byte[] bytes = image.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
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
            string path = GetImageFilePath(slot.areaName);

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);

                slot.slotImage.texture = tex;
                savedImages[slot.areaName] = tex;
            }
            else
            {
                slot.slotImage.texture = slot.defaultTexture;
            }

            // Garante que botão funcione
            Button slotButton = slot.slotImage.GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.onClick.RemoveAllListeners();
                string areaName = slot.areaName;
                slotButton.onClick.AddListener(() => OpenFullScreen(areaName));
            }
        }
    }

    // Agora salva no CACHE INTERNO (limpado ao desinstalar)
    private string GetImageFilePath(string areaName)
    {
        return Path.Combine(Application.temporaryCachePath, $"{areaName}_photo.png");
    }
}
