using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ParrotManager : MonoBehaviour
{
    [Header("Parrots")]
    [Tooltip("Nó vazio que contém todos os parrots como filhos")]
    public Transform parrotsContainer;

    public Parrot[] parrots;

    [SerializeField] private TextMeshProUGUI textoQuantidade;

    [Header("Caixa limite")]
    public Vector3 boxSize = new Vector3(10f, 4f, 10f);

    [Header("Velocidade")]
    public float speed = 2f;
    public float maxSpeed = 3f;
    public float minSpeed = 1f;

    [Header("Boids - pesos")]
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 1f;
    public float boundsWeight = 2f;

    [Header("Boids - raios")]
    public float neighborRadius = 3f;
    public float separationRadius = 1.2f;

    // Cache pra evitar alocação de memória todo frame
    private Vector3[] newVelocities;
    private float[] parrotPhases;   // Desfase para cada parrot (para não ficarem sincronizados)
    private float[] parrotAngle;
    private float[] rotSpeed;      // velocidade da oscilação de rotação
    private float[] bobSpeed;      // velocidade do bob vertical
    private float[] rotAmplitude;  // amplitude da rotação
    private float[] bobAmplitude;  // amplitude do bob


    void Start()
    {
        if (parrotsContainer != null)
        {
            parrots = parrotsContainer.GetComponentsInChildren<Parrot>(true);
        }

        int count = parrots.Length;

        parrotPhases   = new float[count];
        parrotAngle    = new float[count];
        rotSpeed       = new float[count];
        bobSpeed       = new float[count];
        rotAmplitude   = new float[count];
        bobAmplitude   = new float[count];

        for (int i = 0; i < count; i++)
        {
            // Fase inicial bem aleatória
            parrotPhases[i] = Random.Range(0f, 100f);

            // Ângulo inicial real
            parrotAngle[i] = parrots[i].transform.localEulerAngles.y;

            // Cada um com velocidade de rotação diferente
            rotSpeed[i] = Random.Range(1.3f, 2.4f);

            // Cada um com velocidade de bob diferente
            bobSpeed[i] = Random.Range(1.8f, 3.2f);

            // Amplitude de rotação diferente
            rotAmplitude[i] = Random.Range(18f, 38f);

            // Amplitude de bob diferente
            bobAmplitude[i] = Random.Range(0.12f, 0.28f);
        }
    }
    void Update()
    {
        for (int i = 0; i < parrots.Length; i++)
        {
            if (!parrots[i].gameObject.activeSelf) continue;

            GameObject parrotObj = parrots[i].gameObject;

            // Oscilação com parâmetros individuais
            float wave = Mathf.Sin(Time.time * rotSpeed[i] + parrotPhases[i]);
            float zAngle = 0.0f;
            // float zAngle = wave * rotAmplitude[i];
            float yAngle = parrotAngle[i] - (wave * rotAmplitude[i] * 0.7f); // um pouco menos no Y

            parrotObj.transform.localRotation = Quaternion.Euler(0f, yAngle, zAngle);

            // Bob vertical individual
            float bobY = Mathf.Sin(Time.time * bobSpeed[i] + parrotPhases[i]) * bobAmplitude[i];
            parrots[i].velocity = new Vector3(0f, bobY, 0f);
            parrots[i].Move(speed, Time.deltaTime);
        }
    }

    public void SetQuantidade(int quantidade)
    {
        quantidade = Mathf.Clamp(quantidade, 0, parrots.Length);

        for (int i = 0; i < parrots.Length; i++)
        {
            bool deveEstarAtivo = i < quantidade;
            bool estavaAtivo = parrots[i].gameObject.activeSelf;

            if (estavaAtivo != deveEstarAtivo)
            {
                parrots[i].gameObject.SetActive(deveEstarAtivo);

                // Se acabou de ser ativado → sorteia uma nova fase
                if (deveEstarAtivo)
                {
                    parrotPhases[i] = Random.Range(0f, 100f);
                    
                    // Opcional: também re-sorteia as velocidades e amplitudes
                    // para ficar ainda mais diferente
                    rotSpeed[i]     = Random.Range(1.3f, 2.4f);
                    bobSpeed[i]     = Random.Range(1.8f, 3.2f);
                    rotAmplitude[i] = Random.Range(18f, 38f);
                    bobAmplitude[i] = Random.Range(0.12f, 0.28f);
                }
            }
        }

        if (textoQuantidade != null)
            textoQuantidade.text = quantidade.ToString();
        else
            Debug.LogWarning("textoQuantidade não atribuído no Inspector!");
    }
}