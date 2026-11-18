using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu "Ambientes > Gerar Menu Principal e Configurações".
/// Cria:
///   • Assets/Resources/MenuConfiguracoes.prefab  (botão ☰ + painel flutuante)
///   • Assets/Scenes/MenuPrincipal.unity          (balão da câmera + escolha das 3 cenas)
/// e coloca o MenuPrincipal como a primeira cena do Build.
/// Pode rodar de novo quando quiser: ele sobrescreve os dois.
/// </summary>
public static class GeradorMenus
{
    private const string CaminhoPrefab = "Assets/_Project/Resources/MenuConfiguracoes.prefab";
    private const string CaminhoCena = "Assets/_Project/Scenes/MenuPrincipal.unity";

    // ---------- Paleta (noite no cerrado + ipê amarelo) ----------
    private static readonly Color CorFundo   = Hex("#10201A");
    private static readonly Color CorPainel  = Hex("#172821");
    private static readonly Color CorCartao  = Hex("#1D3329");
    private static readonly Color CorCampo   = Hex("#E8EFEA");
    private static readonly Color CorTexto   = Hex("#F2F5F0");
    private static readonly Color CorSuave   = Hex("#9DB3A6");
    private static readonly Color CorEscuro  = Hex("#13221C");
    private static readonly Color CorAcento  = Hex("#F2B33D");
    private static readonly Color CorBalao   = Hex("#FFF8EC");
    private static readonly Color CorLinha   = Hex("#2A4337");
    private static readonly Color CorPerigo  = Hex("#3A2622");
    private static readonly Color CorPerigoT = Hex("#F3A48E");

    // (nome do arquivo .unity, nome que aparece no cartão) — na ordem dos cartões
    private static readonly (string arquivo, string exibicao)[] Ambientes =
    {
        ("Aquario", "Aquário"),
        ("Jardin",  "Jardim"),
        ("Cerrado", "Cerrado"),
    };

    private static readonly Color[] CoresAmbientes =
    {
        Hex("#4FB3D9"),   // Aquário
        Hex("#F2B33D"),   // Jardim
        Hex("#8CC63F"),   // Cerrado
    };

