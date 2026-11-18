using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System;

/// <summary>
/// Base comum dos 3 conectores (Aquário, Jardim, Cerrado).
/// Recebe frames RGBA do servidor Python (MediaPipe) via TCP, mostra no RawImage
/// e repassa o gesto para o ContagemAnimais.
///
///  • Só conecta depois que o jogador permite a câmera (balão do menu).
///  • Tenta de novo a cada 0,5 s enquanto o servidor sobe e reconecta se cair.
///  • Gesto e teclado 0–9 passam pelo ContagemAnimais.
///  • Guarda só o frame mais recente (não acumula fila).
///  • A inversão vertical é feita pelo uvRect do RawImage (GPU).
///
/// Cada conector só precisa dizer o que fazer quando a quantidade muda
/// (AplicarQuantidade).
/// </summary>
public abstract class ReceptorMediaPipe : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private RawImage displayImage;

    [Header("Socket")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int port = 5555;
    [SerializeField] private float intervaloReconexao = 0.5f;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private volatile bool isRunning = true;
    private volatile bool conectado = false;

    // Último gesto recebido do Python (-1 = nada recebido ainda)
    private volatile int gestoRecebido = -1;

    // Só o frame mais recente — se o jogo atrasar, frames velhos são descartados
    private readonly object travaFrame = new object();
    private byte[] frameMaisRecente;
    private int frameW, frameH;

    // Textura RGBA32 — canal A carrega a máscara de segmentação
    private Texture2D cameraTexture;

    /// <summary>Chamado sempre que a quantidade muda (gesto ou teclado).</summary>
    protected abstract void AplicarQuantidade(int quantidade);

    protected virtual void Start()
    {
        // O servidor manda as linhas de cima para baixo (OpenCV) e a Unity
        // espera de baixo para cima: inverter o uvRect resolve sem custo de CPU.
        if (displayImage != null)
            displayImage.uvRect = new Rect(0f, 1f, 1f, -1f);

        ContagemAnimais.AoMudar += AplicarQuantidade;
        StartCoroutine(AplicarValorInicial());
        StartCoroutine(LoopConexao());
    }

    private IEnumerator AplicarValorInicial()
    {
        yield return null; // espera o Start() do manager montar os arrays
        AplicarQuantidade(ContagemAnimais.Valor);
    }

    // ─────────────────────────────────────────────
    // Conexão (com espera da permissão e reconexão)
    // ─────────────────────────────────────────────
    private IEnumerator LoopConexao()
    {
        while (!ConsentimentoCamera.PodeUsarCamera)
        {
            if (ConsentimentoCamera.Respondido)
            {
                Debug.Log("📷 Câmera recusada — usando só o teclado (0–9).");
                yield break;
            }
            yield return null;
        }

        bool avisou = false;
        while (isRunning)
        {
            if (!conectado)
            {
                if (TentarConectar())
                {
                    avisou = false;
                }
                else if (!avisou)
                {
                    Debug.Log("⏳ Aguardando o servidor MediaPipe subir...");
                    avisou = true;
                }
            }
            yield return new WaitForSeconds(intervaloReconexao);
        }
    }

    private bool TentarConectar()
    {
        FecharConexao();
        try
        {
            client = new TcpClient();
            client.NoDelay = true;
            client.Connect(serverIP, port);
            stream = client.GetStream();
            conectado = true;
            Debug.Log("✅ Conectado ao servidor MediaPipe!");

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
            return true;
        }
        catch (Exception)
        {
            FecharConexao();
            return false;
        }
    }

    private void FecharConexao()
    {
        conectado = false;
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }
        stream = null;
        client = null;
    }

    // ─────────────────────────────────────────────
    // Loop de recepção (thread separada)
    // ─────────────────────────────────────────────
    private void ReceiveLoop()
    {
        var s = stream;
        while (isRunning)
        {
            try
            {
                // Header: width (4) + height (4) + payloadSize (4) + gesto (4) = 16 bytes
                byte[] header = ReadExact(s, 16);
                if (header == null) break;

                int width       = BitConverter.ToInt32(header, 0);
                int height      = BitConverter.ToInt32(header, 4);
                int payloadSize = BitConverter.ToInt32(header, 8);
                gestoRecebido   = BitConverter.ToInt32(header, 12);

                byte[] rgba = ReadExact(s, payloadSize);
                if (rgba == null) break;

                lock (travaFrame)
                {
                    frameMaisRecente = rgba;
                    frameW = width;
                    frameH = height;
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogWarning("⚠️ Conexão perdida: " + e.Message);
                break;
            }
        }
        conectado = false; // o LoopConexao tenta reconectar
    }

    private static byte[] ReadExact(NetworkStream s, int count)
    {
        byte[] buf = new byte[count];
        int total = 0;
        while (total < count)
        {
            int n = s.Read(buf, total, count - total);
            if (n == 0) return null;
            total += n;
        }
        return buf;
    }

    // ─────────────────────────────────────────────
    // Thread principal
    // ─────────────────────────────────────────────
    protected virtual void Update()
    {
        byte[] frame = null;
        int w = 0, h = 0;
        lock (travaFrame)
        {
            if (frameMaisRecente != null)
            {
                frame = frameMaisRecente;
                w = frameW;
                h = frameH;
                frameMaisRecente = null;
            }
        }
        if (frame != null) ApplyFrame(frame, w, h);

        // Só repassa quando o gesto MUDA → o teclado não é sobrescrito a cada frame
        int gesto = gestoRecebido;
        if (gesto >= 0) ContagemAnimais.ReceberGesto(gesto);
    }

    private void ApplyFrame(byte[] rgbaBytes, int width, int height)
    {
        if (cameraTexture == null || cameraTexture.width != width || cameraTexture.height != height)
        {
            if (cameraTexture != null) Destroy(cameraTexture);
            cameraTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Debug.Log($"📐 Textura criada: {width}x{height} RGBA32");
        }

        cameraTexture.LoadRawTextureData(rgbaBytes);
        cameraTexture.Apply(false);

        if (displayImage != null)
            displayImage.texture = cameraTexture;
    }

    protected virtual void OnDestroy()
    {
        ContagemAnimais.AoMudar -= AplicarQuantidade;
        isRunning = false;
        FecharConexao();
        receiveThread?.Join(500);
        if (cameraTexture != null) Destroy(cameraTexture);
    }
}
