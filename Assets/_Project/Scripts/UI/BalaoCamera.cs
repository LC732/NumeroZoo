using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Balão flutuante que pergunta se o jogador permite o uso da câmera.
/// Enquanto não houver resposta, o conteúdo do menu fica bloqueado.
/// </summary>
public class BalaoCamera : MonoBehaviour
{
    [Header("Referências")]
    public RectTransform balao;
    public CanvasGroup grupoBalao;
    public Button botaoPermitir;
    public Button botaoAgoraNao;

    [Tooltip("Botões do menu principal — ficam bloqueados até o jogador responder.")]
    public CanvasGroup conteudoBloqueado;

    [Header("Animação")]
    public float amplitudeFlutuacao = 10f;
    public float velocidadeFlutuacao = 1.6f;
    public float duracaoEntrada = 0.35f;

    private Vector2 posicaoBase;
    private bool fechando;

    void Start()
    {
        botaoPermitir.onClick.AddListener(() => Responder(true));
        botaoAgoraNao.onClick.AddListener(() => Responder(false));

        if (ConsentimentoCamera.Respondido)
        {
            // Já respondeu nesta sessão (ex.: voltou de uma cena)
            Liberar();
            gameObject.SetActive(false);
            return;
        }

        posicaoBase = balao.anchoredPosition;
        Bloquear();
        StartCoroutine(Entrar());
    }

    void Update()
    {
        if (fechando) return;
        float y = Mathf.Sin(Time.unscaledTime * velocidadeFlutuacao) * amplitudeFlutuacao;
        balao.anchoredPosition = posicaoBase + new Vector2(0f, y);
    }

    private IEnumerator Entrar()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracaoEntrada;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);   // ease-out
            grupoBalao.alpha = e;
            balao.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);
            yield return null;
        }
    }

    private IEnumerator Sair()
    {
        fechando = true;
        float t = 0f;
        Vector3 escalaInicial = balao.localScale;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.2f;
            grupoBalao.alpha = 1f - t;
            balao.localScale = escalaInicial * Mathf.Lerp(1f, 0.9f, t);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    private void Responder(bool permitir)
    {
        if (fechando) return;
        ConsentimentoCamera.Responder(permitir);
        Liberar();
        StartCoroutine(Sair());
    }

    private void Bloquear()
    {
        if (conteudoBloqueado == null) return;
        conteudoBloqueado.interactable = false;
        conteudoBloqueado.alpha = 0.35f;
    }

    private void Liberar()
    {
        if (conteudoBloqueado == null) return;
        conteudoBloqueado.interactable = true;
        conteudoBloqueado.alpha = 1f;
    }
}
