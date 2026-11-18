using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BeeManager : MonoBehaviour
{
    [Header("Bees")]
    public Bees[] bees;

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
    private float[] beePhases;        // Desfase para cada abelha (para não ficarem sincronizadas)
    private float[] beeAngle;

    void Start()
    {

        // === Inicialização das fases das abelhas ===
        beePhases = new float[bees.Length];
        beeAngle = new float[bees.Length];
        for (int i = 0; i < bees.Length; i++)
        {
            beePhases[i] = i * 1.65f;
            beeAngle[i] = bees[i].gameObject.transform.rotation.y;
        }
    }

    void Update()
    {
        for (int i = 0; i < bees.Length; i++)
        {
            if (!bees[i].gameObject.activeSelf) continue;

            GameObject beeObj = bees[i].gameObject;

            // ROTAÇÃO NO Z
            float zAngle = Mathf.Sin(Time.time * 1.8f + beePhases[i]) * 30f;
            Vector3 currentRotation = beeObj.transform.localEulerAngles;
            currentRotation.z = zAngle;

            // ROTAÇÃO NO Y

            float yAngle = beeAngle[i] - (Mathf.Sin(Time.time * 1.8f + beePhases[i]) * 30f);
            currentRotation.y = yAngle;
            beeObj.transform.localRotation = Quaternion.Euler(currentRotation);


            // MOVIMENTO VERTICAL LEVE
            float bobY = Mathf.Sin(Time.time * 2.3f + beePhases[i]) * 0.2f;
            bees[i].velocity = new Vector3(0, bobY, 0);
            bees[i].Move(speed, Time.deltaTime);
        }
    }

   public void SetQuantidade(int quantidade)
    {
        quantidade = Mathf.Clamp(quantidade, 0, bees.Length);

        for (int i = 0; i < bees.Length; i++)
        {
            bool deveEstarAtivo = i < quantidade;
            
            if (bees[i].gameObject.activeSelf != deveEstarAtivo)
                bees[i].gameObject.SetActive(deveEstarAtivo);
        }
        
        // Só atualiza texto se existir
        if (textoQuantidade != null)
            textoQuantidade.text = quantidade.ToString();
        else
            Debug.LogWarning("textoQuantidade não atribuído no Inspector!");
    }

}