using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Botão no canto superior esquerdo + painel flutuante de configurações.
/// O prefab fica em Assets/Resources/MenuConfiguracoes.prefab e é criado
/// automaticamente em TODAS as cenas pelo Inicializador (não precisa arrastar nas cenas).
/// </summary>
public class MenuConfiguracoes : MonoBehaviour
{
    public static bool Aberto { get; private set; }

    [Header("Abrir / fechar")]
    public Button botaoAbrir;
    public GameObject painel;          // raiz do overlay (fundo escuro + janela)
    public Button fundoEscuro;         // clicar fora da janela fecha
    public Button botaoFechar;

    [Header("Áudio")]
    public Slider volume;
    public TextMeshProUGUI textoVolume;

    [Header("Vídeo")]
    public Toggle telaCheia;
    public TMP_Dropdown resolucao;
    public TMP_Dropdown qualidade;
    public Toggle vsync;

    [Header("Navegação")]
    public Button botaoVoltarMenu;
    public Button botaoSair;

    private const string K_VOLUME = "cfg_volume";
    private const string K_TELA_CHEIA = "cfg_telaCheia";
    private const string K_RES_W = "cfg_resW";
    private const string K_RES_H = "cfg_resH";
    private const string K_QUALIDADE = "cfg_qualidade";
    private const string K_VSYNC = "cfg_vsync";

    private readonly List<Vector2Int> resolucoes = new List<Vector2Int>();
    private EventSystem meuEventSystem;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico() => Aberto = false;

    void Awake()
    {
        AplicarPreferenciasSalvas();
        CriarEventSystemProprio();
    }

    void Start()
    {
        PreencherResolucoes();
        PreencherQualidade();
        SincronizarControles();

        botaoAbrir.onClick.AddListener(Alternar);
        if (fundoEscuro) fundoEscuro.onClick.AddListener(Fechar);
        if (botaoFechar) botaoFechar.onClick.AddListener(Fechar);

        volume.onValueChanged.AddListener(MudarVolume);
        telaCheia.onValueChanged.AddListener(MudarTelaCheia);
        resolucao.onValueChanged.AddListener(MudarResolucao);
        qualidade.onValueChanged.AddListener(MudarQualidade);
        vsync.onValueChanged.AddListener(MudarVsync);

        if (botaoVoltarMenu) botaoVoltarMenu.onClick.AddListener(VoltarAoMenu);
        if (botaoSair) botaoSair.onClick.AddListener(SairDoJogo);

        Fechar();
        SceneManager.sceneLoaded += AoCarregarCena;
        AoCarregarCena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        if (Aberto) Aberto = false;
    }

    void Update()
    {
        if (Teclas.EscApertado()) Alternar();
    }

    // ===================== Abrir / fechar =====================

    public void Alternar()
    {
        if (Aberto) Fechar(); else Abrir();
    }

    public void Abrir()
    {
        SincronizarControles();
        painel.SetActive(true);
        Aberto = true;
    }

    public void Fechar()
    {
        painel.SetActive(false);
        Aberto = false;
        PlayerPrefs.Save();
    }

    // ===================== Preferências =====================

    private void AplicarPreferenciasSalvas()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(K_VOLUME, 1f);

        if (PlayerPrefs.HasKey(K_QUALIDADE))
            QualitySettings.SetQualityLevel(
                Mathf.Clamp(PlayerPrefs.GetInt(K_QUALIDADE), 0, QualitySettings.names.Length - 1), true);

        if (PlayerPrefs.HasKey(K_VSYNC))
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(K_VSYNC);