    private static string AcharCena(string nome)
    {
        foreach (var guid in AssetDatabase.FindAssets(nome + " t:Scene"))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(caminho) == nome) return caminho;
        }
        return null;
    }

    // ---------- Sprites embutidos da Unity ----------
    private static Sprite SpriteUI    => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    private static Sprite SpriteFundo => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
    private static Sprite SpriteKnob  => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    private static Sprite SpriteCheck => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
    private static Sprite SpriteSeta  => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
    private static Sprite SpriteMask  => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
    private static Sprite SpriteInput => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");

    private static TMP_FontAsset fonte;

    // =====================================================================
    [MenuItem("Ambientes/Gerar Menu Principal e Configurações")]
    public static void GerarTudo()
    {
        try { fonte = TMP_Settings.defaultFontAsset; } catch { fonte = null; }
        if (fonte == null)
        {
            EditorUtility.DisplayDialog("TextMeshPro",
                "Importe os recursos do TextMeshPro antes:\nWindow > TextMeshPro > Import TMP Essential Resources",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(CaminhoCena) &&
            !EditorUtility.DisplayDialog("Gerar menus",
                "MenuPrincipal.unity e MenuConfiguracoes.prefab já existem e serão substituídos. Continuar?",
                "Substituir", "Cancelar"))
            return;

        // Procura as 3 cenas do projeto pelo nome (em qualquer pasta)
        var cenasJogo = new List<string>();
        var caminhosJogo = new List<string>();
        foreach (var (arquivo, _) in Ambientes)
        {
            string caminho = AcharCena(arquivo);
            if (caminho == null)
            {
                Debug.LogWarning($"Cena '{arquivo}.unity' não encontrada — o cartão vai ficar sem ação.");
                cenasJogo.Add("");
                continue;
            }
            cenasJogo.Add(arquivo);
            caminhosJogo.Add(caminho);
        }

        CriarPasta("Assets", "_Project");
        CriarPasta("Assets/_Project", "Resources");
        CriarPasta("Assets/_Project", "Scenes");

        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1) Prefab de configurações (montado aqui e salvo como asset)
        var menuCfg = MontarMenuConfiguracoes();
        PrefabUtility.SaveAsPrefabAsset(menuCfg, CaminhoPrefab);
        Object.DestroyImmediate(menuCfg);

        // 2) Cena do menu principal
        MontarCenaMenu(cenasJogo);
        EditorSceneManager.SaveScene(cena, CaminhoCena);

        // 3) Build: menu primeiro, depois as 3 cenas (as demais continuam lá, como estavam)
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == CaminhoCena || caminhosJogo.Contains(s.path));
        lista.Insert(0, new EditorBuildSettingsScene(CaminhoCena, true));
        for (int i = 0; i < caminhosJogo.Count; i++)
            lista.Insert(1 + i, new EditorBuildSettingsScene(caminhosJogo[i], true));
        EditorBuildSettings.scenes = lista.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("✔ Menu principal e prefab de configurações gerados.");
    }

    // =====================================================================
    //  PREFAB: botão ☰ no canto superior esquerdo + painel de configurações
    // =====================================================================
    private static GameObject MontarMenuConfiguracoes()
    {
        var raiz = CriarCanvas("MenuConfiguracoes", 100);
        var cfg = raiz.gameObject.AddComponent<MenuConfiguracoes>();

        // ---- Botão ☰ (três barras) ----
        var botaoAbrir = Botao(raiz, "BotaoAbrir", "", WithAlpha(CorEscuro, 0.85f), CorTexto, 1, 0.35f);
        var rtAbrir = (RectTransform)botaoAbrir.transform;
        Ancorar(rtAbrir, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(76, 76));
        Object.DestroyImmediate(rtAbrir.Find("Texto").gameObject);
        for (int i = -1; i <= 1; i++)
        {
            var barra = Novo("Barra", rtAbrir);
            Ancorar(barra, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0, i * 13), new Vector2(38, 5));
            Img(barra, CorTexto, SpriteUI).raycastTarget = false;
        }

        // ---- Painel (fundo escuro + janela) ----
        var painel = Novo("Painel", raiz);
        Esticar(painel);

        var fundo = Novo("FundoEscuro", painel);
        Esticar(fundo);
        Img(fundo, new Color(0, 0, 0, 0.55f));
        var botaoFundo = fundo.gameObject.AddComponent<Button>();
        botaoFundo.transition = Selectable.Transition.None;
        SemNavegacao(botaoFundo);

        var janela = Novo("Janela", painel);
        Ancorar(janela, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), Vector2.zero, new Vector2(700, 600));
        Img(janela, CorPainel, SpriteUI).pixelsPerUnitMultiplier = 0.2f;
        var vlg = janela.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(44, 44, 36, 40);
        vlg.spacing = 14;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var fit = janela.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Cabeçalho
        var cab = Linha(janela, "Cabecalho", null, 60);
        var titulo = Txt(cab, "Titulo", "Configurações", 40, CorTexto, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        Layout(titulo.gameObject, flexW: 1, prefH: 60);
        var botaoFechar = Botao(cab, "BotaoFechar", "X", CorCartao, CorTexto, 26, 0.5f);
        Layout(botaoFechar.gameObject, prefW: 52, prefH: 52);

        Divisor(janela);

        // Áudio
        Secao(janela, "ÁUDIO");
        var linhaVol = Linha(janela, "Volume", "Volume");
        var slider = CriarSlider(linhaVol);
        Layout(slider.gameObject, flexW: 1, prefW: 100, prefH: 28);
        var textoVol = Txt(linhaVol, "Valor", "100%", 24, CorSuave, TextAlignmentOptions.MidlineRight);
        Layout(textoVol.gameObject, prefW: 80, prefH: 56);

        // Vídeo
        Secao(janela, "VÍDEO");
        var linhaTela = Linha(janela, "TelaCheia", "Tela cheia");
        var toggleTela = CriarToggle(linhaTela);

        var linhaRes = Linha(janela, "Resolucao", "Resolução");
        var ddRes = CriarDropdown(linhaRes);

        var linhaQual = Linha(janela, "Qualidade", "Qualidade");
        var ddQual = CriarDropdown(linhaQual);

        var linhaVsync = Linha(janela, "VSync", "V-Sync");
        var toggleVsync = CriarToggle(linhaVsync);

        Divisor(janela);

        // Navegação
        var linhaBotoes = Novo("Botoes", janela);
        var hlg = linhaBotoes.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;
        Layout(linhaBotoes.gameObject, prefH: 62);

        var botaoVoltar = Botao(linhaBotoes, "VoltarMenu", "Voltar ao menu", CorAcento, CorEscuro, 24);
        Layout(botaoVoltar.gameObject, flexW: 1, prefH: 62);
        var botaoSair = Botao(linhaBotoes, "Sair", "Sair do jogo", CorPerigo, CorPerigoT, 24);
        Layout(botaoSair.gameObject, flexW: 1, prefH: 62);

        // Dica
        var dica = Txt(janela, "Dica", "Esc abre e fecha este menu  ·  Teclas 0–9 mudam a quantidade de animais",
            18, CorSuave, TextAlignmentOptions.Center);
        Layout(dica.gameObject, prefH: 30);

        // ---- Liga as referências no componente ----
        cfg.botaoAbrir = botaoAbrir;
        cfg.painel = painel.gameObject;
        cfg.fundoEscuro = botaoFundo;
        cfg.botaoFechar = botaoFechar;
        cfg.volume = slider;
        cfg.textoVolume = textoVol;
        cfg.telaCheia = toggleTela;
        cfg.resolucao = ddRes;
        cfg.qualidade = ddQual;
        cfg.vsync = toggleVsync;
        cfg.botaoVoltarMenu = botaoVoltar;
        cfg.botaoSair = botaoSair;

        painel.gameObject.SetActive(false);
        return raiz.gameObject;
    }

    // =====================================================================
    //  CENA: Menu principal
    // =====================================================================
    private static void MontarCenaMenu(List<string> cenasJogo)
    {
        // Câmera (com AudioListener para o volume funcionar no menu)
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        var c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = CorFundo;
        c.orthographic = true;

        CriarEventSystem();

        // Servidor Python (persistente; configure porta/câmera no Inspector)
        new GameObject("ServidorPython", typeof(ServidorPython));

        var canvas = CriarCanvas("CanvasMenu", 0);
        var menu = canvas.gameObject.AddComponent<MenuPrincipal>();

        // Fundo com "manchas" suaves
        var fundo = Novo("Fundo", canvas);
        Esticar(fundo);
        Img(fundo, CorFundo);
        Mancha(fundo, new Vector2(-760, 380), 900, WithAlpha(CoresAmbientes[2], 0.07f));
        Mancha(fundo, new Vector2(820, -420), 1000, WithAlpha(CoresAmbientes[0], 0.06f));
        Mancha(fundo, new Vector2(700, 460), 520, WithAlpha(CorAcento, 0.06f));

        // Conteúdo (fica bloqueado até responder o balão)
        var conteudo = Novo("Conteudo", canvas);
        Esticar(conteudo);
        var grupoConteudo = conteudo.gameObject.AddComponent<CanvasGroup>();

        var titulo = Txt(conteudo, "Titulo", "Ambientes Interativos", 92, CorTexto, TextAlignmentOptions.Center, FontStyles.Bold);
        Ancorar(titulo.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), new Vector2(0, -150), new Vector2(0, 120));

        var sub = Txt(conteudo, "Subtitulo", "Escolha um ambiente e levante os dedos para chamar os animais",
            32, CorSuave, TextAlignmentOptions.Center);
        Ancorar(sub.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), new Vector2(0, -275), new Vector2(0, 50));

        // Cartões das cenas
        var cartoes = Novo("Cartoes", conteudo);
        Ancorar(cartoes, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1320, 360));
        var hlg = cartoes.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 40;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < 3; i++)
        {
            string nomeCena = i < cenasJogo.Count ? cenasJogo[i] : "";
            string exibicao = Ambientes[i].exibicao;
            var cartao = Cartao(cartoes, i, exibicao, CoresAmbientes[i % CoresAmbientes.Length]);
            if (nomeCena != "")
                UnityEventTools.AddStringPersistentListener(cartao.onClick,
                    new UnityAction<string>(menu.CarregarCena), nomeCena);
        }

        // Botão sair
        var sair = Botao(conteudo, "BotaoSair", "Sair", CorCartao, CorTexto, 28);
        Ancorar((RectTransform)sair.transform, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), new Vector2(0, 120), new Vector2(260, 66));
        UnityEventTools.AddPersistentListener(sair.onClick, new UnityAction(menu.Sair));

        var rodape = Txt(conteudo, "Rodape", "Teclas 0–9 também controlam os animais  ·  Esc abre as configurações",
            22, CorSuave, TextAlignmentOptions.Center);
        Ancorar(rodape.rectTransform, V(0, 0), V(1, 0), V(0.5f, 0), new Vector2(0, 50), new Vector2(0, 40));

        // Balão da câmera (por cima de tudo)
        MontarBalaoCamera(canvas, grupoConteudo);
    }

    private static void MontarBalaoCamera(RectTransform canvas, CanvasGroup conteudo)
    {
        var overlay = Novo("BalaoCamera", canvas);
        Esticar(overlay);
        Img(overlay, new Color(0, 0, 0, 0.55f));        // bloqueia cliques no menu
        var grupo = overlay.gameObject.AddComponent<CanvasGroup>();
        var comp = overlay.gameObject.AddComponent<BalaoCamera>();

        var balao = Novo("Balao", overlay);
        Ancorar(balao, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0, 30), new Vector2(780, 420));
        Img(balao, CorBalao, SpriteUI).pixelsPerUnitMultiplier = 0.15f;
        var vlg = balao.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(56, 56, 44, 48);
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var fit = balao.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // "Rabinho" do balão
        var rabo = Novo("Rabo", balao);
        Ancorar(rabo, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46));
        rabo.localRotation = Quaternion.Euler(0, 0, 45);
        Img(rabo, CorBalao).raycastTarget = false;
        rabo.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        // Ícone de câmera desenhado com formas
        var icone = Novo("Icone", balao);
        Layout(icone.gameObject, prefH: 84);
        var corpo = Novo("Corpo", icone);
        Ancorar(corpo, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0, -4), new Vector2(110, 72));
        Img(corpo, CorEscuro, SpriteUI).pixelsPerUnitMultiplier = 0.4f;
        var topo = Novo("Topo", icone);
        Ancorar(topo, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(-22, 36), new Vector2(40, 14));
        Img(topo, CorEscuro, SpriteUI);
        var lente = Novo("Lente", icone);
        Ancorar(lente, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0, -4), new Vector2(46, 46));
        Img(lente, CorBalao, SpriteKnob, false);
        var pupila = Novo("Pupila", icone);
        Ancorar(pupila, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0, -4), new Vector2(24, 24));
        Img(pupila, CorAcento, SpriteKnob, false);
        foreach (var img in icone.GetComponentsInChildren<Image>()) img.raycastTarget = false;

        var titulo = Txt(balao, "Titulo", "Podemos usar sua câmera?", 44, CorEscuro, TextAlignmentOptions.Center, FontStyles.Bold);
        Layout(titulo.gameObject, prefH: 56);
        var texto = Txt(balao, "Texto",
            "A câmera serve para contar quantos dedos você levanta e fazer os animais aparecerem. " +
            "As imagens são processadas só neste computador.",
            26, Hex("#41534A"), TextAlignmentOptions.Center);
        Layout(texto.gameObject, prefH: 100);

        var botoes = Novo("Botoes", balao);
        var hlg = botoes.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 18;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;
        Layout(botoes.gameObject, prefH: 68);

        var nao = Botao(botoes, "AgoraNao", "Agora não", Hex("#E6DFD2"), Hex("#41534A"), 28);
        Layout(nao.gameObject, flexW: 1, prefH: 68);
        var sim = Botao(botoes, "Permitir", "Permitir", CorAcento, CorEscuro, 28);
        Layout(sim.gameObject, flexW: 1, prefH: 68);

        comp.balao = balao;
        comp.grupoBalao = grupo;
        comp.botaoPermitir = sim;
        comp.botaoAgoraNao = nao;
        comp.conteudoBloqueado = conteudo;
    }

    private static Button Cartao(Transform pai, int indice, string nome, Color cor)
    {
        var b = Botao(pai, "Cartao_" + (indice + 1), "", CorCartao, CorTexto, 1, 0.2f);
        var rt = (RectTransform)b.transform;
        Object.DestroyImmediate(rt.Find("Texto").gameObject);
        Layout(b.gameObject, prefW: 400, prefH: 340);

        var circulo = Novo("Circulo", rt);
        Ancorar(circulo, V(0.5f, 1), V(0.5f, 1), V(0.5f, 1), new Vector2(0, -44), new Vector2(124, 124));
        Img(circulo, cor, SpriteKnob, false).raycastTarget = false;
        var num = Txt(circulo, "Numero", (indice + 1).ToString(), 64, CorEscuro, TextAlignmentOptions.Center, FontStyles.Bold);
        Esticar(num.rectTransform);

        var t = Txt(rt, "Nome", nome, 40, CorTexto, TextAlignmentOptions.Center, FontStyles.Bold);
        Ancorar(t.rectTransform, V(0, 0), V(1, 0), V(0.5f, 0), new Vector2(0, 96), new Vector2(-40, 70));

        var entrar = Txt(rt, "Entrar", "ENTRAR", 22, cor, TextAlignmentOptions.Center, FontStyles.Bold);
        entrar.characterSpacing = 12;
        Ancorar(entrar.rectTransform, V(0, 0), V(1, 0), V(0.5f, 0), new Vector2(0, 44), new Vector2(0, 36));
        return b;
    }

    private static void Mancha(Transform pai, Vector2 pos, float tamanho, Color cor)
    {
        var m = Novo("Mancha", pai);
        Ancorar(m, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), pos, new Vector2(tamanho, tamanho));
        Img(m, cor, SpriteKnob, false).raycastTarget = false;
    }

    // =====================================================================
    //  Controles
    // =====================================================================
    private static Slider CriarSlider(Transform pai)
    {
        var res = new DefaultControls.Resources
        {
            standard = SpriteUI, background = SpriteFundo, knob = SpriteKnob,
            checkmark = SpriteCheck, dropdown = SpriteSeta, mask = SpriteMask, inputField = SpriteInput
        };
        var go = DefaultControls.CreateSlider(res);
        go.name = "Slider";
        go.transform.SetParent(pai, false);
        DefinirLayer(go);

        var s = go.GetComponent<Slider>();
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = 1f;
        SemNavegacao(s);

        var bg = go.transform.Find("Background")?.GetComponent<Image>();
        if (bg) bg.color = CorLinha;
        var fill = go.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        if (fill) fill.color = CorAcento;
        var handle = go.transform.Find("Handle Slide Area/Handle");
        if (handle)
        {
            handle.GetComponent<Image>().color = CorTexto;
            ((RectTransform)handle).sizeDelta = new Vector2(28, 0);
        }
        return s;
    }

    private static Toggle CriarToggle(Transform pai)
    {
        var rt = Novo("Toggle", pai);
        Layout(rt.gameObject, prefW: 44, prefH: 44);

        var fundo = Novo("Fundo", rt);
        Esticar(fundo);
        var imgFundo = Img(fundo, CorCampo, SpriteUI);
        imgFundo.pixelsPerUnitMultiplier = 0.5f;

        var marca = Novo("Marca", fundo);
        Esticar(marca);
        marca.offsetMin = new Vector2(8, 8);
        marca.offsetMax = new Vector2(-8, -8);
        var imgMarca = Img(marca, CorAcento, SpriteUI);
        imgMarca.raycastTarget = false;

        var t = rt.gameObject.AddComponent<Toggle>();
        t.targetGraphic = imgFundo;
        t.graphic = imgMarca;
        t.isOn = true;
        SemNavegacao(t);
        return t;
    }

    private static TMP_Dropdown CriarDropdown(Transform pai)
    {
        var res = new TMP_DefaultControls.Resources
        {
            standard = SpriteUI, background = SpriteFundo, knob = SpriteKnob,
            checkmark = SpriteCheck, dropdown = SpriteSeta, mask = SpriteMask, inputField = SpriteInput
        };
        var go = TMP_DefaultControls.CreateDropdown(res);
        go.name = "Dropdown";
        go.transform.SetParent(pai, false);
        DefinirLayer(go);
        Layout(go, flexW: 1, prefW: 200, prefH: 48);

        var dd = go.GetComponent<TMP_Dropdown>();
        SemNavegacao(dd);
        go.GetComponent<Image>().color = CorCampo;

        if (dd.captionText)
        {
            dd.captionText.font = fonte;
            dd.captionText.fontSize = 22;
            dd.captionText.color = CorEscuro;
        }
        if (dd.itemText)
        {
            dd.itemText.font = fonte;
            dd.itemText.fontSize = 22;
            dd.itemText.color = CorEscuro;
            var item = (RectTransform)dd.itemText.transform.parent;
            item.sizeDelta = new Vector2(item.sizeDelta.x, 42);
            var content = (RectTransform)item.parent;
            content.sizeDelta = new Vector2(content.sizeDelta.x, 42);
        }
        if (dd.template)
            dd.template.sizeDelta = new Vector2(dd.template.sizeDelta.x, 280);

        var seta = go.transform.Find("Arrow") as RectTransform;
        if (seta) seta.sizeDelta = new Vector2(28, 28);
        return dd;
    }

    // =====================================================================
    //  Helpers de UI
    // =====================================================================
    private static RectTransform CriarCanvas(string nome, int ordem)
    {
        var rt = Novo(nome, null);
        var canvas = rt.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordem;
        var scaler = rt.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        rt.gameObject.AddComponent<GraphicRaycaster>();
        return rt;
    }

    private static void CriarEventSystem()
    {
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    private static RectTransform Linha(Transform pai, string nome, string rotulo, float altura = 56)
    {
        var rt = Novo(nome, pai);
        var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        Layout(rt.gameObject, prefH: altura, minH: altura);

        if (rotulo != null)
        {
            var t = Txt(rt, "Rotulo", rotulo, 26, CorTexto, TextAlignmentOptions.MidlineLeft);
            Layout(t.gameObject, prefW: 200, prefH: altura);
        }
        return rt;
    }

    private static void Secao(Transform pai, string texto)
    {
        var t = Txt(pai, "Secao_" + texto, texto, 18, CorAcento, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        t.characterSpacing = 10;
        Layout(t.gameObject, prefH: 34);
    }

    private static void Divisor(Transform pai)
    {
        var d = Novo("Divisor", pai);
        Img(d, CorLinha).raycastTarget = false;
        Layout(d.gameObject, prefH: 2);
    }

    private static Button Botao(Transform pai, string nome, string texto, Color fundo, Color corTexto,
                                float tamFonte, float raio = 0.35f)
    {
        var rt = Novo(nome, pai);
        // Imagem branca: a cor real vem do ColorBlock (permite clarear no hover)
        var img = Img(rt, Color.white, SpriteUI);
        img.pixelsPerUnitMultiplier = raio;

        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cores = ColorBlock.defaultColorBlock;
        cores.normalColor = fundo;
        cores.highlightedColor = Color.Lerp(fundo, Color.white, 0.15f);
        cores.pressedColor = Color.Lerp(fundo, Color.black, 0.15f);
        cores.selectedColor = fundo;
        cores.disabledColor = WithAlpha(fundo, 0.5f);
        cores.colorMultiplier = 1f;
        cores.fadeDuration = 0.08f;
        b.colors = cores;
        SemNavegacao(b);

        var t = Txt(rt, "Texto", texto, tamFonte, corTexto, TextAlignmentOptions.Center, FontStyles.Bold);
        Esticar(t.rectTransform);
        return b;
    }

    private static TextMeshProUGUI Txt(Transform pai, string nome, string texto, float tamanho, Color cor,
                                       TextAlignmentOptions alinhamento, FontStyles estilo = FontStyles.Normal)
    {
        var rt = Novo(nome, pai);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = fonte;
        t.text = texto;
        t.fontSize = tamanho;
        t.color = cor;
        t.alignment = alinhamento;
        t.fontStyle = estilo;
        t.raycastTarget = false;
        return t;
    }

    private static Image Img(RectTransform rt, Color cor, Sprite sprite = null, bool fatiado = true)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = cor;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = fatiado ? Image.Type.Sliced : Image.Type.Simple;
        }
        return img;
    }

    private static RectTransform Novo(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.layer = 5; // UI
        var rt = (RectTransform)go.transform;
        if (pai != null) rt.SetParent(pai, false);
        return rt;
    }

    private static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Ancorar(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 tamanho)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
    }

    private static void Layout(GameObject go, float prefW = -1, float prefH = -1, float flexW = -1, float minH = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();   // (?? não funciona com objetos Unity)
        le.preferredWidth = prefW;
        le.preferredHeight = prefH;
        le.flexibleWidth = flexW;
        le.minHeight = minH;
    }

    private static void SemNavegacao(Selectable s)
    {
        var n = s.navigation;
        n.mode = Navigation.Mode.None;   // setas/Tab não roubam o foco dos controles
        s.navigation = n;
    }

    private static void DefinirLayer(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
    }

    private static void CriarPasta(string pai, string nome)
    {
        if (!AssetDatabase.IsValidFolder($"{pai}/{nome}"))
            AssetDatabase.CreateFolder(pai, nome);
    }

    private static Vector2 V(float x, float y) => new Vector2(x, y);

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
