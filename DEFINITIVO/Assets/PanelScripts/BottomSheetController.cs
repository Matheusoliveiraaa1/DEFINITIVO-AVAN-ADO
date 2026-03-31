using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BottomSheetController : MonoBehaviour, IDragHandler
{
    [Header("Referências")]
    public RectTransform cardRect;           // O RectTransform do próprio card branco
    public ScrollRect scrollView;            // O Scroll View das informações
    public RectTransform areaDeArrastoTopo;  // A imagem invisível no topo do card

    [Header("Configurações de Posição (Eixo Y)")]
    public float yMinimizado = -600f; // Ajuste para a altura que o card deve ficar escondido
    public float yExpandido = 0f;     // Posição quando ocupa a tela toda

    [Header("Suavização")]
    public float velocidadeAnimacao = 12f;

    private Vector2 posicaoAlvo;
    private bool estaExpandido = false;

    void Start()
    {
        // Define a posição inicial como minimizada
        posicaoAlvo = new Vector2(cardRect.anchoredPosition.x, yMinimizado);

        // O Scroll View começa desativado para o usuário não rolar a lista enquanto o card tá pequeno
        scrollView.enabled = false;

        // Adiciona um "espião" para ver quando a lista é rolada
        scrollView.onValueChanged.AddListener(VerificarOverscroll);
    }

    void Update()
    {
        // Interpolação suave (Lerp) para o movimento do card ficar natural e não seco
        cardRect.anchoredPosition = Vector2.Lerp(cardRect.anchoredPosition, posicaoAlvo, Time.deltaTime * velocidadeAnimacao);
    }

    // Essa interface IDragHandler detecta o dedo arrastando na tela
    public void OnDrag(PointerEventData eventData)
    {
        if (estaExpandido) return; // Se já está em tela cheia, o Scroll View é quem manda no toque

        // Verifica se o dedo está tocando especificamente na área do topo do card
        if (RectTransformUtility.RectangleContainsScreenPoint(areaDeArrastoTopo, eventData.position, eventData.pressEventCamera))
        {
            // Se arrastou pra cima (delta Y positivo), abre o card
            if (eventData.delta.y > 0)
            {
                AbrirCard();
            }
        }
    }

    private void VerificarOverscroll(Vector2 pos)
    {
        // O segredo: como o ScrollRect é "Elastic", quando o usuário puxa a lista
        // além do limite do topo, o valor de 'y' passa de 1.0. 
        // 1.10f é um bom limite para o usuário ter que fazer uma leve força pra fechar.
        if (estaExpandido && pos.y > 1.10f)
        {
            FecharCard();
        }
    }

    public void AbrirCard()
    {
        estaExpandido = true;
        posicaoAlvo = new Vector2(cardRect.anchoredPosition.x, yExpandido);
        scrollView.enabled = true; // Libera o conteúdo para o jogador rolar
    }

    public void FecharCard()
    {
        estaExpandido = false;
        posicaoAlvo = new Vector2(cardRect.anchoredPosition.x, yMinimizado);

        scrollView.enabled = false; // Trava a lista
        scrollView.verticalNormalizedPosition = 1f; // Reseta a lista pro topo para a próxima vez que abrir
    }
}