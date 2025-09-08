using UnityEngine;
using UnityEngine.UI;

public class StickerSimulationButton : MonoBehaviour
{
    public string testAreaName = "AreaTeste"; // Nome de uma área válida
    public int testStickerIndex = 0; // Índice de sticker para simular

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(SimulateStickerCollection);
    }

    public void SimulateStickerCollection()
    {
        Debug.Log("[TESTE] Simulando coleta de sticker...");

        // Pega o NativeCameraExample para achar o sprite certo
        NativeCameraExample cameraExample = FindObjectOfType<NativeCameraExample>();
        if (cameraExample != null)
        {
            Sprite stickerSprite = cameraExample.GetStickerSprite(testAreaName, testStickerIndex);
            if (stickerSprite != null)
            {
                // Mostra o overlay do sticker normalmente (mesmo fluxo do LocationServiceManager)
                PhotoAreaOverlay.ShowSticker(stickerSprite);
            }
            else
            {
                Debug.LogWarning("[TESTE] Nenhum sprite encontrado para área " + testAreaName + " e index " + testStickerIndex);
            }
        }
        else
        {
            Debug.LogWarning("[TESTE] Nenhum NativeCameraExample encontrado na cena!");
        }
    }
}
