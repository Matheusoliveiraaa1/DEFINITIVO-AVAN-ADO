using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class GalleryManager : MonoBehaviour
{



    [System.Serializable]
    public class AreaSlot
    {
        public string areaName;
        public RawImage slotImage;
    }

    [Header("Slots da Galeria")]
    public List<AreaSlot> slots = new List<AreaSlot>();

    [Header("Painel de Tela Cheia")]
    public GameObject fullImagePanel;
    public RawImage fullImageDisplay;

    [Header("Painel da Galeria")]
    public GameObject galeriaPainel;

    // ✅ NÃO armazenamos mais Textures na RAM
    private Dictionary<string, string> savedImagePaths = new Dictionary<string, string>();

    private Texture2D currentFullTexture; // ✅ controle de memória

    void Awake()
    {
        if (!Application.isPlaying)
            return;
    }




    private void Start()
    {
        AtualizarMiniaturas();
    }

    public void SaveImage(string areaName, Texture2D image)
    {
        string path = GetImageFilePath(areaName);

        byte[] bytes = image.EncodeToJPG(80); // ✅ muito mais leve que PNG
        File.WriteAllBytes(path, bytes);

        savedImagePaths[areaName] = path;

        AtualizarMiniaturas();
    }

    public void OpenFullScreen(string areaName)
    {
        string path = GetImageFilePath(areaName);
        if (!File.Exists(path)) return;

        // ✅ DESTROI textura anterior antes de abrir nova
        if (currentFullTexture != null)
        {
            Destroy(currentFullTexture);
            currentFullTexture = null;
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        byte[] bytes = File.ReadAllBytes(path);
        currentFullTexture = new Texture2D(2, 2);
        currentFullTexture.LoadImage(bytes);

        fullImageDisplay.texture = currentFullTexture;
        fullImagePanel.SetActive(true);
    }

    public void CloseFullScreen()
    {
        fullImagePanel.SetActive(false);

        // ✅ LIBERA a textura da tela cheia
        if (currentFullTexture != null)
        {
            Destroy(currentFullTexture);
            currentFullTexture = null;
            fullImageDisplay.texture = null;
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
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

    public void AtualizarMiniaturas()
    {
        foreach (var slot in slots)
        {
            string path = GetImageFilePath(slot.areaName);

            // ✅ DESTROI miniatura antiga antes de criar nova
            if (slot.slotImage.texture != null)
            {
                Destroy(slot.slotImage.texture);
                slot.slotImage.texture = null;
            }

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);

                slot.slotImage.texture = tex;
            }

            Button slotButton = slot.slotImage.GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.onClick.RemoveAllListeners();
                string areaName = slot.areaName;
                slotButton.onClick.AddListener(() => OpenFullScreen(areaName));
            }
        }

        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    private string GetImageFilePath(string areaName)
    {
        return Path.Combine(Application.temporaryCachePath, $"{areaName}_photo.jpg");
    }
}