        if (!Application.isEditor && PlayerPrefs.HasKey(K_TELA_CHEIA))
        {
            var modo = PlayerPrefs.GetInt(K_TELA_CHEIA) == 1
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            int w = PlayerPrefs.GetInt(K_RES_W, Screen.currentResolution.width);
            int h = PlayerPrefs.GetInt(K_RES_H, Screen.currentResolution.height);
            Screen.SetResolution(w, h, modo);
        }
    }

    private void SincronizarControles()
    {
        volume.SetValueWithoutNotify(AudioListener.volume);
        AtualizarTextoVolume(AudioListener.volume);
        telaCheia.SetIsOnWithoutNotify(Screen.fullScreenMode != FullScreenMode.Windowed);
        qualidade.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
        vsync.SetIsOnWithoutNotify(QualitySettings.vSyncCount > 0);
        resolucao.SetValueWithoutNotify(IndiceResolucaoAtual());
    }

    // ===================== Áudio =====================

    private void MudarVolume(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(K_VOLUME, v);
        AtualizarTextoVolume(v);
    }

    private void AtualizarTextoVolume(float v)
    {
        if (textoVolume) textoVolume.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    // ===================== Vídeo =====================

    private void MudarTelaCheia(bool cheia)
    {
        Screen.fullScreenMode = cheia ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        PlayerPrefs.SetInt(K_TELA_CHEIA, cheia ? 1 : 0);
    }

    private void PreencherResolucoes()
    {
        resolucoes.Clear();
        foreach (var r in Screen.resolutions)
        {
            var v = new Vector2Int(r.width, r.height);
            if (!resolucoes.Contains(v)) resolucoes.Add(v);
        }
        if (resolucoes.Count == 0)
            resolucoes.Add(new Vector2Int(Screen.width, Screen.height));

        var opcoes = new List<string>();
        foreach (var v in resolucoes) opcoes.Add($"{v.x} × {v.y}");
        resolucao.ClearOptions();
        resolucao.AddOptions(opcoes);
    }

    private int IndiceResolucaoAtual()
    {
        int melhor = 0;
        int menorDiferenca = int.MaxValue;
        for (int i = 0; i < resolucoes.Count; i++)
        {
            int d = Mathf.Abs(resolucoes[i].x - Screen.width) + Mathf.Abs(resolucoes[i].y - Screen.height);
            if (d < menorDiferenca) { menorDiferenca = d; melhor = i; }
        }
        return melhor;
    }

    private void MudarResolucao(int indice)
    {
        if (indice < 0 || indice >= resolucoes.Count) return;
        var r = resolucoes[indice];
        Screen.SetResolution(r.x, r.y, Screen.fullScreenMode);
        PlayerPrefs.SetInt(K_RES_W, r.x);
        PlayerPrefs.SetInt(K_RES_H, r.y);
    }

    private void PreencherQualidade()
    {
        qualidade.ClearOptions();
        qualidade.AddOptions(new List<string>(QualitySettings.names));
    }

    private void MudarQualidade(int indice)
    {
        QualitySettings.SetQualityLevel(indice, true);
        PlayerPrefs.SetInt(K_QUALIDADE, indice);
        // Alguns níveis de qualidade mudam o vSync; mantém a escolha do jogador
        QualitySettings.vSyncCount = vsync.isOn ? 1 : 0;
    }

    private void MudarVsync(bool ligado)
    {
        QualitySettings.vSyncCount = ligado ? 1 : 0;
        PlayerPrefs.SetInt(K_VSYNC, QualitySettings.vSyncCount);
    }

    // ===================== Navegação =====================

    private void VoltarAoMenu()
    {
        Fechar();
        SceneManager.LoadScene(MenuPrincipal.NomeCenaMenu);
    }

    private void SairDoJogo()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (botaoVoltarMenu)
            botaoVoltarMenu.gameObject.SetActive(cena.name != MenuPrincipal.NomeCenaMenu);
        if (Aberto) Fechar();
        AtualizarEventSystem();
    }

    // ===================== EventSystem =====================
    // Sem EventSystem os botões não respondem. Mantemos um próprio e
    // desligamos ele quando a cena já tiver o dela (evita aviso de duplicado).

    private void CriarEventSystemProprio()
    {
        var go = new GameObject("EventSystem (MenuConfiguracoes)");
        go.SetActive(false);   // só liga se a cena não tiver EventSystem próprio
        go.transform.SetParent(transform, false);
        meuEventSystem = go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    private void AtualizarEventSystem()
    {
        if (meuEventSystem == null) return;
        bool outroNaCena = false;
        var todos = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var es in todos)
            if (es != meuEventSystem) { outroNaCena = true; break; }
        meuEventSystem.gameObject.SetActive(!outroNaCena);
    }
}
