using UnityEngine;
using TMPro;

public class FishManager : MonoBehaviour
{
    [Header("Peixes")]
    public Fish[] fishes;

    [SerializeField] private TextMeshProUGUI textoQuantidade;

    [Header("Velocidade")]
    public float speed = 2f;
    public float maxSpeed = 3f;
    public float minSpeed = 1f;



    // Cache pra evitar alocação de memória todo frame
    private Vector3[] newVelocities;
    private float[] beePhases; 
    private float[] nextLookTime;

    void Start()
    {
        // === Inicialização das fases das abelhas ===
        nextLookTime = new float[fishes.Length];
        beePhases = new float[fishes.Length];
        for (int i = 0; i < fishes.Length; i++)
        {
            nextLookTime[i] = Time.time + Random.Range(1f, 3f);
            beePhases[i] = i * 1.65f;

        }
    }

    void Update()
    {
        for (int i = 0; i < fishes.Length; i++)
        {
            if (!fishes[i].gameObject.activeSelf) continue;

            // MOVIMENTO VERTICAL LEVE
            float bobY = Mathf.Sin(Time.time * 2.3f + beePhases[i]) * 0.1f;
            fishes[i].velocity = new Vector3(0, bobY, 0);
            fishes[i].Move(speed, Time.deltaTime);
            // LOOK ALEATÓRIO
            if (Time.time >= nextLookTime[i])
            {
                fishes[i].Look();

                // Próximo look entre 2 e 8 segundos
                nextLookTime[i] = Time.time + Random.Range(1f, 4f);
            }
        }
    }

   public void SetQuantidade(int quantidade)
    {
        quantidade = Mathf.Clamp(quantidade, 0, fishes.Length);

        for (int i = 0; i < fishes.Length; i++)
        {
            bool deveEstarAtivo = i < quantidade;
            
            if (fishes[i].gameObject.activeSelf != deveEstarAtivo)
                fishes[i].gameObject.SetActive(deveEstarAtivo);
        }
        
        // Só atualiza texto se existir
        if (textoQuantidade != null)
            textoQuantidade.text = quantidade.ToString();
        else
            Debug.LogWarning("textoQuantidade não atribuído no Inspector!");
    }

}