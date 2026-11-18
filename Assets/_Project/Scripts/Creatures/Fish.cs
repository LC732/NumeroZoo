using UnityEngine;

public class Fish : MonoBehaviour
{
    [HideInInspector] public Vector3 velocity;

    private Vector3 inicialPosition;
    private Animator animator;

    private bool looking = false;

    private float lookStartY;
    private float lookTargetY;
    private float lookTimer;

    [Header("Look")]
    public float lookAngle = 90f;
    public float lookDuration = 20f;

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem particlePrefab;

    private ParticleSystem myParticle;


    void Start()
    {
        animator = GetComponent<Animator>();

        inicialPosition = transform.position;

        velocity = Random.insideUnitSphere * 2f;
        velocity.y *= 0.3f;
    }

    void Update()
    {
        if (looking)
        {
            lookTimer += Time.deltaTime;

            float t = Mathf.Clamp01(lookTimer / lookDuration);

            float y = Mathf.LerpAngle(
                lookStartY,
                lookTargetY,
                t
            );

            transform.eulerAngles = new Vector3(
                transform.eulerAngles.x,
                y,
                transform.eulerAngles.z
            );

            if (t >= 1f)
            {
                looking = false;
                animator.CrossFade("idle", 0.2f);
            }
        }
    }

     private void Awake()
    {
        // Criamos a partícula o mais cedo possível
        if (particlePrefab != null)
        {
            myParticle = Instantiate(particlePrefab, transform.position, Quaternion.identity);
        }
    }

    private void OnDisable()
    {
        transform.position = inicialPosition;
        transform.rotation = Quaternion.identity;
        PlayParticle();
        looking = false;
        lookTimer = 0f;
    }
    private void OnEnable()
    {
        PlayParticle();
    }

    public void Move(float speed, float deltaTime)
    {
        transform.position += velocity * deltaTime;
    }

    public void Look()
    {
        if (looking)
            return;

        lookTimer = 0f;
        looking = true;

        lookStartY = transform.eulerAngles.y;

        bool right = Random.value > 0.5f;

        if (right)
        {
            lookTargetY = lookStartY + lookAngle;
            animator.CrossFade("idle_right", 0.2f);
        }
        else
        {
            lookTargetY = lookStartY - lookAngle;
            animator.CrossFade("idle_left", 0.2f);
        }
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

}