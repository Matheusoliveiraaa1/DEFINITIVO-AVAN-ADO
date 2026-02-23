using UnityEngine;
using UnityEngine.UI; // Necessário para lidar com componentes de Image

public class SpriteAnimationManager : MonoBehaviour
{
    [Header("Configurações da Imagem")]
    public Image targetImage; // Arraste o objeto da Imagem aqui
    public Sprite[] animationSprites; // Coloque todos os seus sprites aqui
    public float framesPerSecond = 10f; // Velocidade da animação

    [Header("Configurações de Detecção")]
    public string[] tagsToWatch; // Ex: "Inimigo", "Player", etc.

    private int currentFrame;
    private float timer;

    void Update()
    {
        bool shouldHide = CheckForObjects();

        if (shouldHide)
        {
            // Esconde a imagem se encontrar as tags
            if (targetImage.gameObject.activeSelf)
                targetImage.gameObject.SetActive(false);
        }
        else
        {
            // Mostra a imagem e roda a animação
            if (!targetImage.gameObject.activeSelf)
                targetImage.gameObject.SetActive(true);

            PlayAnimation();
        }
    }

    void PlayAnimation()
    {
        if (animationSprites.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;
            currentFrame = (currentFrame + 1) % animationSprites.Length;
            targetImage.sprite = animationSprites[currentFrame];
        }
    }

    bool CheckForObjects()
    {
        foreach (string tag in tagsToWatch)
        {
            // Tenta encontrar pelo menos um objeto com a tag na cena
            if (GameObject.FindWithTag(tag) != null)
            {
                return true;
            }
        }
        return false;
    }
}