using UnityEngine;

public class Parrot : MonoBehaviour
{
    [HideInInspector] public Vector3 velocity;
    private Vector3 inicialPosition;

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem particlePrefab;
    private ParticleSystem myParticle;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(); // pega o Animator (mesmo se estiver em um filho)

        if (particlePrefab != null)
        {
            myParticle = Instantiate(particlePrefab, transform.position, Quaternion.identity);
        }
    }

    private void OnEnable()
    {
        if (animator != null)
        {
            // Começa a animação em um ponto aleatório do clip (0% até 100%)
            float randomStart = Random.Range(0f, 1f);
            animator.Play(0, 0, randomStart);

            // Opcional: também muda um pouco a velocidade da animação
            animator.speed = Random.Range(0.2f, 0.2f);
        }

        PlayParticle();
    }

    private void OnDisable()
    {
        transform.position = inicialPosition;
        PlayParticle();
    }

    private void PlayParticle()
    {
        if (myParticle == null) return;
        myParticle.transform.position = transform.position;
        myParticle.Play();
    }

    void Start()
    {
        inicialPosition = transform.position;
        velocity = Random.insideUnitSphere * 2f;
        velocity.y *= 0.3f;
    }

    public void Move(float speed, float deltaTime)
    {
        transform.position += velocity * deltaTime;
    }
}