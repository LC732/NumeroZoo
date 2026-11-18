using UnityEngine;

public class Bees : MonoBehaviour
{
    [HideInInspector] public Vector3 velocity;
    private Vector3 inicialPosition;
    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem particlePrefab;

    private ParticleSystem myParticle;

    private void OnEnable()
    {
        PlayParticle();
    }

    private void OnDisable()
    {
        transform.position = inicialPosition;
        PlayParticle();
    }

    private void PlayParticle()
    {
        if (myParticle == null)
        {
            return;
        }

        myParticle.transform.position = transform.position;
        myParticle.Play();

    }

    private void Awake()
    {
        // Criamos a partícula o mais cedo possível
        if (particlePrefab != null)
        {
            myParticle = Instantiate(particlePrefab, transform.position, Quaternion.identity);
        }
    }

    void Start()
    {
        // Velocidade inicial aleatória
        inicialPosition = transform.position;
        velocity = Random.insideUnitSphere * 2f;
        velocity.y *= 0.3f; // peixes se movem mais no plano horizontal
    }

    public void Move(float speed, float deltaTime)
    {
        transform.position += velocity * deltaTime;
        /*Quaternion targetRot = Quaternion.LookRotation(velocity);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, deltaTime * 3f // era 5f, reduz
            );*/
    }

}