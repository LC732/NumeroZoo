using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Inicia e encerra o servidor de mãos empacotado (PyInstaller) que fica em
/// Assets/StreamingAssets/ServidorMaos/. Não precisa de Python instalado no PC.
/// Persiste entre cenas (DontDestroyOnLoad).
/// </summary>
public class ServidorPython : MonoBehaviour
{
    public static ServidorPython Instancia { get; private set; }

    [Header("Executável (dentro de StreamingAssets)")]
    public string pasta = "ServidorMaos";
    public string nomeExecutavel = "servidor_maos";

    [Header("Parâmetros")]
    public int porta = 5555;
    public int indiceCamera = 0;

    [Header("Editor")]
    [Tooltip("Desmarque se no Editor você prefere rodar o Server.py manualmente pelo terminal.")]
    public bool iniciarNoEditor = true;

    [Header("Debug")]
    public bool mostrarLogs = true;

    private Process processo;

    public bool EstaRodando
    {
        get
        {
            try { return processo != null && !processo.HasExited; }
            catch { return false; }
        }
    }

    public static ServidorPython GarantirInstancia()
    {
        if (Instancia != null) return Instancia;
        var go = new GameObject("ServidorPython");
        return go.AddComponent<ServidorPython>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Subpasta de cada sistema: ServidorMaos/Windows, ServidorMaos/Linux, ServidorMaos/Mac.</summary>
    public static string PastaDaPlataforma()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                return "Windows";
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                return "Mac";
            default:
                return "Linux";
        }
    }

    public string CaminhoExecutavel()
    {
        string nome = nomeExecutavel;
        if (PastaDaPlataforma() == "Windows")
            nome += ".exe";
        return Path.Combine(Application.streamingAssetsPath, pasta, PastaDaPlataforma(), nome);
    }

    /// <summary>Inicia o servidor. Retorna true se já estava rodando ou se iniciou.</summary>
    public bool Iniciar()
    {
        if (EstaRodando) return true;

        if (Application.isEditor && !iniciarNoEditor)
        {
            Debug.Log("[Servidor] iniciarNoEditor desligado — rode o Server.py manualmente.");
            return false;
        }

        string caminho = CaminhoExecutavel();
        if (!File.Exists(caminho))
        {
            Debug.LogWarning($"[Servidor] Executável não encontrado em: {caminho}\n" +
                             $"Baixe o servidor_maos-{PastaDaPlataforma()} do GitHub Actions e extraia em " +
                             $"StreamingAssets/{pasta}/{PastaDaPlataforma()}/.");
            return false;
        }

        GarantirPermissaoExecucao(caminho);

        var psi = new ProcessStartInfo
        {
            FileName = caminho,
            Arguments = $"--port {porta} --camera {indiceCamera}",
            WorkingDirectory = Path.GetDirectoryName(caminho),
            UseShellExecute = false,
            CreateNoWindow = true,          // sem janela de terminal
            RedirectStandardInput = true,   // o Python se encerra quando este pipe fecha
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
        psi.EnvironmentVariables["TF_CPP_MIN_LOG_LEVEL"] = "2";   // menos spam do TFLite
        psi.EnvironmentVariables["GLOG_minloglevel"] = "2";

        try
        {
            processo = new Process { StartInfo = psi, EnableRaisingEvents = true };
            processo.OutputDataReceived += (_, e) =>
            {
                if (mostrarLogs && !string.IsNullOrEmpty(e.Data)) Debug.Log("[Servidor] " + e.Data);
            };
            processo.ErrorDataReceived += (_, e) =>
            {
                if (mostrarLogs && !string.IsNullOrEmpty(e.Data)) Debug.Log("[Servidor:err] " + e.Data);
            };
            processo.Exited += (_, __) => Debug.Log("[Servidor] processo encerrado.");

            processo.Start();
            processo.BeginOutputReadLine();
            processo.BeginErrorReadLine();
            Debug.Log($"[Servidor] iniciado (PID {processo.Id}) na porta {porta}.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[Servidor] Falha ao iniciar: " + ex.Message);
            processo = null;
            return false;
        }
    }

    public void Parar()
    {
        if (processo == null) return;
        try
        {
            if (!processo.HasExited)
            {
                // Fechar o stdin faz o Server.py encerrar sozinho e liberar a câmera
                try { processo.StandardInput.Close(); } catch { }
                if (!processo.WaitForExit(800))
                    processo.Kill();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Servidor] Erro ao encerrar: " + ex.Message);
        }
        finally
        {
            processo.Dispose();
            processo = null;
        }
    }

    private static void GarantirPermissaoExecucao(string caminho)
    {
        // No Linux/macOS o bit de execução pode se perder ao copiar StreamingAssets
        if (Application.platform != RuntimePlatform.LinuxPlayer &&
            Application.platform != RuntimePlatform.LinuxEditor &&
            Application.platform != RuntimePlatform.OSXPlayer &&
            Application.platform != RuntimePlatform.OSXEditor)
            return;
        try
        {
            using var chmod = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"+x \"{caminho}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            chmod?.WaitForExit(2000);
        }
        catch { }
    }

    void OnApplicationQuit() => Parar();

    void OnDestroy()
    {
        if (Instancia == this)
        {
            Parar();
            Instancia = null;
        }
    }
}
